namespace Mooncake.EcommercePlatform.WebApi.Configurations;

/// <summary>Frontend origins permitted to call the API.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string FrontendPolicyName = "frontend";

    public string[] AllowedOrigins { get; set; } = [];
}
