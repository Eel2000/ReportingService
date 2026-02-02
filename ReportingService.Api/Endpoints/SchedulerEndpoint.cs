using Carter;
using Microsoft.EntityFrameworkCore;
using Quartz;
using ReportingService.Api.Data;
using ReportingService.Api.Jobs;
using ReportingService.Api.Models;

namespace ReportingService.Api.Endpoints;

public class SchedulerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules")
            .WithTags("Schedules");

        group.MapGet("/", GetAllSchedules)
            .WithName("GetAllSchedules")
            .WithDescription("Get all schedules");

        group.MapGet("/{id:guid}", GetScheduleById)
            .WithName("GetScheduleById")
            .WithDescription("Get a schedule by ID");

        group.MapPost("/", CreateSchedule)
            .WithName("CreateSchedule")
            .WithDescription("Create a new schedule and register it with Quartz");

        group.MapPut("/{id:guid}", UpdateSchedule)
            .WithName("UpdateSchedule")
            .WithDescription("Update an existing schedule");

        group.MapDelete("/{id:guid}", DeleteSchedule)
            .WithName("DeleteSchedule")
            .WithDescription("Delete a schedule and unregister it from Quartz");

        group.MapPost("/{id:guid}/pause", PauseSchedule)
            .WithName("PauseSchedule")
            .WithDescription("Pause a schedule");

        group.MapPost("/{id:guid}/resume", ResumeSchedule)
            .WithName("ResumeSchedule")
            .WithDescription("Resume a paused schedule");
    }

    private static async Task<IResult> GetAllSchedules(ApplicationDbContext db)
    {
        var schedules = await db.Schedules.ToListAsync();
        return Results.Ok(schedules);
    }

    private static async Task<IResult> GetScheduleById(Guid id, ApplicationDbContext db)
    {
        var schedule = await db.Schedules.FindAsync(id);
        return schedule is null ? Results.NotFound() : Results.Ok(schedule);
    }

    private static async Task<IResult> CreateSchedule(
        Schedule schedule,
        ApplicationDbContext db,
        ISchedulerFactory schedulerFactory)
    {
        schedule.Id = Guid.CreateVersion7();
        schedule.CreatedAt = DateTimeOffset.UtcNow;

        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();

        // Register with Quartz
        await RegisterQuartzJob(schedule, schedulerFactory);

        return Results.Created($"/api/schedules/{schedule.Id}", schedule);
    }

    private static async Task<IResult> UpdateSchedule(
        Guid id,
        Schedule updatedSchedule,
        ApplicationDbContext db,
        ISchedulerFactory schedulerFactory)
    {
        var schedule = await db.Schedules.FindAsync(id);
        if (schedule is null)
            return Results.NotFound();

        schedule.Name = updatedSchedule.Name;
        schedule.Type = updatedSchedule.Type;
        schedule.StartingDay = updatedSchedule.StartingDay;
        schedule.EndingDay = updatedSchedule.EndingDay;
        schedule.SendHour = updatedSchedule.SendHour;
        schedule.SendMinute = updatedSchedule.SendMinute;
        schedule.Recurrence = updatedSchedule.Recurrence;
        schedule.RetentionPeriod = updatedSchedule.RetentionPeriod;
        schedule.Description = updatedSchedule.Description;
        schedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        // Update Quartz job
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey(id.ToString(), "schedules");

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }

        await RegisterQuartzJob(schedule, schedulerFactory);

        return Results.Ok(schedule);
    }

    private static async Task<IResult> DeleteSchedule(
        Guid id,
        ApplicationDbContext db,
        ISchedulerFactory schedulerFactory)
    {
        var schedule = await db.Schedules.FindAsync(id);
        if (schedule is null)
            return Results.NotFound();

        // Remove from Quartz
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey(id.ToString(), "schedules");
        await scheduler.DeleteJob(jobKey);

        db.Schedules.Remove(schedule);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> PauseSchedule(
        Guid id,
        ApplicationDbContext db,
        ISchedulerFactory schedulerFactory)
    {
        var schedule = await db.Schedules.FindAsync(id);
        if (schedule is null)
            return Results.NotFound();

        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey(id.ToString(), "schedules");

        if (!await scheduler.CheckExists(jobKey))
            return Results.BadRequest("Schedule job not found in scheduler");

        await scheduler.PauseJob(jobKey);
        return Results.Ok(new { Message = "Schedule paused", ScheduleId = id });
    }

    private static async Task<IResult> ResumeSchedule(
        Guid id,
        ApplicationDbContext db,
        ISchedulerFactory schedulerFactory)
    {
        var schedule = await db.Schedules.FindAsync(id);
        if (schedule is null)
            return Results.NotFound();

        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey(id.ToString(), "schedules");

        if (!await scheduler.CheckExists(jobKey))
            return Results.BadRequest("Schedule job not found in scheduler");

        await scheduler.ResumeJob(jobKey);
        return Results.Ok(new { Message = "Schedule resumed", ScheduleId = id });
    }

    private static async Task RegisterQuartzJob(Schedule schedule, ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var jobKey = new JobKey(schedule.Id.ToString(), "schedules");
        var job = JobBuilder.Create<SampleJob>()
            .WithIdentity(jobKey)
            .WithDescription(schedule.Description ?? schedule.Name)
            .UsingJobData("ScheduleId", schedule.Id.ToString())
            .UsingJobData("ScheduleName", schedule.Name)
            .StoreDurably()
            .Build();

        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity($"{schedule.Id}-trigger", "schedules")
            .ForJob(jobKey);

        if (schedule.Type == ScheduleType.OneTime)
        {
            triggerBuilder.StartNow();
        }
        else
        {
            if (schedule.Recurrence == RecurrencePattern.Daily)
            {
                var daysOfWeek = Enumerable.Range(schedule.StartingDay, schedule.EndingDay - schedule.StartingDay + 1)
                    .Select(day => (DayOfWeek)day)
                    .ToArray();

                triggerBuilder.WithDailyTimeIntervalSchedule(x => x
                .StartingDailyAt(new TimeOfDay(schedule.SendHour, schedule.SendMinute))
                .OnDaysOfTheWeek(daysOfWeek));
            }
            else if (schedule.Recurrence == RecurrencePattern.Weekly)
            {
                //more cleaner approach for weekly recurrence
                triggerBuilder.WithSchedule(CronScheduleBuilder
                    .WeeklyOnDayAndHourAndMinute((DayOfWeek)schedule.StartingDay, schedule.SendHour, schedule.SendMinute));
            }
            else if (schedule.Recurrence == RecurrencePattern.Monthly)
            {
                //More cleaner approach for monthly recurrence
                triggerBuilder.WithSchedule(CronScheduleBuilder
                    .MonthlyOnDayAndHourAndMinute(schedule.StartingDay, schedule.SendHour, schedule.SendMinute));
            }
            else
            {
                var cronExpression = BuildCronExpression(schedule);
                triggerBuilder.WithCronSchedule(cronExpression);
            }


        }

        var trigger = triggerBuilder.Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    private static string BuildCronExpression(Schedule schedule)
    {
        // Build cron expression based on recurrence pattern
        // Format: Seconds Minutes Hours DayOfMonth Month DayOfWeek Year
        var hour = schedule.SendHour;
        var minute = schedule.SendMinute;

        return schedule.Recurrence switch
        {
            RecurrencePattern.Daily => $"0 {minute} {hour} ? * {schedule.StartingDay}-{schedule.EndingDay}",
            RecurrencePattern.Weekly => $"0 {minute} {hour} ? * {schedule.StartingDay}",
            RecurrencePattern.Monthly => $"0 {minute} {hour} {schedule.StartingDay} * ?",
            RecurrencePattern.Yearly => $"0 {minute} {hour} {schedule.StartingDay} 1 ?",
            _ => $"0 {minute} {hour} ? * MON-FRI"
        };
    }
}

