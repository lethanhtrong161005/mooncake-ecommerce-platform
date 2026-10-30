namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class AiChatManagementTests
{
    private readonly Mock<IAiConversationRepository> conversationRepo = new();
    private readonly Mock<IProductCatalogRepository> productRepo = new();
    private readonly Mock<IGeminiGateway> geminiGateway = new();

    private readonly AiChatService chatService;
    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid conversationId = Guid.NewGuid();
    private readonly string sessionToken = "aic-test-token-001";

    public AiChatManagementTests()
    {
        chatService = new AiChatService(
            conversationRepo.Object,
            productRepo.Object,
            geminiGateway.Object,
            NullLogger<AiChatService>.Instance);
    }

    [Fact]
    public async Task StartSessionAsync_CreatesNewSession_WhenNoExistingTokenProvided()
    {
        conversationRepo.Setup(r => r.CreateConversationAsync(It.IsAny<AiConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConversation c, CancellationToken _) => c);

        var result = await chatService.StartSessionAsync(userId, new StartAiSessionRequest
        {
            ConversationType = AiConversationType.ProductInfo
        });

        Assert.NotNull(result);
        Assert.StartsWith("aic-", result.SessionToken);
        Assert.Equal(AiConversationType.ProductInfo, result.ConversationType);
    }

    [Fact]
    public async Task StartSessionAsync_ReturnsExistingSession_WhenTokenMatches()
    {
        conversationRepo.Setup(r => r.GetBySessionTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiConversation
            {
                Id = conversationId,
                SessionToken = sessionToken,
                ConversationType = AiConversationType.MooncakeHistory,
                CreatedAt = DateTime.UtcNow
            });

        var result = await chatService.StartSessionAsync(userId, new StartAiSessionRequest
        {
            ExistingSessionToken = sessionToken
        });

        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal(sessionToken, result.SessionToken);
        Assert.Equal(AiConversationType.MooncakeHistory, result.ConversationType);
    }

    [Fact]
    public async Task ProcessMessageAsync_ThrowsBadRequest_WhenContentIsWhitespace()
    {
        await Assert.ThrowsAsync<HttpException>(() => chatService.ProcessMessageAsync(userId, new SendAiMessageRequest
        {
            SessionToken = sessionToken,
            Content = "   "
        }));
    }

    [Fact]
    public async Task ProcessMessageAsync_RetrievesMatchingCatalogProducts_AndCallsGemini()
    {
        var conversation = new AiConversation
        {
            Id = conversationId,
            SessionToken = sessionToken,
            ConversationType = AiConversationType.ProductInfo
        };

        conversationRepo.Setup(r => r.GetBySessionTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var shopSummary = new ProductShopSummaryResponse(Guid.NewGuid(), "xuong-trang-an", "Xưởng Bánh Tràng An Nghệ Nhân", 88.5m, true, "Hà Nội");
        var categorySummary = new ProductCategorySummaryResponse(Guid.NewGuid(), "Bánh nướng cao cấp");

        var searchResponse = new ProductSearchResponse(
            new List<ProductSearchCardResponse>
            {
                new(Guid.NewGuid(), "Hộp Quà VIP Bát Bửu Thượng Hạng", shopSummary, categorySummary, "https://example.com/img.jpg",
                    480000m, 850000m, 10, "hộp", true, ProductStatus.Active, 4.9m, 42, DateTime.UtcNow)
            },
            1, 10, 1, new ProductSearchFacetsResponse([], [], 480000m, 850000m, 1, 1), "reputation", DateTime.UtcNow);

        productRepo.Setup(r => r.SearchAsync(It.IsAny<ProductSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        conversationRepo.Setup(r => r.GetRecentMessagesAsync(conversationId, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiMessage>());

        geminiGateway.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Dạ với ngân sách 500k, set bánh VIP Bát Bửu từ Xưởng Tràng An là lựa chọn hoàn hảo!");

        var result = await chatService.ProcessMessageAsync(userId, new SendAiMessageRequest
        {
            SessionToken = sessionToken,
            Content = "Tư vấn set quà 500k cho đối tác VIP",
            TargetBudget = 500000m
        });

        Assert.NotNull(result);
        Assert.Contains("set bánh VIP Bát Bửu", result.Content);
        Assert.Single(result.RecommendedProducts);
        Assert.Equal("Hộp Quà VIP Bát Bửu Thượng Hạng", result.RecommendedProducts[0].ProductName);
        Assert.Equal("Đối tác Vàng", result.RecommendedProducts[0].ReputationBadgeTitle);
    }

    [Fact]
    public async Task ProcessMessageAsync_FallsBackGracefully_WhenGeminiFailsOrReturnsEmpty()
    {
        var conversation = new AiConversation
        {
            Id = conversationId,
            SessionToken = sessionToken,
            ConversationType = AiConversationType.MooncakeHistory
        };

        conversationRepo.Setup(r => r.GetBySessionTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        conversationRepo.Setup(r => r.GetRecentMessagesAsync(conversationId, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiMessage>());

        geminiGateway.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty); // Gemini down / empty

        var result = await chatService.ProcessMessageAsync(userId, new SendAiMessageRequest
        {
            SessionToken = sessionToken,
            Content = "Giải thích ý nghĩa của set 4 bánh Tứ Quý"
        });

        Assert.NotNull(result);
        Assert.Contains("Tứ Quý", result.Content);
        Assert.Contains("Xuân - Hạ - Thu - Đông", result.Content);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllMessages_WhenSessionExists()
    {
        var conversation = new AiConversation
        {
            Id = conversationId,
            SessionToken = sessionToken,
            ConversationType = AiConversationType.General
        };

        conversationRepo.Setup(r => r.GetBySessionTokenAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        conversationRepo.Setup(r => r.GetAllMessagesAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiMessage>
            {
                new() { Id = Guid.NewGuid(), ConversationId = conversationId, Role = AiMessageRole.User, Content = "Chào AI" },
                new() { Id = Guid.NewGuid(), ConversationId = conversationId, Role = AiMessageRole.Assistant, Content = "Chào bạn! Tôi có thể giúp gì?" }
            });

        var history = await chatService.GetHistoryAsync(sessionToken);

        Assert.NotNull(history);
        Assert.Equal(2, history.Messages.Count);
        Assert.Equal("Chào AI", history.Messages[0].Content);
    }
}

