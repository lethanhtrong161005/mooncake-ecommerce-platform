namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Idempotently auto-confirms wholesale orders shipped with Proof-of-Delivery photos created 72h or more ago without open disputes.</summary>
public sealed class OrderDeliveryAutoConfirmJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Bulk order delivery auto-confirmation";
    public TimeSpan Interval => TimeSpan.FromMinutes(15);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEnterpriseOrderRepository>();
        await repository.AutoConfirmDeliveredOrdersAsync(cancellationToken);
    }
}
