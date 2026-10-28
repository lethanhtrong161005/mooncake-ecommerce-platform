namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Infrastructure;
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
        services.AddSupplierVerificationFeature(configuration);
        services.AddShopFeature();
        services.AddProductCatalogFeature();
        return services;
    }

    private static void ConfigureInvalidModelStateResponse(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<Mooncake.EcommercePlatform.WebApi.Filters.ProductApiAttribute>() != null)
            {
                const string invalidField = "Invalid field value.";
                var fields = context.ModelState.Where(x => x.Value?.Errors.Count > 0).SelectMany(x => x.Value!.Errors.Select(e =>
                    new Mooncake.EcommercePlatform.Application.Common.DTOs.ApiValidationError(x.Key,
                        ProductConstants.Invalid, string.IsNullOrWhiteSpace(e.ErrorMessage) ? invalidField : e.ErrorMessage))).ToArray();
                return new BadRequestObjectResult(new Mooncake.EcommercePlatform.Application.Common.DTOs.ApiResponse<object> {
                    Status = 400, ErrorCode = ProductConstants.Invalid, Message = ProductConstants.Error,
                    ValidationErrors = fields, Errors = fields.Select(x => x.Message).ToList(),
                    Path = context.HttpContext.Request.Path, TraceId = context.HttpContext.TraceIdentifier });
            }
            if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<Mooncake.EcommercePlatform.WebApi.Filters.ShopApiAttribute>() != null)
            {
                const string invalidField = "Invalid field value.";
                var fields = context.ModelState.Where(x => x.Value?.Errors.Count > 0).SelectMany(x => x.Value!.Errors.Select(e =>
                    new Mooncake.EcommercePlatform.Application.Common.DTOs.ApiValidationError(x.Key,
                        Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants.Invalid,
                        string.IsNullOrWhiteSpace(e.ErrorMessage) ? invalidField : e.ErrorMessage))).ToArray();
                return new BadRequestObjectResult(new Mooncake.EcommercePlatform.Application.Common.DTOs.ApiResponse<object> {
                    Status = 400, ErrorCode = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants.Invalid,
                    Message = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants.Error,
                    ValidationErrors = fields, Errors = fields.Select(x => x.Message).ToList(),
                    Path = context.HttpContext.Request.Path, TraceId = context.HttpContext.TraceIdentifier });
            }
            if (!context.HttpContext.Request.Path.StartsWithSegments(AuthRoutePrefix))
            {
                var error = ResponseHelper.Error<object>(400,
                    Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants.Error,
                    context.HttpContext.Request.Path, context.HttpContext.TraceIdentifier);
                error.ErrorCode = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants.Invalid;
                return new BadRequestObjectResult(error);
            }

            var isGoogleRequest = context.HttpContext.Request.Path.StartsWithSegments(GoogleAuthRoutePrefix);
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            var traceId = context.HttpContext.Items["TraceId"]?.ToString() ?? string.Empty;
            var message = isGoogleRequest ? MessageConstant.GoogleInvalidRequest : MessageConstant.InvalidRequest;
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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Path.StartsWithSegments(
                        Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants.HubPath))
                        context.Token = context.Request.Query["access_token"];
                    return Task.CompletedTask;
                }
            };
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
            policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
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
                await http.Response.WriteAsJsonAsync(ResponseHelper.Error<object>(
                    StatusCodes.Status429TooManyRequests, MessageConstant.TooManyAttempts,
                    http.Request.Path.Value ?? string.Empty,
                    http.Items["TraceId"]?.ToString() ?? string.Empty), cancellationToken);
            };
        });
    }
}
