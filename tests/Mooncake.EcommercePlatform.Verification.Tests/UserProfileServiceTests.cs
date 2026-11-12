namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Xunit;

public sealed class UserProfileServiceTests
{
    [Fact]
    public async Task OwnProfileUpdatePreservesEmailAndUpdatesOnlyEditableFields()
    {
        var userId = Guid.NewGuid();
        var user = new AuthUserData
        {
            Id = userId,
            Email = "buyer@example.com",
            FullName = "Old Name",
            IsActive = true
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository.Setup(value => value.GetByPhoneAsync("0901234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData?)null);
        repository.Setup(value => value.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData value, CancellationToken _) => value);
        var service = new UserService(repository.Object, new UserHelper());

        var result = await service.UpdateOwnProfileAsync(userId, new UpdateOwnProfileRequest
        {
            FullName = "New Name",
            Phone = "0901234567",
            AvatarUrl = "https://example.com/avatar.jpg"
        });

        Assert.Equal("buyer@example.com", result.Email);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("0901234567", result.Phone);
        Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
        repository.Verify(value => value.UpdateAsync(It.Is<AuthUserData>(updated =>
            updated.Id == userId && updated.Email == "buyer@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OwnAvatarUpdatePersistsTrimmedUrlForActiveUser()
    {
        var userId = Guid.NewGuid();
        var user = new AuthUserData
        {
            Id = userId,
            Email = "buyer@example.com",
            FullName = "Trong An",
            IsActive = true
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository.Setup(value => value.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData value, CancellationToken _) => value);
        var service = new UserService(repository.Object, new UserHelper());

        var result = await service.UpdateOwnAvatarAsync(userId, "  https://res.cloudinary.com/demo/avatar.png  ");

        Assert.Equal("https://res.cloudinary.com/demo/avatar.png", result.AvatarUrl);
        repository.Verify(value => value.UpdateAsync(It.Is<AuthUserData>(updated =>
            updated.Id == userId && updated.AvatarUrl == "https://res.cloudinary.com/demo/avatar.png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OwnAvatarUpdateClearsUrlWhenWhitespace()
    {
        var userId = Guid.NewGuid();
        var user = new AuthUserData
        {
            Id = userId,
            Email = "buyer@example.com",
            FullName = "Trong An",
            AvatarUrl = "https://res.cloudinary.com/demo/old.png",
            IsActive = true
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository.Setup(value => value.UpdateAsync(It.IsAny<AuthUserData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthUserData value, CancellationToken _) => value);
        var service = new UserService(repository.Object, new UserHelper());

        var result = await service.UpdateOwnAvatarAsync(userId, "   ");

        Assert.Null(result.AvatarUrl);
        repository.Verify(value => value.UpdateAsync(It.Is<AuthUserData>(updated =>
            updated.Id == userId && updated.AvatarUrl == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
