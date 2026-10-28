namespace Mooncake.EcommercePlatform.Infrastructure.Helpers;

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Domain.Entities;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

public static class ClamAvScanHelper
{
    private const string ScanCommand = "zINSTREAM\0";
    public static async Task ScanAsync(byte[] content, IConfiguration configuration, IHostEnvironment environment, ILogger logger, CancellationToken cancellationToken)
    {
        // Development bypass must be explicit; production never silently accepts unscanned legal files.
        if (environment.IsDevelopment() && configuration.GetValue<bool>("SupplierVerification:AllowUnscannedDevelopmentFiles")) return;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var client = new TcpClient();
            await client.ConnectAsync(configuration["SupplierVerification:ClamAvHost"] ?? "127.0.0.1",
                configuration.GetValue<int?>("SupplierVerification:ClamAvPort") ?? 3310, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes(ScanCommand), timeout.Token);
            var length = new byte[4];
            for (var offset = 0; offset < content.Length; offset += 65536)
            {
                var count = Math.Min(65536, content.Length - offset);
                BinaryPrimitives.WriteInt32BigEndian(length, count);
                await stream.WriteAsync(length, timeout.Token);
                await stream.WriteAsync(content.AsMemory(offset, count), timeout.Token);
            }
            await stream.WriteAsync(new byte[4], timeout.Token);
            var response = new byte[1024];
            var countRead = 0;
            while (countRead < response.Length)
            {
                var read = await stream.ReadAsync(response.AsMemory(countRead), timeout.Token);
                if (read == 0) break;
                countRead += read;
                if (response.AsSpan(0, countRead).Contains((byte)0)) break;
            }
            var verdict = Encoding.UTF8.GetString(response, 0, countRead).TrimEnd('\0');
            if (verdict.EndsWith(": OK", StringComparison.Ordinal)) return;
            SupplierVerificationHelper.Require(!verdict.EndsWith(" FOUND", StringComparison.Ordinal), 415, C.DocumentMalwareDetected);
            throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(503, C.Error, C.ScannerUnavailable);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception failure) when (failure is SocketException or IOException or OperationCanceledException)
        {
            logger.LogWarning(C.ScanFailureLog, failure.GetType().Name);
            throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(503, C.Error, C.ScannerUnavailable);
        }
    }
}
