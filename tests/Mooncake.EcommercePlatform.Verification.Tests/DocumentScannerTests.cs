namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Services;

public sealed class DocumentScannerTests
{
    [Theory]
    [InlineData("stream: OK\0", null, null)]
    [InlineData("stream: Test-Signature FOUND\0", 415, "DOCUMENT_MALWARE_DETECTED")]
    [InlineData("stream: scanner database unavailable ERROR\0", 503, "DOCUMENT_SCANNER_UNAVAILABLE")]
    public async Task ScannerVerdictSeparatesUnsafeFilesFromUnavailableServices(string reply, int? status, string? code)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var server = RespondAsync(listener, reply, deadline.Token);
        var gateway = Create(port);
        if (status.HasValue)
        {
            var failure = await Assert.ThrowsAsync<HttpException>(() => gateway.ScanAsync([1, 2, 3], deadline.Token));
            Assert.Equal(status, failure.StatusCode);
            Assert.Equal(code, failure.ErrorCode);
        }
        else await gateway.ScanAsync([1, 2, 3], deadline.Token);
        await server;
    }

    [Fact]
    public async Task MissingScannerFailsClosedWithScannerErrorInsteadOfStorageError()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var failure = await Assert.ThrowsAsync<HttpException>(() => Create(port).ScanAsync([1], default));
        Assert.Equal(503, failure.StatusCode);
        Assert.Equal("DOCUMENT_SCANNER_UNAVAILABLE", failure.ErrorCode);
    }

    private static ClamAvFileSafetyScanner Create(int port)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["SupplierVerification:ClamAvHost"] = "127.0.0.1",
            ["SupplierVerification:ClamAvPort"] = port.ToString(),
        }).Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Production);
        return new(config, environment.Object, NullLogger<ClamAvFileSafetyScanner>.Instance);
    }

    private static async Task RespondAsync(TcpListener listener, string reply, CancellationToken ct)
    {
        using var client = await listener.AcceptTcpClientAsync(ct);
        await using var stream = client.GetStream();
        var command = new byte[10];
        await stream.ReadExactlyAsync(command, ct);
        Assert.Equal("zINSTREAM\0", Encoding.ASCII.GetString(command));
        var length = new byte[4];
        while (true)
        {
            await stream.ReadExactlyAsync(length, ct);
            var count = BinaryPrimitives.ReadInt32BigEndian(length);
            if (count == 0) break;
            var content = new byte[count];
            await stream.ReadExactlyAsync(content, ct);
        }
        await stream.WriteAsync(Encoding.ASCII.GetBytes(reply), ct);
    }
}
