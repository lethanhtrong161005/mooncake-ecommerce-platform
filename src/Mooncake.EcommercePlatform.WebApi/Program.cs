using Mooncake.EcommercePlatform.Application;
using Mooncake.EcommercePlatform.Infrastructure;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Serilog;

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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mooncake E-Commerce Platform API", Version = "v1" });
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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
