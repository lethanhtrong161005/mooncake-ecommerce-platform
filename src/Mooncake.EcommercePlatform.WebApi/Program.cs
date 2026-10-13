using Mooncake.EcommercePlatform.WebApi.Configurations;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("TZ", builder.Configuration["Server:TimeZone"] ?? "Etc/UTC");
TimeZoneInfo.ClearCachedData();

var port = builder.Configuration["Server:Port"] ?? "8089";
var host = builder.Environment.IsDevelopment() ? "localhost" : "0.0.0.0";
builder.WebHost.UseUrls($"http://{host}:{port}");

SerilogSetup.ConfigureSerilog(builder);
builder.Services.AddWebApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
