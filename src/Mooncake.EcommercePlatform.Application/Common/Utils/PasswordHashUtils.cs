namespace Mooncake.EcommercePlatform.Application.Common.Utils;

using System.Security.Cryptography;
using System.Text;

/// <summary>Verifies PBKDF2, bcrypt, and legacy Base64 password hashes.</summary>
public static class PasswordHashUtils
{
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Prefix = "pbkdf2-sha256";

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (storedHash.StartsWith("$2a$", StringComparison.Ordinal)
            || storedHash.StartsWith("$2b$", StringComparison.Ordinal)
            || storedHash.StartsWith("$2y$", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        if (storedHash.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)
                || iterations < 100_000 || iterations > 2_000_000)
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                if (salt.Length != SaltBytes || expected.Length != HashBytes)
                    return false;
                var actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                needsUpgrade = iterations < Iterations;
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Existing users were stored as Base64(UTF8(password)). Upgrade after a valid login.
        var legacy = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash), Encoding.UTF8.GetBytes(legacy));
        needsUpgrade = matches;
        return matches;
    }
}
