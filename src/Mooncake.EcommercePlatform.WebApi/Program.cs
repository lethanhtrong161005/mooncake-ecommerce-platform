using Mooncake.EcommercePlatform.Application;
using Mooncake.EcommercePlatform.Infrastructure;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

const string BootstrapCompleteMessage = "Platform bootstrap completed successfully.";

// ── Force UTC runtime timezone ────────────────────────────────────────────────
Environment.SetEnvironmentVariable("TZ", "Etc/UTC");
TimeZoneInfo.ClearCachedData();

var builder = WebApplication.CreateBuilder(args);

// ── Environment Variables (Enterprise Standard) ───────────────────────────────
// On Cloud/Production (AWS, Azure, GCP, K8s), environment variables are injected directly by the orchestrator.
// We only load .env files manually during local development.
if (builder.Environment.IsDevelopment())
{
    var localEnvPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "docker", "dev", ".env.dev");
    if (File.Exists(localEnvPath))
    {
        DotNetEnv.Env.Load(localEnvPath);
        builder.Configuration.AddEnvironmentVariables();
    }
}

// Cloud providers (Heroku, GCP Cloud Run) often inject the 'PORT' environment variable.
// Fallback to our custom 'BACKEND_PORT', and default to 8089.
var port = Environment.GetEnvironmentVariable("PORT") 
        ?? Environment.GetEnvironmentVariable("BACKEND_PORT") 
        ?? "8089";

var host = builder.Environment.IsDevelopment() ? "localhost" : "0.0.0.0";
builder.WebHost.UseUrls($"http://{host}:{port}");

// ── Logging (Serilog) ─────────────────────────────────────────────────────────
Mooncake.EcommercePlatform.WebApi.Configurations.SerilogSetup.ConfigureSerilog(builder);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mooncake E-Commerce Platform API", Version = "v1" });
});

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("JWT_SECRET_KEY must be configured with at least 32 bytes.");
}

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "Mooncake.EcommercePlatform";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "Mooncake.Client";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (args.Contains("--bootstrap-platform", StringComparer.OrdinalIgnoreCase))
{
    var adminEmail = Environment.GetEnvironmentVariable("INITIAL_ADMIN_EMAIL");
    var adminPassword = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        throw new InvalidOperationException("INITIAL_ADMIN_EMAIL and INITIAL_ADMIN_PASSWORD are required for platform bootstrap.");

    await using var bootstrapScope = app.Services.CreateAsyncScope();
    var bootstrapService = bootstrapScope.ServiceProvider.GetRequiredService<IPlatformBootstrapService>();
    await bootstrapService.InitializeAsync(adminEmail, adminPassword);
    Console.WriteLine(BootstrapCompleteMessage);
    return;
}

// TraceId middleware MUST be first in the pipeline
app.UseMiddleware<TraceIdMiddleware>();
app.UseSerilogRequestLogging(); // Log HTTP requests with Serilog
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mooncake E-Commerce Platform API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
