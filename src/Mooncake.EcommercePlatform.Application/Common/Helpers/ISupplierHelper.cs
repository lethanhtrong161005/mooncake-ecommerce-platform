namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Admin.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for Supplier mapping helpers.</summary>
public interface ISupplierHelper
{
    SupplierResponse ToResponse(Supplier supplier);
}
