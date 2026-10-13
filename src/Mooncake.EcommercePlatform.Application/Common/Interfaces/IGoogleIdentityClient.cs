namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Exchanges and validates a Google authorization code.</summary>
public interface IGoogleIdentityClient
{
    Task<GoogleIdentity> ExchangeAsync(
        string code, string codeVerifier, string nonce, CancellationToken cancellationToken);
}

/// <summary>Claims from a fully validated Google ID token.</summary>
public sealed record GoogleIdentity(string Subject, string Email, string? Name, string? Picture);
