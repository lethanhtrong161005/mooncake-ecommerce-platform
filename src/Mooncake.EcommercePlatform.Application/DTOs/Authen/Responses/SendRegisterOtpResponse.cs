namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

public record SendRegisterOtpResponse(string PhoneNumber, int CooldownSeconds, int ExpiresInSeconds);
