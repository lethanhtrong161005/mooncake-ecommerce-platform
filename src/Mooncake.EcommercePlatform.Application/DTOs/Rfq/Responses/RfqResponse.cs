namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Individual product line requested in an RFQ.</summary>
public record RfqItemResponse(
    long Id,
    long RfqId,
    long? ProductId,
    string? ProductName,
    string ItemName,
    string? Specification,
    int Quantity,
    long? CustomPackagingId,
    string? CustomPackagingName,
    string? Note
);

/// <summary>RFQ projection returned to callers.</summary>
public record RfqResponse(
    long Id,
    long CustomerId,
    string CustomerName,
    string Title,
    string? Description,
    RfqVisibility Visibility,
    RfqStatus Status,
    DateTime? QuoteDeadline,
    DateOnly? RequiredDeliveryDate,
    string DeliveryAddress,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<RfqItemResponse> Items,
    int QuotationCount = 0
);
