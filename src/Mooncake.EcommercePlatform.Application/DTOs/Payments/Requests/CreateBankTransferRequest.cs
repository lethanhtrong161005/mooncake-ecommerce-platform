namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

/// <summary>Customer request to initiate a VietQR bank transfer payment.</summary>
public sealed record CreateBankTransferRequest
{
    public Guid? OrderId { get; init; }
    public Guid? ContractId { get; init; }
    public Guid? MilestoneId { get; init; }
}

