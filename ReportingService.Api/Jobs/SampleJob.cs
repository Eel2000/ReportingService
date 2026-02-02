using Quartz;

namespace ReportingService.Api.Jobs;

/// <summary>
/// Example job demonstrating Quartz job implementation.
/// Implement IJob interface for your scheduled tasks.
/// </summary>
[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class SampleJob : IJob
{
    private readonly ILogger<SampleJob> _logger;

    public SampleJob(ILogger<SampleJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        var jobData = context.MergedJobDataMap;
        
        _logger.LogInformation(
            "SampleJob executed at {Time}. FireInstanceId: {FireInstanceId}",
            DateTimeOffset.Now,
            context.FireInstanceId);

        // Access job data if needed
        // var someValue = jobData.GetString("someKey");

        return Task.CompletedTask;
    }
}
