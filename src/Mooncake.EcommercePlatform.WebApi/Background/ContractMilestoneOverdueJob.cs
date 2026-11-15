namespace Mooncake.EcommercePlatform.WebApi.Background;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Marks overdue contract milestones and sends idempotent T-7/T-3/T-1 due-date reminders.</summary>
public sealed class ContractMilestoneOverdueJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Contract milestone overdue scan & due reminders";
    public TimeSpan Interval => TimeSpan.FromMinutes(15);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IContractRepository>();
        await repository.ProcessMilestoneDeadlinesAsync(cancellationToken);
    }
}
