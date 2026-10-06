namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Runs registered recurring jobs independently and retries failures on their next interval.</summary>
public sealed class RecurringJobRunner(IEnumerable<IRecurringJob> jobs, ILogger<RecurringJobRunner> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(jobs.Select(job => RunJobAsync(job, stoppingToken)));

    private async Task RunJobAsync(IRecurringJob job, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(job.Interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try { await job.ExecuteAsync(cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Recurring job {JobName} failed and will retry next interval.", job.Name); }
        }
    }
}
