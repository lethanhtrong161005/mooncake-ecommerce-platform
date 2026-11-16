namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Periodically scans food-safety certificates (ATTP) to warn suppliers T-30 before expiry and suspend shops with expired licenses.</summary>
public sealed class SupplierLicenseExpiryJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Supplier license expiry monitor";
    public TimeSpan Interval => TimeSpan.FromHours(1);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();
        await repository.ProcessLicenseExpiriesAsync(cancellationToken);
    }
}
