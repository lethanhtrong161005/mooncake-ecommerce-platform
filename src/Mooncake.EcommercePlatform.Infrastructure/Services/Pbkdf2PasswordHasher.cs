namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Security.Cryptography;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Uses PBKDF2-SHA256 with a per-password random salt.</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 310_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Algorithm = "PBKDF2-SHA256";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations) || iterations is < 100_000 or > 1_000_000)
            return VerifyLegacyHash(password, passwordHash);

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string passwordHash) => !passwordHash.StartsWith($"{Algorithm}${Iterations}$", StringComparison.Ordinal);

    private static bool VerifyLegacyHash(string password, string passwordHash)
    {
        try
        {
            var legacyBytes = Convert.FromBase64String(passwordHash);
            return CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(password), legacyBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
