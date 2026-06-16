namespace R2SentinelBak.Features.Scheduling;

public class Worker(
    ILogger<Worker> logger,
    BackupOrchestrator orchestrator,
    IOptions<AppOptions> appOptions,
    BackupRunOptions runOptions) : BackgroundService
{
    private readonly int _runAtHour = ValidateRange(appOptions.Value.Schedule.RunAtHour, 0, 23, "AppOptions:Schedule:RunAtHour");
    private readonly int _runAtMinute = ValidateRange(appOptions.Value.Schedule.RunAtMinute, 0, 59, "AppOptions:Schedule:RunAtMinute");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (runOptions.RunOnce)
        {
            logger.LogInformation("Run-once mode enabled. Starting backup orchestration immediately.");
            try 
            {
                await orchestrator.RunAsync(stoppingToken); 
            }
            catch (Exception ex) 
            { 
                logger.LogCritical(ex, "Run-once backup orchestration failed."); 
            }
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = GetNextRunTime(DateTime.Now, _runAtHour, _runAtMinute);
            logger.LogInformation("Next backup run scheduled at {NextRun}.", nextRun);
            await Task.Delay(nextRun - DateTime.Now, stoppingToken);

            try 
            {
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

    public static DateTime GetNextRunTime(DateTime now, int hour, int minute)
    {
        var next = now.Date.AddHours(hour).AddMinutes(minute);
        return now >= next ? next.AddDays(1) : next;
    }

    public static int ValidateRange(int value, int min, int max, string key)
    {
        if (value < min || value > max)
            throw new InvalidOperationException($"{key} must be between {min} and {max}.");

        return value;
    }

}