namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Scoped, restart-safe outbox consumer; no SMTP or push occurs in the original command transaction.</summary>
public sealed class VerificationOutboxWorker(IServiceScopeFactory scopes, ILogger<VerificationOutboxWorker> logger) : BackgroundService
{
    private const string ErrorMessage = "Verification delivery is unavailable; the durable outbox will be retried.";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var processed = await scope.ServiceProvider.GetRequiredService<IVerificationNotificationService>().DeliverNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch
            {
                logger.LogWarning(ErrorMessage);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
