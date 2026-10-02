namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to start a new AI conversation.</summary>
public record CreateAiConversationRequest
{
    [MaxLength(255)]
    public string? Title { get; init; }

    /// <summary>Optional initial message from the user.</summary>
    public string? InitialMessage { get; init; }
}
