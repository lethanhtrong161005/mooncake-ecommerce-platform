namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Gateway for sending requests to Google Gemini generative language API.</summary>
public interface IGeminiGateway
{
    /// <summary>Generates content from Gemini using system instructions and user/assistant messages.</summary>
    Task<string> GenerateContentAsync(string systemInstruction, string userPrompt, CancellationToken cancellationToken = default);
}

