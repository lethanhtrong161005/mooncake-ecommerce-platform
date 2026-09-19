namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using Serilog;
using Serilog.Events;

public static class SerilogSetup
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var appName = builder.Configuration["spring.application.name"] ?? "mooncake-api";
        var logPath = "./logs";

        // Console pattern matches the Logback format: Time [Level] [Thread] Logger : [TraceId] - Message
        const string consoleTemplate = 
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.SSS}] [{Level:u4}] [{ThreadId}] {SourceContext} : [{TraceId}] - {Message:lj}{NewLine}{Exception}";

        // File pattern
        const string fileTemplate = 
            "{Timestamp:yyyy-MM-dd HH:mm:ss.SSS} {Level:u5} [{ThreadId}] {SourceContext} : [{TraceId}] - {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            // Root Level equivalent
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Debug) // Hibernate SQL log equivalent (Debug mode)
            
            // Enrichers
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", appName)
            
            // 1. Console Appender
            .WriteTo.Console(outputTemplate: consoleTemplate)
            
            // 2. DEBUG Async Rolling File
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                .WriteTo.Async(a => a.File(
                    path: $"{logPath}/debug/{appName}-debug-.log",
                    outputTemplate: fileTemplate,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 50 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 30)))
                    
            // 3. INFO Async Rolling File
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                .WriteTo.Async(a => a.File(
                    path: $"{logPath}/info/{appName}-info-.log",
                    outputTemplate: fileTemplate,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 50 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 30)))
                    
            // 4. WARN Async Rolling File
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                .WriteTo.Async(a => a.File(
                    path: $"{logPath}/warn/{appName}-warn-.log",
                    outputTemplate: fileTemplate,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 50 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 60)))
                    
            // 5. ERROR Async Rolling File
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error || e.Level == LogEventLevel.Fatal)
                .WriteTo.Async(a => a.File(
                    path: $"{logPath}/error/{appName}-error-.log",
                    outputTemplate: fileTemplate,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 50 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 60)))
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
