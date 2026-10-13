namespace Mooncake.EcommercePlatform.Application.Common.Options;

/// <summary>Google OpenID Connect settings.</summary>
public sealed class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public int StateLifetimeMinutes { get; set; } = 5;
}
