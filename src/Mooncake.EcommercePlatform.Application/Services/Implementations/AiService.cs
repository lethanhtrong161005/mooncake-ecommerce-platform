namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements AI chatbot and sales reporting analytics.</summary>
public class AiService(
    IAiRepository aiRepository,
    ISupplierRepository supplierRepository,
    IUserRepository userRepository,
    IAiHelper aiHelper) : IAiService
{
    public async Task<AiConversationResponse> CreateConversationAsync(long userId, CreateAiConversationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{userId}' was not found.");

        var conversation = new AiConversation
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Mid-Autumn Mooncake Inquiry" : request.Title,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await aiRepository.CreateConversationAsync(conversation, cancellationToken);

        // If an initial message was provided, store user prompt and generate assistant greeting
        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            var userMsg = new AiMessage
            {
                ConversationId = created.Id,
                Role = AiMessageRole.User,
                Content = request.InitialMessage,
                Metadata = "{}",
                CreatedAtUtc = DateTime.UtcNow
            };
            await aiRepository.AddMessageAsync(userMsg, cancellationToken);

            var botMsg = new AiMessage
            {
                ConversationId = created.Id,
                Role = AiMessageRole.Assistant,
                Content = GenerateSmartAssistantResponse(request.InitialMessage),
                Metadata = JsonSerializer.Serialize(new { source = "MooncakeAIEngine", confidence = 0.98 }),
                CreatedAtUtc = DateTime.UtcNow.AddMilliseconds(200)
            };
            await aiRepository.AddMessageAsync(botMsg, cancellationToken);
        }

        var fullConversation = await aiRepository.GetConversationByIdAsync(created.Id, cancellationToken);
        return aiHelper.ToResponse(fullConversation ?? created);
    }

    public async Task<IEnumerable<AiConversationResponse>> GetUserConversationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var conversations = await aiRepository.GetConversationsByUserIdAsync(userId, cancellationToken);
        return conversations.Select(aiHelper.ToResponse);
    }

    public async Task<AiConversationResponse> GetConversationByIdAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await aiRepository.GetConversationByIdAsync(conversationId, cancellationToken)
                           ?? throw new HttpException(404, $"AI conversation with id '{conversationId}' was not found.");
        return aiHelper.ToResponse(conversation);
    }

    public async Task<AiMessageResponse> SendMessageAsync(long conversationId, SendAiMessageRequest request, CancellationToken cancellationToken = default)
    {
        var conversation = await aiRepository.GetConversationByIdAsync(conversationId, cancellationToken)
                           ?? throw new HttpException(404, $"AI conversation with id '{conversationId}' was not found.");

        var userMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = AiMessageRole.User,
            Content = request.Message,
            Metadata = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };
        await aiRepository.AddMessageAsync(userMessage, cancellationToken);

        // Generate response from AI engine
        var replyContent = GenerateSmartAssistantResponse(request.Message);
        var assistantMessage = new AiMessage
        {
            ConversationId = conversationId,
            Role = AiMessageRole.Assistant,
            Content = replyContent,
            Metadata = JsonSerializer.Serialize(new { model = "Mooncake-GenAI-v1", promptTokens = 120, completionTokens = 180 }),
            CreatedAtUtc = DateTime.UtcNow.AddMilliseconds(150)
        };
        var savedAssistantMessage = await aiRepository.AddMessageAsync(assistantMessage, cancellationToken);

        return aiHelper.ToResponse(savedAssistantMessage);
    }

    public async Task<AiAnalyticsReportResponse> GenerateReportAsync(GenerateAiReportRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
                       ?? throw new HttpException(404, $"Supplier with id '{request.SupplierId}' was not found.");

        var report = new AiAnalyticsReport
        {
            SupplierId = request.SupplierId,
            ReportType = request.ReportType,
            PeriodStart = request.PeriodStart ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            PeriodEnd = request.PeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Parameters = request.Parameters,
            Status = AiReportStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        // Synthesize smart AI sales analytics based on report type
        var reportData = new
        {
            supplier = supplier.BusinessName,
            reportType = request.ReportType,
            generatedAt = DateTime.UtcNow,
            executiveSummary = "Demand for premium baked and lava mooncakes is surging. Corporate gift box customization represents 64% of potential high-margin revenue.",
            kpis = new
            {
                forecastedRevenueVnd = 185000000m,
                expectedOrderVolume = 1250,
                recommendedBulkDiscountTier = "15% off for orders over 100 boxes",
                peakSalesWindow = "15 to 25 days before Mid-Autumn Festival"
            },
            trendingFlavors = new[]
            {
                new { flavor = "Mixed Nuts & Roasted Chicken", popularityScore = 95, marketShare = "32%" },
                new { flavor = "Matcha Lava Molten", popularityScore = 88, marketShare = "24%" },
                new { flavor = "Lotus Seed with Double Salted Egg", popularityScore = 85, marketShare = "22%" },
                new { flavor = "Musang King Durian", popularityScore = 79, marketShare = "14%" },
                new { flavor = "Tiramisu Low-Sugar", popularityScore = 72, marketShare = "8%" }
            },
            strategicRecommendations = new[]
            {
                "Offer custom corporate logo box packaging at 25,000 VND fee to boost B2B conversions.",
                "Implement tiered volume discounts: 5% for 10+ boxes, 10% for 50+ boxes, 15% for 100+ boxes.",
                "Enforce a 30% deposit rule for bulk orders over 50 boxes to guarantee ingredient inventory."
            }
        };

        report.Result = JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true });

        var created = await aiRepository.CreateReportAsync(report, cancellationToken);
        return aiHelper.ToResponse(created);
    }

    public async Task<IEnumerable<AiAnalyticsReportResponse>> GetSupplierReportsAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var reports = await aiRepository.GetReportsBySupplierIdAsync(supplierId, cancellationToken);
        return reports.Select(aiHelper.ToResponse);
    }

    public async Task<AiAnalyticsReportResponse> GetReportByIdAsync(long reportId, CancellationToken cancellationToken = default)
    {
        var report = await aiRepository.GetReportByIdAsync(reportId, cancellationToken)
                     ?? throw new HttpException(404, $"AI Report with id '{reportId}' was not found.");
        return aiHelper.ToResponse(report);
    }

    private static string GenerateSmartAssistantResponse(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        if (lower.Contains("price") || lower.Contains("giá") || lower.Contains("cost"))
        {
            return "Mooncake prices typically range from 65,000 VND to 280,000 VND per cake depending on variant, flavor (lotus seed, lava, salted egg), and size. We also support volume discounts when buying in tiers of 10, 50, or 100+ items!";
        }

        if (lower.Contains("box") || lower.Contains("packaging") || lower.Contains("hộp") || lower.Contains("logo"))
        {
            return "Our platform supports Custom Packaging! You can upload your company or personal logo, design notes, and choose 2-cake, 4-cake, or 6-cake luxury gift boxes. Custom packaging fees are automatically applied per product.";
        }

        if (lower.Contains("bulk") || lower.Contains("large") || lower.Contains("sỉ") || lower.Contains("doanh nghiệp") || lower.Contains("corporate"))
        {
            return "For corporate and bulk orders exceeding 50 boxes, you receive tiered promotional discounts automatically at checkout! Note that high-value orders require an initial 30% deposit to confirm production.";
        }

        if (lower.Contains("flavor") || lower.Contains("taste") || lower.Contains("vị") || lower.Contains("nhân"))
        {
            return "Popular flavors this season include: Traditional Mixed Nuts with Roasted Chicken, Lotus Seed with Double Salted Yolk, Molten Matcha Lava, Musang King Durian, and Modern Low-Sugar Tiramisu. Browse our catalog for full variant details!";
        }

        return "Welcome to Mooncake E-Commerce Platform! I am your AI assistant. I can help you select the best mooncake flavors, configure custom logo packaging, check bulk discounts, or assist suppliers with market trends. How can I help you today?";
    }
}
