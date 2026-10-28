namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Projects committed catalog outbox events into a rebuildable public search model.</summary>
public sealed class ProductSearchProjectionWorker(IServiceScopeFactory scopes, IDateTimeProvider clock,
    ILogger<ProductSearchProjectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IProductCatalogRepository>();
                var processed = await repository.ProjectPendingAsync(50, clock.UtcNow, stoppingToken);
                await Task.Delay(processed == 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromMilliseconds(100), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception failure)
            {
                logger.LogError(failure, "Product search projection batch failed. FailureType={FailureType}", failure.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
