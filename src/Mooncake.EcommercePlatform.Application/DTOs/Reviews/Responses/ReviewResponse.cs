namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Review projection returned to callers.</summary>
public record ReviewResponse(
    long Id,
    long CustomerId,
    string CustomerName,
    long SupplierId,
    string SupplierBusinessName,
    long? OrderId,
    long? ContractId,
    short Rating,
    string? Comment,
    DateTime CreatedAtUtc
);

/// <summary>Reputation audit log entry projection.</summary>
public record ReputationLogResponse(
    long Id,
    long SupplierId,
    ReputationEventType EventType,
    int ScoreDelta,
    long? ReviewId,
    long? ContractId,
    string? Reason,
    DateTime CreatedAtUtc
);
