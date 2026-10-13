namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;

/// <summary>Exchanges a Google code and validates its signed OpenID Connect ID token.</summary>
public class GoogleIdentityClient(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<GoogleOAuthOptions> options) : IGoogleIdentityClient
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string JwksEndpoint = "https://www.googleapis.com/oauth2/v3/certs";
    private const string SigningKeysCacheKey = "google-oidc-signing-keys";
    private const string InvalidClientError = "invalid_client";
    private readonly GoogleOAuthOptions _options = options.Value;

    public async Task<GoogleIdentity> ExchangeAsync(
        string code, string codeVerifier, string nonce, CancellationToken cancellationToken)
    {
        try
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier
            });
            using var response = await httpClient.PostAsync(TokenEndpoint, form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (errorBody.Contains(InvalidClientError, StringComparison.Ordinal))
                    throw new HttpException(500, MessageConstant.ServerError);
                throw response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Unauthorized
                    ? new HttpException(401, MessageConstant.GoogleInvalidIdentity)
                    : new HttpException(502, MessageConstant.GoogleUnavailable);
            }

            using var payload = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            if (!payload.RootElement.TryGetProperty("id_token", out var idTokenElement) ||
                idTokenElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(idTokenElement.GetString()))
                throw new HttpException(401, MessageConstant.GoogleInvalidIdentity);

            var keys = await cache.GetOrCreateAsync(SigningKeysCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                var jwks = await httpClient.GetStringAsync(JwksEndpoint, cancellationToken);
                return new JsonWebKeySet(jwks).GetSigningKeys();
            });
            if (keys is null || keys.Count == 0)
                throw new HttpException(502, MessageConstant.GoogleUnavailable);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                ValidateAudience = true,
                ValidAudience = _options.ClientId,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
            };
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
                idTokenElement.GetString(), parameters, out _);
            var subject = principal.FindFirstValue("sub");
            var email = principal.FindFirstValue("email");
            var verified = principal.FindFirstValue("email_verified");
            var returnedNonce = principal.FindFirstValue("nonce");
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email) ||
                !string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(returnedNonce, nonce, StringComparison.Ordinal))
                throw new HttpException(401, MessageConstant.GoogleInvalidIdentity);

            return new GoogleIdentity(subject, email.Trim(),
                principal.FindFirstValue("name"), principal.FindFirstValue("picture"));
        }
        catch (HttpException)
        {
            throw;
        }
        catch (SecurityTokenException)
        {
            throw new HttpException(401, MessageConstant.GoogleInvalidIdentity);
        }
        catch (ArgumentException)
        {
            throw new HttpException(401, MessageConstant.GoogleInvalidIdentity);
        }
        catch (FormatException)
        {
            throw new HttpException(401, MessageConstant.GoogleInvalidIdentity);
        }
        catch (JsonException)
        {
            throw new HttpException(502, MessageConstant.GoogleUnavailable);
        }
        catch (HttpRequestException)
        {
            throw new HttpException(502, MessageConstant.GoogleUnavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpException(502, MessageConstant.GoogleUnavailable);
        }
    }
}
