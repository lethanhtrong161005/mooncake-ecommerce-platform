namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record RfqItemResponse(Guid Id, string? ProductName, string? Description, int Quantity, string? Unit, decimal? TargetPrice, string? Specifications, string? PackagingRequirements, bool NeedsCustomPackaging);
public sealed record RfqResponse(Guid Id, string RfqNumber, string? Title, string? Description, string? DeliveryAddress, DateOnly? DeliveryDateRequired, decimal? BudgetRangeMin, decimal? BudgetRangeMax, DateTime? BidDeadline, RfqStatus Status, IReadOnlyList<RfqItemResponse> Items, int InvitationCount, int BidCount);
public sealed record BidItemResponse(Guid RfqItemId, string? ProductName, int Quantity, string? Unit, decimal UnitPrice, decimal Subtotal, string? Notes);
public sealed record BidResponse(Guid Id, string BidNumber, Guid SupplierId, decimal? TotalPrice, int? EstimatedDeliveryDays, int ValidityDays, DateTime SubmittedAt, bool IsExpired, BidStatus Status, IReadOnlyList<BidItemResponse> Items);
