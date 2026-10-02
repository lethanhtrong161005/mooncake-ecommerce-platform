namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Admin.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Supplier entities to DTOs.</summary>
public class SupplierHelper : ISupplierHelper
{
    public SupplierResponse ToResponse(Supplier supplier) =>
        new(
            supplier.Id,
            supplier.UserId,
            supplier.BusinessName,
            supplier.Description,
            supplier.Address,
            supplier.TaxCode,
            supplier.IsVerified,
            supplier.ReputationScore,
            supplier.User?.Email ?? string.Empty,
            supplier.User?.FullName ?? string.Empty,
            supplier.User?.Phone,
            supplier.CreatedAtUtc
        );
}
