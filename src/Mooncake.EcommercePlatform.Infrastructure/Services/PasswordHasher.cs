namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>BCrypt implementation of <see cref="IPasswordHasher"/>.</summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
