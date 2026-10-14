namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Closes RFQs whose bidding deadline has passed.</summary>
public sealed class RfqDeadlineJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "RFQ deadline maintenance";
    public TimeSpan Interval => TimeSpan.FromMinutes(1);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRfqRepository>();
        await repository.CloseExpiredRfqsAsync(cancellationToken);
    }
}
