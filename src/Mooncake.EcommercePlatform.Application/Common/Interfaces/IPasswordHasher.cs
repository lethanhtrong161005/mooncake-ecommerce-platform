namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Contract for password hashing and verification.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes the specified plain-text password.</summary>
    string HashPassword(string password);

    /// <summary>Verifies that the plain-text password matches the stored hash.</summary>
    bool VerifyPassword(string password, string passwordHash);
}
