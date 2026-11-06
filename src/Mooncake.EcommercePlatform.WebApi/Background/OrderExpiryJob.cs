namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Expires overdue bulk order response, delivery proposal, deposit payment, and packaging reservation deadlines idempotently.</summary>
public sealed class OrderExpiryJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Bulk order SLA & reservation expiry maintenance";
    public TimeSpan Interval => TimeSpan.FromMinutes(1);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEnterpriseOrderRepository>();
        await repository.ExpireOverdueOrdersAsync(cancellationToken);
    }
}
