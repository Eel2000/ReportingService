using Carter;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl.Matchers;
using ReportingService.Api.Data;
using ReportingService.Api.Jobs;
using Serilog;
using Serilog.Events;

// Configure Serilog to only log exceptions (Error level and above)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{

    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add Carter for minimal API endpoints
    builder.Services.AddCarter();

    // Add PostgreSQL DbContext with Aspire integration
    builder.AddNpgsqlDbContext<ApplicationDbContext>("reportingdb");

    // Add Quartz scheduler with PostgreSQL persistence
    builder.Services.AddQuartz(q =>
    {
        // Scheduler identification
        q.SchedulerId = "AUTO";
        q.SchedulerName = "ReportingServiceScheduler";

        // Use PostgreSQL as persistent job store
        q.UsePersistentStore(store =>
        {
            store.UseProperties = true;
            store.RetryInterval = TimeSpan.FromSeconds(15);

            // PostgreSQL configuration
            store.UsePostgres(db =>
            {
                db.ConnectionString = builder.Configuration.GetConnectionString("reportingdb")!;
                db.TablePrefix = "qrtz_";
            });

            // JSON serialization for job data map
            store.UseNewtonsoftJsonSerializer();

            // Clustering support for multiple instances
            store.UseClustering(cluster =>
            {
                cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                cluster.CheckinInterval = TimeSpan.FromSeconds(10);
            });
        });

        // Thread pool configuration
        q.UseDefaultThreadPool(tp =>
        {
            tp.MaxConcurrency = 10;
        });

        // Misfire handling - interrupt jobs that are recovering
        q.InterruptJobsOnShutdown = true;
        q.InterruptJobsOnShutdownWithWait = true;

        // Configure job listeners for advanced monitoring (optional)
        q.AddJobListener<LoggingJobListener>(GroupMatcher<JobKey>.AnyGroup());
    });

    builder.Services.AddQuartzHostedService(options =>
    {
        // Graceful shutdown - wait for jobs to complete
        options.WaitForJobsToComplete = true;

        // Start scheduler automatically
        options.AwaitApplicationStarted = true;

        // Delay before starting scheduler
        options.StartDelay = TimeSpan.FromSeconds(5);
    });

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reporting Service API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    app.UseHttpsRedirection();

    // Map Carter endpoints
    app.MapCarter();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
