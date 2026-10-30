namespace Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>Options for Google Gemini Generative AI Gateway.</summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-flash-latest";

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxTokens { get; set; } = 2048;

    public double Temperature { get; set; } = 0.7;
}

