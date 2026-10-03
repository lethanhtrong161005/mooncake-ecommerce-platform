namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps CustomPackaging entities to DTOs.</summary>
public class CustomPackagingHelper : ICustomPackagingHelper
{
    public CustomPackagingResponse ToResponse(CustomPackaging packaging) =>
        new(
            packaging.Id,
            packaging.CustomerId,
            packaging.Name,
            packaging.LogoUrl,
            packaging.DesignNotes,
            packaging.CreatedAtUtc,
            packaging.UpdatedAtUtc
        );
}
