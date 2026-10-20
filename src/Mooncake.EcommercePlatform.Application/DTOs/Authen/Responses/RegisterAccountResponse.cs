namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

public record RegisterAccountResponse(Guid UserId, string Phone, string FullName, UserRole Role);
