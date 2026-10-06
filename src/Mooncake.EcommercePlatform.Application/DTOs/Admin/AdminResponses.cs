namespace Mooncake.EcommercePlatform.Application.DTOs.Admin;

using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

public sealed record AdminDashboardResponse(int TotalUsers, int ActiveUsers, int PendingSuppliers, int ActiveShops,
    int TotalOrders, decimal PaidGmv, int PendingPayments, int DisputedContracts,
    IReadOnlyList<AdminTopSupplierResponse> TopSuppliers, IReadOnlyList<AdminTopProductResponse> TopProducts);
public sealed record AdminTopSupplierResponse(Guid SupplierId, string? CompanyName, decimal SalesAmount);
public sealed record AdminTopProductResponse(Guid ProductId, string ProductName, int UnitsSold, decimal SalesAmount);
public sealed record SystemConfigResponse(string Key, string ValueJson, string? Description, DateTime UpdatedAtUtc);
public sealed record AuditLogResponse(Guid Id, Guid? ActorUserId, string Action, string EntityName, string? EntityId,
    string? MetadataJson, string? IpAddress, DateTime CreatedAtUtc);
public sealed record AdminShopResponse(ShopResponse Shop, int ProductCount);
public sealed record AdminUserUpdateRequest(bool? IsActive, UserRole? Role);
