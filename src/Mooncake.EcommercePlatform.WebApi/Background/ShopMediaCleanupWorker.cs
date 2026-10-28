namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

public sealed class ShopMediaCleanupWorker(IServiceScopeFactory scopes, ILogger<ShopMediaCleanupWorker> logger) : BackgroundService
{
    private const string CleanupFailure = "Shop media maintenance failed; pending assets remain retryable.";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try {
                using var scope = scopes.CreateScope();
                await ShopMediaCleanupHelper.RunAsync(scope.ServiceProvider.GetRequiredService<IShopRepository>(),
                    scope.ServiceProvider.GetRequiredService<IShopMediaGateway>(),
                    scope.ServiceProvider.GetRequiredService<IDateTimeProvider>(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { logger.LogWarning(CleanupFailure); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
