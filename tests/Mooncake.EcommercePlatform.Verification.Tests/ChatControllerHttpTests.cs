namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class ChatControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ThreadId = Guid.NewGuid();
    private readonly Mock<IChatService> chatService = new();

    private HttpClient CreateClient(bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ChatController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(chatService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddRateLimiter(_ => { });

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();

        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                    new Claim(ClaimTypes.Role, "Customer"),
                ], "Test");
                context.User = new ClaimsPrincipal(identity);
                await next();
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        var client = app.GetTestClient();
        return client;
    }

    [Fact]
    public async Task ChatEndpoints_RequireAuthentication()
    {
        using var client = CreateClient(authenticated: false);

        var response = await client.GetAsync("/api/v1/chat/threads");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var sendResponse = await client.PostAsJsonAsync($"/api/v1/chat/threads/{ThreadId}/messages", new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            Body = "Test"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, sendResponse.StatusCode);
    }

    [Fact]
    public async Task OpenThread_Returns400_WhenDtoValidationFails()
    {
        using var client = CreateClient();

        // TargetId is empty/null
        var response = await client.PostAsJsonAsync("/api/v1/chat/threads", new OpenChatThreadRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_Returns400_WhenBodyAndAttachmentsAndProductAreEmpty()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync($"/api/v1/chat/threads/{ThreadId}/messages", new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            Body = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_Returns201_WhenValid()
    {
        using var client = CreateClient();
        var clientMsgId = Guid.NewGuid();

        chatService.Setup(x => x.SendMessageAsync(UserId, ThreadId, It.IsAny<SendChatMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageResponse(
                Guid.NewGuid(),
                ThreadId,
                UserId,
                ChatSenderRole.Customer,
                ChatMessageType.Text,
                "Xin chào",
                clientMsgId,
                false,
                DateTime.UtcNow,
                null,
                []));

        var response = await client.PostAsJsonAsync($"/api/v1/chat/threads/{ThreadId}/messages", new SendChatMessageRequest
        {
            ClientMessageId = clientMsgId,
            Body = "Xin chào"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetUnreadCount_Returns200()
    {
        using var client = CreateClient();

        chatService.Setup(x => x.GetUnreadCountAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatUnreadCountResponse(5));

        var response = await client.GetAsync("/api/v1/chat/unread-count");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
