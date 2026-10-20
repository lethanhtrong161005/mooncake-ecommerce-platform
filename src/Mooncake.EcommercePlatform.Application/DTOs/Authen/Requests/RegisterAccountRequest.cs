namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;

public record RegisterAccountRequest(string? PhoneNumber, string? VerificationToken,
    string? FullName, string? Password, string? ConfirmPassword);
