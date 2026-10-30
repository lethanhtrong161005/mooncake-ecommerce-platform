namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implementation of <see cref="IAiChatService"/> supporting RAG product recommendations and cultural consultation.</summary>
public class AiChatService : IAiChatService
{
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IProductCatalogRepository _productCatalogRepository;
    private readonly IGeminiGateway _geminiGateway;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IAiConversationRepository aiConversationRepository,
        IProductCatalogRepository productCatalogRepository,
        IGeminiGateway geminiGateway,
        ILogger<AiChatService> logger)
    {
        _aiConversationRepository = aiConversationRepository;
        _productCatalogRepository = productCatalogRepository;
        _geminiGateway = geminiGateway;
        _logger = logger;
    }

    public async Task<AiSessionResponse> StartSessionAsync(Guid? userId, StartAiSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ExistingSessionToken))
        {
            var existing = await _aiConversationRepository.GetBySessionTokenAsync(request.ExistingSessionToken, cancellationToken);
            if (existing != null)
            {
                return new AiSessionResponse
                {
                    ConversationId = existing.Id,
                    SessionToken = existing.SessionToken,
                    ConversationType = existing.ConversationType,
                    CreatedAt = existing.CreatedAt
                };
            }
        }

        var newSession = new AiConversation
        {
            UserId = userId ?? Guid.Empty,
            SessionToken = $"aic-{Guid.NewGuid():N}",
            ConversationType = request.ConversationType,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _aiConversationRepository.CreateConversationAsync(newSession, cancellationToken);

        return new AiSessionResponse
        {
            ConversationId = created.Id,
            SessionToken = created.SessionToken,
            ConversationType = created.ConversationType,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<AiMessageResponse> ProcessMessageAsync(Guid? userId, SendAiMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            throw new HttpException(400, "Session token is required.", "INVALID_SESSION_TOKEN");
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 1000)
        {
            throw new HttpException(400, "Message content must be between 1 and 1000 characters.", "INVALID_MESSAGE_CONTENT");
        }

        var conversation = await _aiConversationRepository.GetBySessionTokenAsync(request.SessionToken, cancellationToken);
        if (conversation == null)
        {
            conversation = await _aiConversationRepository.CreateConversationAsync(new AiConversation
            {
                UserId = userId ?? Guid.Empty,
                SessionToken = request.SessionToken,
                ConversationType = request.ConversationType,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        // 1. Record User Message
        var userMessage = new AiMessage
        {
            ConversationId = conversation.Id,
            Role = AiMessageRole.User,
            Content = request.Content.Trim(),
            ModelUsed = "user-input",
            CreatedAt = DateTime.UtcNow
        };
        await _aiConversationRepository.AddMessageAsync(userMessage, cancellationToken);

        // 2. RAG Catalog Retrieval: Search real products based on budget and query
        var recommendedProducts = new List<AiProductRecommendationDto>();
        var catalogContextBuilder = new StringBuilder();

        try
        {
            var searchQuery = new ProductSearchQuery
            {
                Page = 1,
                PageSize = 4,
                MaxPrice = request.TargetBudget > 0 ? request.TargetBudget : null,
                Sort = "reputation"
            };

            var searchResult = await _productCatalogRepository.SearchAsync(searchQuery, cancellationToken);
            if (searchResult?.Items != null && searchResult.Items.Count > 0)
            {
                foreach (var item in searchResult.Items)
                {
                    var badge = ReputationBadgeHelper.ResolveBadge(item.Shop.ReputationScore);
                    var recDto = new AiProductRecommendationDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Name,
                        ShopName = item.Shop.Name,
                        PrimaryImageUrl = item.PrimaryImageUrl,
                        BasePrice = item.MinPrice,
                        MinOrderQty = item.MinOrderQty,
                        ReputationScore = item.Shop.ReputationScore,
                        ReputationBadgeTitle = badge.Title,
                        RecommendationReason = $"Xưởng {item.Shop.Name} đạt huy hiệu {badge.Title} ({item.Shop.ReputationScore}đ uy tín)"
                    };
                    recommendedProducts.Add(recDto);

                    catalogContextBuilder.AppendLine($"- Product: '{item.Name}' by Bakery '{item.Shop.Name}' (Reputation: {item.Shop.ReputationScore}/100, Badge: {badge.Title}). Price: {item.MinPrice:N0} VND, Min Order: {item.MinOrderQty} boxes. Supports Custom Packaging: {item.SupportsCustomPackaging}.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve RAG catalog items for query '{Query}'. Proceeding without catalog injection.", request.Content);
        }

        // 3. Sliding Window History (Last 6 messages)
        var recentHistory = await _aiConversationRepository.GetRecentMessagesAsync(conversation.Id, 6, cancellationToken);
        var historyBuilder = new StringBuilder();
        foreach (var msg in recentHistory)
        {
            if (msg.Id != userMessage.Id)
            {
                historyBuilder.AppendLine($"[{msg.Role}]: {msg.Content}");
            }
        }

        // 4. Assemble System & User Prompt
        var systemInstruction = CulturalKnowledgeHelper.GetCulturalSystemPrompt();
        var promptBuilder = new StringBuilder();

        if (catalogContextBuilder.Length > 0)
        {
            promptBuilder.AppendLine("REAL CATALOG PRODUCTS IN STOCK (Recommend ONLY from this list when suggesting items):");
            promptBuilder.AppendLine(catalogContextBuilder.ToString());
            promptBuilder.AppendLine();
        }

        if (historyBuilder.Length > 0)
        {
            promptBuilder.AppendLine("CONVERSATION HISTORY:");
            promptBuilder.AppendLine(historyBuilder.ToString());
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"CURRENT USER QUESTION: {request.Content}");
        if (request.TargetBudget.HasValue)
        {
            promptBuilder.AppendLine($"Target Budget: {request.TargetBudget.Value:N0} VND/box");
        }
        if (request.TargetQuantity.HasValue)
        {
            promptBuilder.AppendLine($"Target Quantity: {request.TargetQuantity.Value} boxes");
        }

        // 5. Call Gemini Gateway
        string assistantContent;
        try
        {
            assistantContent = await _geminiGateway.GenerateContentAsync(systemInstruction, promptBuilder.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini Gateway: {Message}", ex.Message);
            assistantContent = string.Empty;
        }

        // 6. Graceful Fallback if Gemini is unavailable or empty
        if (string.IsNullOrWhiteSpace(assistantContent))
        {
            assistantContent = CulturalKnowledgeHelper.GetFallbackAdvisorResponse(request.Content);
        }

        // 7. Save Assistant Message
        var assistantMessage = new AiMessage
        {
            ConversationId = conversation.Id,
            Role = AiMessageRole.Assistant,
            Content = assistantContent,
            TokensUsed = Math.Max(250, assistantContent.Length / 4),
            ModelUsed = "gemini-flash-latest",
            CreatedAt = DateTime.UtcNow
        };
        await _aiConversationRepository.AddMessageAsync(assistantMessage, cancellationToken);

        // 8. Follow-up Suggestions
        var followUps = new List<string>
        {
            "Tư vấn set quà đối tác VIP ngân sách 500k",
            "Ý nghĩa set 4 bánh Tứ Quý & Loại trà phối kèm",
            "Bánh Trung Thu thực dưỡng ít đường cho người ăn kiêng",
            "Tạo yêu cầu báo giá RFQ cho 200 hộp"
        };

        return new AiMessageResponse
        {
            MessageId = assistantMessage.Id,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            TokensUsed = assistantMessage.TokensUsed ?? 300,
            ModelUsed = assistantMessage.ModelUsed ?? "gemini-flash-latest",
            CreatedAt = assistantMessage.CreatedAt,
            RecommendedProducts = recommendedProducts,
            SuggestedFollowUps = followUps
        };
    }

    public async Task<AiConversationHistoryResponse> GetHistoryAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var conversation = await _aiConversationRepository.GetBySessionTokenAsync(sessionToken, cancellationToken);
        if (conversation == null)
        {
            throw new HttpException(404, "Conversation session not found.", "AI_SESSION_NOT_FOUND");
        }

        var messages = await _aiConversationRepository.GetAllMessagesAsync(conversation.Id, cancellationToken);

        return new AiConversationHistoryResponse
        {
            ConversationId = conversation.Id,
            SessionToken = conversation.SessionToken,
            ConversationType = conversation.ConversationType,
            Messages = messages.Select(m => new AiHistoryItemDto
            {
                MessageId = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    public async Task EndSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var conversation = await _aiConversationRepository.GetBySessionTokenAsync(sessionToken, cancellationToken);
        if (conversation != null)
        {
            await _aiConversationRepository.EndConversationAsync(conversation.Id, cancellationToken);
        }
    }
}

