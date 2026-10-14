namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Hashes and verifies user passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
    bool NeedsRehash(string passwordHash);
}
