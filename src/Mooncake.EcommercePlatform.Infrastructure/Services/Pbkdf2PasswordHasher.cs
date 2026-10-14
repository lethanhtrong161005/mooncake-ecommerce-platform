namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;

/// <summary>Uses PBKDF2-SHA256 with a per-password random salt.</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string CurrentAlgorithm = "pbkdf2-sha256$600000$";

    public string Hash(string password) => PasswordHashUtils.Hash(password);

    public bool Verify(string password, string passwordHash) =>
        PasswordHashUtils.Verify(password, passwordHash, out _);

    public bool NeedsRehash(string passwordHash) =>
        !passwordHash.StartsWith(CurrentAlgorithm, StringComparison.Ordinal);
}
