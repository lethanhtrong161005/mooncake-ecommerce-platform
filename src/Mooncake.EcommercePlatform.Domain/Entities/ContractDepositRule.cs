namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the ContractDepositRule domain entity.</summary>
public class ContractDepositRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal DepositPercentage { get; set; }

    public bool IsMandatory { get; set; }

    public bool AppliesToAll { get; set; }

    public decimal? MinContractValue { get; set; }

    public int PaymentDeadlineHours { get; set; }
}
