namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Infrastructure;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Middleware;

/// <summary>Registers HTTP, authentication, and application services for the Web API.</summary>
public static class WebApiServiceCollectionExtensions
{
    private const string AuthRoutePrefix = "/api/v1/auth";
    private const string GoogleAuthRoutePrefix = "/api/v1/auth/google";
    private const string AuthLoginRateLimitPolicyName = "auth-login";
    private const string DevelopmentJwtKeyPrefix = "development-only-";

    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));
        services.Configure<ApiBehaviorOptions>(ConfigureInvalidModelStateResponse);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "Mooncake E-Commerce Platform API", Version = "v1" }));
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddApplication().AddInfrastructure(configuration);
        services.AddJwtAuthentication(configuration, environment);
        services.AddGoogleOAuth(configuration, environment);
        services.AddFrontendCors(configuration);
        services.AddAuthenticationRateLimiting();
        services.AddAuthorization();
        return services;
    }

    private static void ConfigureInvalidModelStateResponse(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            if (!context.HttpContext.Request.Path.StartsWithSegments(AuthRoutePrefix))
                return new BadRequestObjectResult(context.ModelState);

            var isGoogleRequest = context.HttpContext.Request.Path.StartsWithSegments(GoogleAuthRoutePrefix);
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            var traceId = context.HttpContext.Items["TraceId"]?.ToString() ?? string.Empty;
            var isRegisterRequest = context.HttpContext.Request.Path.StartsWithSegments("/api/v1/auth/register");
            var message = isGoogleRequest ? MessageConstant.GoogleInvalidRequest :
                isRegisterRequest ? MessageConstant.InvalidRegisterData : MessageConstant.InvalidRequest;
            return new BadRequestObjectResult(ResponseHelper.Error<object>(400, message, path, traceId));
        };
    }

    private static void AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");
        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32 || string.IsNullOrWhiteSpace(jwt.Issuer)
            || string.IsNullOrWhiteSpace(jwt.Audience) || jwt.AccessTokenMinutes <= 0
            || jwt.RefreshTokenDays <= 0)
            throw new InvalidOperationException("JWT configuration is invalid.");
        if (!environment.IsDevelopment() && jwt.Key.StartsWith(DevelopmentJwtKeyPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("A production JWT signing key must be configured.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (context.Principal?.IsInRole("Supplier") != true) return;
                    if (!Guid.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                    {
                        context.Fail("Invalid supplier identity.");
                        return;
                    }
                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var valid = await db.Users.AsNoTracking().AnyAsync(user =>
                        user.Id == userId && !user.IsDeleted && user.IsActive && user.Role == UserRole.Supplier,
                        context.HttpContext.RequestAborted)
                        && await db.SupplierProfiles.AsNoTracking().AnyAsync(profile =>
                            profile.UserId == userId && !profile.IsDeleted &&
                            profile.VerificationStatus == SupplierVerificationStatus.Verified,
                            context.HttpContext.RequestAborted);
                    if (!valid) context.Fail("Supplier verification is no longer valid.");
                }
            };
        });
    }

    private static void AddGoogleOAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var google = configuration.GetSection(GoogleOAuthOptions.SectionName).Get<GoogleOAuthOptions>()
            ?? throw new InvalidOperationException("Google OAuth configuration is missing.");
        var hasValidAuthorizationEndpoint = Uri.TryCreate(
            google.AuthorizationEndpoint, UriKind.Absolute, out var authorizationEndpoint)
            && authorizationEndpoint.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(authorizationEndpoint.Query)
            && string.IsNullOrEmpty(authorizationEndpoint.Fragment);
        var hasValidRedirectUri = Uri.TryCreate(google.RedirectUri, UriKind.Absolute, out var redirectUri)
            && (redirectUri.Scheme == Uri.UriSchemeHttps
                || environment.IsDevelopment() && redirectUri.Scheme == Uri.UriSchemeHttp
                    && redirectUri.Host == "localhost");
        if (string.IsNullOrWhiteSpace(google.ClientId) || string.IsNullOrWhiteSpace(google.ClientSecret)
            || !hasValidAuthorizationEndpoint || !hasValidRedirectUri || google.StateLifetimeMinutes <= 0)
            throw new InvalidOperationException("Google OAuth configuration is invalid.");

        services.Configure<GoogleOAuthOptions>(configuration.GetSection(GoogleOAuthOptions.SectionName));
    }

    private static void AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? throw new InvalidOperationException("CORS configuration is missing.");
        var origins = cors.AllowedOrigins
            .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (origins.Length == 0)
            throw new InvalidOperationException("CORS configuration must contain at least one valid origin.");

        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.AddCors(options => options.AddPolicy(CorsOptions.FrontendPolicyName,
            policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }

    private static void AddAuthenticationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthLoginRateLimitPolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("auth-otp", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var message = http.Request.Path.StartsWithSegments("/api/v1/auth/register")
                    ? MessageConstant.OtpRateLimited : MessageConstant.TooManyAttempts;
                await http.Response.WriteAsJsonAsync(ResponseHelper.Error<object>(
                    StatusCodes.Status429TooManyRequests, message,
                    http.Request.Path.Value ?? string.Empty,
                    http.Items["TraceId"]?.ToString() ?? string.Empty), cancellationToken);
            };
        });
    }
}
