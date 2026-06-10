namespace R2SentinelBak.Features.Scheduling;
    
public class Worker(
    ILogger<Worker> logger,
    BackupOrchestrator orchestrator,
    IConfiguration configuration,
    BackupRunOptions runOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (runOptions.RunOnce)
        {
            try
            {
                logger.LogInformation("Run-once mode enabled. Starting backup orchestration immediately.");
                await orchestrator.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Run-once backup orchestration failed.");
            }
            return;
        }

        var runAtHour = configuration.GetValue<int?>("BackupSchedule:RunAtHour") ?? 2;
        var runAtMinute = configuration.GetValue<int?>("BackupSchedule:RunAtMinute") ?? 0;

        if (runAtHour is < 0 or > 23)
        {
            throw new InvalidOperationException("BackupSchedule:RunAtHour must be between 0 and 23.");
        }

        if (runAtMinute is < 0 or > 59)
        {
            throw new InvalidOperationException("BackupSchedule:RunAtMinute must be between 0 and 59.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = GetNextRunTime(DateTime.Now, runAtHour, runAtMinute);
            var delay = nextRun - DateTime.Now;

            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Next backup run scheduled at {NextRun}.", nextRun);
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                logger.LogInformation("Starting scheduled backup orchestration at: {time}", DateTimeOffset.Now);
                await orchestrator.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backup cycle failed.");
            }

        }
    }

    private static DateTime GetNextRunTime(DateTime now, int runAtHour, int runAtMinute)
    {
        var nextRun = now.Date
            .AddHours(runAtHour)
            .AddMinutes(runAtMinute);

        if (now >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }
}
    

