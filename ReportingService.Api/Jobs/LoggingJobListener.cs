using Quartz;
using Quartz.Listener;

namespace ReportingService.Api.Jobs;

public class LoggingJobListener : JobListenerSupport
{
    private readonly ILogger<LoggingJobListener> _logger;

    public LoggingJobListener(ILogger<LoggingJobListener> logger)
    {
        _logger = logger;
    }

    public override string Name => "LoggingJobListener";

    public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Job {JobKey} is about to be executed in group {JobGroup}",
            context.JobDetail.Key.Name,
            context.JobDetail.Key.Group);

        return Task.CompletedTask;
    }

    public override Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Job {JobKey} execution was vetoed",
            context.JobDetail.Key.Name);

        return Task.CompletedTask;
    }

    public override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        if (jobException != null)
        {
            _logger.LogError(
                jobException,
                "Job {JobKey} failed with exception",
                context.JobDetail.Key.Name);
        }
        else
        {
            _logger.LogInformation(
                "Job {JobKey} executed successfully. Duration: {Duration}ms",
                context.JobDetail.Key.Name,
                context.JobRunTime.TotalMilliseconds);
        }

        return Task.CompletedTask;
    }
}
