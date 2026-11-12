namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Xunit;

public sealed class ChatServiceTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid ShopId = Guid.NewGuid();
    private static readonly Guid ThreadId = Guid.NewGuid();

    private static bool TryValidateDto(object dto, out List<ValidationResult> errors)
    {
        errors = [];
        return Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
    }

    [Fact]
    public void SendChatMessageRequest_RequiresBodyOrAttachmentOrProduct()
    {
        var emptyReq = new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            Body = "   "
        };
        Assert.False(TryValidateDto(emptyReq, out var errors));
        Assert.Contains(errors, e => e.ErrorMessage == ChatConstants.EmptyMessage);

        var validText = new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            Body = "Xin chào xưởng bánh!"
        };
        Assert.True(TryValidateDto(validText, out _));

        var validProduct = new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            ProductId = Guid.NewGuid()
        };
        Assert.True(TryValidateDto(validProduct, out _));

        var overLimit = new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            AttachmentIds = Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToArray()
        };
        Assert.False(TryValidateDto(overLimit, out errors));
        Assert.Contains(errors, e => e.ErrorMessage == ChatConstants.TooManyAttachments);

        var duplicates = new SendChatMessageRequest
        {
            ClientMessageId = Guid.NewGuid(),
            AttachmentIds = [Guid.NewGuid(), Guid.NewGuid()]
        };
        duplicates = duplicates with { AttachmentIds = [duplicates.AttachmentIds[0], duplicates.AttachmentIds[0]] };
        Assert.False(TryValidateDto(duplicates, out errors));
        Assert.Contains(errors, e => e.ErrorMessage == ChatConstants.DuplicateAttachments);
    }

    [Fact]
    public void OpenChatThreadRequest_RequiresValidTarget()
    {
        var invalid = new OpenChatThreadRequest();
        Assert.False(TryValidateDto(invalid, out var errors));
        Assert.Contains(errors, e => e.ErrorMessage == ChatConstants.InvalidShopId);

        var validShop = new OpenChatThreadRequest { ShopId = ShopId };
        Assert.True(TryValidateDto(validShop, out _));
        Assert.Equal(ShopId, validShop.TargetId);
    }

    [Fact]
    public void ChatPresenceQuery_ValidatesBatchIds()
    {
        var invalid = new ChatPresenceQuery { UserIds = ["not-a-guid"] };
        Assert.False(TryValidateDto(invalid, out var errors));
        Assert.Contains(errors, e => e.ErrorMessage == ChatConstants.InvalidPresenceIds);

        var valid = new ChatPresenceQuery { UserIds = [$"{Guid.NewGuid()},{Guid.NewGuid()}"] };
        Assert.True(TryValidateDto(valid, out _));
        Assert.Equal(2, valid.ToUserIds().Count);
    }

    [Fact]
    public void CursorProtector_RejectsCrossUserCrossThreadOrExpired()
    {
        var now = new DateTime(2026, 11, 12, 6, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(() => now);
        var protector = new ChatCursorProtector(new EphemeralDataProtectionProvider(), clock.Object);

        var cursor = new ChatCursorDto(CustomerId, ThreadId, now.AddMinutes(-5), Guid.NewGuid());
        var protectedToken = protector.Protect(cursor);

        var unpacked = protector.Unprotect(CustomerId, ThreadId, protectedToken);
        Assert.Equal(cursor.Id, unpacked.Id);

        // Cross-user reject
        var otherUser = Guid.NewGuid();
        Assert.Throws<HttpException>(() => protector.Unprotect(otherUser, ThreadId, protectedToken));

        // Cross-thread reject
        var otherThread = Guid.NewGuid();
        Assert.Throws<HttpException>(() => protector.Unprotect(CustomerId, otherThread, protectedToken));

        // Expired reject
        clock.SetupGet(x => x.UtcNow).Returns(() => now.AddMinutes(20));
        Assert.Throws<HttpException>(() => protector.Unprotect(CustomerId, ThreadId, protectedToken));
    }

    [Fact]
    public async Task OpenThread_PreventsOwnerChattingWithOwnShop()
    {
        var clock = new Mock<IDateTimeProvider>();
        var repo = new Mock<IChatRepository>();
        var cursors = new Mock<IChatCursorProtector>();
        var realtime = new Mock<IChatRealtimePublisher>();
        var presence = new Mock<IPresenceTracker>();
        var storage = new Mock<IChatAttachmentStorage>();

        repo.Setup(x => x.FindActiveShopOwnerAsync(ShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatShopOwnerDto(ShopId, SupplierId));

        var service = new ChatService(repo.Object, cursors.Object, realtime.Object, presence.Object, storage.Object, clock.Object);

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.OpenThreadAsync(SupplierId, new OpenChatThreadRequest { ShopId = ShopId }));
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(ChatConstants.CannotChatOwnShop, ex.Message);
    }

    [Fact]
    public async Task SendMessage_BlocksIfThreadIsBlocked()
    {
        var clock = new Mock<IDateTimeProvider>();
        var repo = new Mock<IChatRepository>();
        var cursors = new Mock<IChatCursorProtector>();
        var realtime = new Mock<IChatRealtimePublisher>();
        var presence = new Mock<IPresenceTracker>();
        var storage = new Mock<IChatAttachmentStorage>();

        repo.Setup(x => x.GetThreadAccessAsync(ThreadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatThreadAccessDto(ThreadId, CustomerId, SupplierId, ShopId, BlockedByUserId: SupplierId));

        var service = new ChatService(repo.Object, cursors.Object, realtime.Object, presence.Object, storage.Object, clock.Object);

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.SendMessageAsync(CustomerId, ThreadId, new SendChatMessageRequest
            {
                ClientMessageId = Guid.NewGuid(),
                Body = "Chào bạn"
            }));
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(ChatConstants.ConversationBlocked, ex.Message);
    }

    [Fact]
    public async Task EditMessage_FailsIfPastFiveMinutesOrNotSender()
    {
        var now = new DateTime(2026, 11, 12, 6, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var repo = new Mock<IChatRepository>();
        var cursors = new Mock<IChatCursorProtector>();
        var realtime = new Mock<IChatRealtimePublisher>();
        var presence = new Mock<IPresenceTracker>();
        var storage = new Mock<IChatAttachmentStorage>();

        var msgId = Guid.NewGuid();
        var message = new ChatMessage
        {
            Id = msgId,
            ThreadId = ThreadId,
            SenderId = CustomerId,
            CreatedAt = now.AddMinutes(-6),
            Body = "Gốc"
        };

        repo.Setup(x => x.GetMessageAsync(msgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        repo.Setup(x => x.GetThreadAccessAsync(ThreadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatThreadAccessDto(ThreadId, CustomerId, SupplierId, ShopId));

        var service = new ChatService(repo.Object, cursors.Object, realtime.Object, presence.Object, storage.Object, clock.Object);

        // Edit past 5 minutes fails
        var expiredEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.EditMessageAsync(CustomerId, msgId, new EditChatMessageRequest { Body = "Đã sửa" }));
        Assert.Equal(400, expiredEx.StatusCode);
        Assert.Equal(ChatConstants.EditWindowExpired, expiredEx.Message);

        // Edit by another user fails
        message.CreatedAt = now.AddMinutes(-2);
        var notOwnerEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.EditMessageAsync(SupplierId, msgId, new EditChatMessageRequest { Body = "Đã sửa" }));
        Assert.Equal(403, notOwnerEx.StatusCode);
        Assert.Equal(ChatConstants.NotMessageOwner, notOwnerEx.Message);

        // Edit valid succeeds
        var edited = await service.EditMessageAsync(CustomerId, msgId, new EditChatMessageRequest { Body = "Đã sửa thành công" });
        Assert.Equal("Đã sửa thành công", edited.Body);
        repo.Verify(x => x.EditMessageAsync(message, "Đã sửa thành công", now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void InMemoryPresenceTracker_TracksMultipleConnectionsAccurately()
    {
        var tracker = new InMemoryPresenceTracker();
        var user = Guid.NewGuid();

        Assert.False(tracker.IsOnline(user));

        // Connect 1st tab
        Assert.True(tracker.Connect(user, "conn-1"));
        Assert.True(tracker.IsOnline(user));

        // Connect 2nd tab (returns false because user was already online)
        Assert.False(tracker.Connect(user, "conn-2"));
        Assert.True(tracker.IsOnline(user));

        // Disconnect 1st tab (returns false because user still has 1 tab open)
        Assert.False(tracker.Disconnect(user, "conn-1"));
        Assert.True(tracker.IsOnline(user));

        // Disconnect 2nd tab (returns true because user now has 0 connections)
        Assert.True(tracker.Disconnect(user, "conn-2"));
        Assert.False(tracker.IsOnline(user));
    }
}
