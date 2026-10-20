namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;

public record VerifyRegisterOtpRequest(string? PhoneNumber, string? Otp);
