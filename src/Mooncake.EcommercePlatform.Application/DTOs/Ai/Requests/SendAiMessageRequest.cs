namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to send a prompt to the AI conversation.</summary>
public record SendAiMessageRequest
{
    [Required]
    public string Message { get; init; } = string.Empty;
}
