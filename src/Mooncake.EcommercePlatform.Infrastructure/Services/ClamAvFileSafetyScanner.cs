namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Helpers;

public sealed class ClamAvFileSafetyScanner(IConfiguration configuration, IHostEnvironment environment,
    ILogger<ClamAvFileSafetyScanner> logger) : IFileSafetyScanner
{
    public Task ScanAsync(byte[] content, CancellationToken ct) => ClamAvScanHelper.ScanAsync(content, configuration, environment, logger, ct);
}
