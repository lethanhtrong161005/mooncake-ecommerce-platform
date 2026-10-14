namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Expires pending payments whose configured payment window has elapsed.</summary>
public sealed class PaymentExpiryJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Payment expiry maintenance";
    public TimeSpan Interval => TimeSpan.FromMinutes(5);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        await repository.ExpirePendingPaymentsAsync(cancellationToken);
    }
}
