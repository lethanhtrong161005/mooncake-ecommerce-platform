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
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Users.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

using GoogleLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.GoogleLoginRequest;
using PhoneLoginRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.LoginRequest;
using RefreshTokenRequest = Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests.RefreshTokenRequest;

public sealed class Module1AuthAndUsersControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IAuthService> authService = new();
    private readonly Mock<IUserService> userService = new();

    private HttpClient CreateClient(string role = "Customer", bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(authService.Object);
        builder.Services.AddSingleton(userService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

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
                    new Claim(ClaimTypes.Role, role),
                ], "Test");

                context.User = new ClaimsPrincipal(identity);
                await next();
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }

    // =========================================================================
    // FEATURE #1: AUTHENTICATION & REGISTRATION ENDPOINTS
    // =========================================================================

    [Fact]
    public async Task Register_Returns202Accepted_OnSuccess()
    {
        var client = CreateClient(authenticated: false);
        var request = new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "vana@example.com",
            Phone = "0987654321",
            Password = "Password123@",
            Role = "Customer"
        };

        authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Verification code sent to the registered email address.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Register_Returns409Conflict_WhenEmailOrPhoneAlreadyRegistered()
    {
        var client = CreateClient(authenticated: false);
        var request = new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "vana@example.com",
            Phone = "0987654321",
            Password = "Password123@",
            Role = "Customer"
        };

        authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(409, "User with this email or phone number already exists."));

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_Returns200OK_WhenOtpValid()
    {
        var client = CreateClient(authenticated: false);
        var request = new VerifyEmailRequest
        {
            Email = "vana@example.com",
            Code = "123456"
        };

        authService.Setup(s => s.VerifyEmailAsync(It.IsAny<VerifyEmailRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Email verified successfully.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task VerifyEmail_Returns400BadRequest_WhenOtpInvalidOrExpired()
    {
        var client = CreateClient(authenticated: false);
        var request = new VerifyEmailRequest
        {
            Email = "vana@example.com",
            Code = "999999"
        };

        authService.Setup(s => s.VerifyEmailAsync(It.IsAny<VerifyEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(400, "Mã xác minh không chính xác hoặc đã hết hạn."));

        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_Returns200OK()
    {
        var client = CreateClient(authenticated: false);
        var request = new ForgotPasswordRequest { Email = "vana@example.com" };

        authService.Setup(s => s.ResendVerificationAsync("vana@example.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/resend-verification", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("If verification is pending, a code has been sent.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ForgotPassword_Returns200OK()
    {
        var client = CreateClient(authenticated: false);
        var request = new ForgotPasswordRequest { Email = "vana@example.com" };

        authService.Setup(s => s.RequestPasswordResetAsync("vana@example.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("If the account exists, a reset code has been sent.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ResetPassword_Returns200OK_OnSuccess()
    {
        var client = CreateClient(authenticated: false);
        var request = new ResetPasswordRequest
        {
            Email = "vana@example.com",
            Code = "654321",
            NewPassword = "NewSecurePassword123@"
        };

        authService.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Password reset successfully.", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GoogleAuthorizationUrl_Returns200WithUrl()
    {
        var client = CreateClient(authenticated: false);

        authService.Setup(s => s.CreateGoogleAuthorizationUrlAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleAuthorizationUrlResponse(
                "https://accounts.google.com/o/oauth2/v2/auth?client_id=xyz",
                "state-abc-123",
                300));

        var response = await client.PostAsync("/api/v1/auth/google/authorization-url", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("authorizationUrl").GetString()?.Contains("google.com"));
        Assert.Equal("state-abc-123", json.GetProperty("data").GetProperty("state").GetString());
    }

    [Fact]
    public async Task GoogleLogin_Returns200WithTokens()
    {
        var client = CreateClient(authenticated: false);
        var request = new GoogleLoginRequest("auth-code-xyz", "state-123");

        var authenUser = new AuthenUserResponse(UserId, "0987654321", "Google User", UserRole.Customer);
        authService.Setup(s => s.GoogleLoginAsync(It.IsAny<GoogleLoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenResponse("mock-access-token", "mock-refresh-token", "Bearer", 900, authenUser));

        var response = await client.PostAsJsonAsync("/api/v1/auth/google/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("mock-access-token", json.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal("Google User", json.GetProperty("data").GetProperty("user").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Login_Returns200WithTokens_OnValidCredentials()
    {
        var client = CreateClient(authenticated: false);
        var request = new PhoneLoginRequest("0987654321", "Password123@");

        var authenUser = new AuthenUserResponse(UserId, "0987654321", "Nguyen Van A", UserRole.Customer);
        authService.Setup(s => s.LoginAsync(It.IsAny<PhoneLoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenResponse("valid-access-token", "valid-refresh-token", "Bearer", 900, authenUser));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("valid-access-token", json.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal("valid-refresh-token", json.GetProperty("data").GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Login_Returns400_WhenCredentialsWrong()
    {
        var client = CreateClient(authenticated: false);
        var request = new PhoneLoginRequest("0987654321", "WrongPassword");

        authService.Setup(s => s.LoginAsync(It.IsAny<PhoneLoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(400, "Số điện thoại hoặc mật khẩu không chính xác."));

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_Returns200WithRotatedTokens()
    {
        var client = CreateClient(authenticated: false);
        var request = new RefreshTokenRequest("old-refresh-token");

        var authenUser = new AuthenUserResponse(UserId, "0987654321", "Nguyen Van A", UserRole.Customer);
        authService.Setup(s => s.RefreshAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenResponse("new-access-token", "new-refresh-token", "Bearer", 900, authenUser));

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("new-access-token", json.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal("new-refresh-token", json.GetProperty("data").GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Logout_Returns200OK()
    {
        var client = CreateClient(authenticated: false);
        var request = new RefreshTokenRequest("active-refresh-token");

        authService.Setup(s => s.LogoutAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_Returns200WithUserData_WhenAuthenticated()
    {
        var client = CreateClient(role: "Customer", authenticated: true);

        var authenUser = new AuthenUserResponse(UserId, "0987654321", "Nguyen Van A", UserRole.Customer);
        authService.Setup(s => s.GetCurrentUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenUser);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(UserId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
        Assert.Equal("Nguyen Van A", json.GetProperty("data").GetProperty("fullName").GetString());
    }

    // =========================================================================
    // FEATURE #1: USER MANAGEMENT (ADMIN REST ENDPOINTS)
    // =========================================================================

    [Fact]
    public async Task Users_GetAll_Returns200_ForAdmin()
    {
        var client = CreateClient(role: "Admin");

        userService.Setup(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new UserResponse(UserId, "admin@example.com", "Admin User", "0900000001", null, UserRole.Admin, DateTime.UtcNow, DateTime.UtcNow),
                new UserResponse(Guid.NewGuid(), "customer@example.com", "Customer One", "0900000002", null, UserRole.Customer, DateTime.UtcNow, DateTime.UtcNow)
            ]);

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Users_GetAll_Returns403Forbidden_ForCustomer()
    {
        var client = CreateClient(role: "Customer");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Users_GetById_Returns200_WhenFoundByAdmin()
    {
        var client = CreateClient(role: "Admin");
        var targetId = Guid.NewGuid();

        userService.Setup(s => s.GetUserByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(targetId, "target@example.com", "Target User", "0911111111", null, UserRole.Customer, DateTime.UtcNow, DateTime.UtcNow));

        var response = await client.GetAsync($"/api/v1/users/{targetId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Target User", json.GetProperty("data").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Users_GetById_Returns404_WhenUserNotFound()
    {
        var client = CreateClient(role: "Admin");
        var targetId = Guid.NewGuid();

        userService.Setup(s => s.GetUserByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpException(404, "User not found."));

        var response = await client.GetAsync($"/api/v1/users/{targetId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Users_Create_Returns201Created_ForAdmin()
    {
        var client = CreateClient(role: "Admin");
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            FullName = "New User",
            Phone = "0922222222",
            Password = "SecurePassword123@",
            Role = UserRole.Supplier
        };

        var createdId = Guid.NewGuid();
        userService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(createdId, request.Email, request.FullName, request.Phone, null, UserRole.Supplier, DateTime.UtcNow, DateTime.UtcNow));

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(createdId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Users_Update_Returns200OK_ForAdmin()
    {
        var client = CreateClient(role: "Admin");
        var targetId = Guid.NewGuid();
        var request = new UpdateUserRequest
        {
            FullName = "Updated Full Name",
            Email = "updated@example.com",
            Phone = "0933333333",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        userService.Setup(s => s.UpdateUserAsync(targetId, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(targetId, request.Email, request.FullName, request.Phone, request.AvatarUrl, UserRole.Customer, DateTime.UtcNow, DateTime.UtcNow));

        var response = await client.PutAsJsonAsync($"/api/v1/users/{targetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Full Name", json.GetProperty("data").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Users_Delete_Returns200OK_ForAdmin()
    {
        var client = CreateClient(role: "Admin");
        var targetId = Guid.NewGuid();

        userService.Setup(s => s.DeleteUserAsync(targetId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.DeleteAsync($"/api/v1/users/{targetId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("User deleted successfully.", json.GetProperty("message").GetString());
    }
}
