namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

public record VerifyRegisterOtpResponse(string PhoneNumber, string VerificationToken, int ExpiresInSeconds);
