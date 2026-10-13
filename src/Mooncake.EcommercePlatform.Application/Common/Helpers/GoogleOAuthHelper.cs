namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application.Common.Options;

/// <summary>Creates opaque state, PKCE values, and Google authorization URLs.</summary>
public static class GoogleOAuthHelper
{
    public static string RandomToken(int bytes) =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(bytes));

    public static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string BuildUrl(GoogleOAuthOptions options, string state, string nonce, string verifier)
    {
        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256"
        };
        return $"{options.AuthorizationEndpoint}?{string.Join("&", parameters.Select(
            item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"))}";
    }
}
