using Microsoft.EntityFrameworkCore;
using ReportingService.Api.Models;

namespace ReportingService.Api.Data;

public class ApplicationDbContext : DbContext
{

    public DbSet<Schedule> Schedules { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ScheduleType>("scheduler_type");

        modelBuilder.HasPostgresEnum<RecurrencePattern>("recurrence_pattern");

        modelBuilder.HasPostgresEnum<DataRetention>("data_retention");

    }
}
