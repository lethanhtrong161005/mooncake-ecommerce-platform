using Mooncake.EcommercePlatform.WebApi.Configurations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Mooncake.EcommercePlatform.WebApi.Background;
using Serilog;

const string BootstrapCompleteMessage = "Platform bootstrap completed successfully.";

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("TZ", builder.Configuration["Server:TimeZone"] ?? "Etc/UTC");
TimeZoneInfo.ClearCachedData();

var port = builder.Configuration["Server:Port"] ?? "8089";
var host = builder.Environment.IsDevelopment() ? "localhost" : "0.0.0.0";
builder.WebHost.UseUrls($"http://{host}:{port}");

SerilogSetup.ConfigureSerilog(builder);
builder.Services.AddWebApiServices(builder.Configuration, builder.Environment);

builder.Services.AddSingleton<IRecurringJob, AuthMaintenanceJob>();
builder.Services.AddSingleton<IRecurringJob, RfqDeadlineJob>();
builder.Services.AddSingleton<IRecurringJob, PaymentExpiryJob>();
builder.Services.AddHostedService<RecurringJobRunner>();

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

app.UseMiddleware<TraceIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mooncake E-Commerce Platform API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsOptions.FrontendPolicyName);
app.UseAuthentication();
app.UseMiddleware<CurrentAccountMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AdminAuditMiddleware>();
app.MapControllers();
app.MapHub<Mooncake.EcommercePlatform.WebApi.Realtime.SupplierVerificationHub>(
    Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants.HubPath,
    options => options.CloseOnAuthenticationExpiration = true);

app.Run();
