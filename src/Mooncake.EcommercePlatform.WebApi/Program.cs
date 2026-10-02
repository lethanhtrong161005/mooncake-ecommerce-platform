using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Mooncake.EcommercePlatform.Application;
using Mooncake.EcommercePlatform.Infrastructure;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Serilog;

// ── Force UTC runtime timezone ────────────────────────────────────────────────
Environment.SetEnvironmentVariable("TZ", "Etc/UTC");
TimeZoneInfo.ClearCachedData();

var builder = WebApplication.CreateBuilder(args);

// ── Environment Variables (Enterprise Standard) ───────────────────────────────
if (builder.Environment.IsDevelopment())
{
    var localEnvPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "docker", "dev", ".env.dev");
    if (File.Exists(localEnvPath))
    {
        DotNetEnv.Env.Load(localEnvPath);
    }
}

var port = Environment.GetEnvironmentVariable("PORT") 
        ?? Environment.GetEnvironmentVariable("BACKEND_PORT") 
        ?? "8089";

var host = builder.Environment.IsDevelopment() ? "localhost" : "0.0.0.0";
builder.WebHost.UseUrls($"http://{host}:{port}");

// ── Logging (Serilog) ─────────────────────────────────────────────────────────
Mooncake.EcommercePlatform.WebApi.Configurations.SerilogSetup.ConfigureSerilog(builder);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mooncake E-Commerce Platform API", Version = "v1" });
});

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:SecretKey"] 
             ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
             ?? "MooncakeEcommercePlatformSuperSecretSecurityKey2026!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MooncakePlatform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MooncakePlatformAudience";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

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

// Serve uploaded static files under /uploads
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
