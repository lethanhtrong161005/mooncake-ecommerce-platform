namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for CustomPackaging mapping helpers.</summary>
public interface ICustomPackagingHelper
{
    CustomPackagingResponse ToResponse(CustomPackaging packaging);
}
