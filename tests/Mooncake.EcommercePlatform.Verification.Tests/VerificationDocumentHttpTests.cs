namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;

public sealed class VerificationDocumentHttpTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    [PostgresFact]
    public async Task RealUploadAcceptsAllFormatsAndOriginalBytesButRejectsInvalidSizes()
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test",
            Role = UserRole.Customer, RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = DateTime.UtcNow };
        await using (var seed = fixture.Create()) { seed.Users.Add(user); await seed.SaveChangesAsync(); }
        var acceptedBytes = PdfFixtureHelper.Create("/OpenAction [3 0 R /XYZ null null 1]");
        byte[]? storedBytes = null;
        var storage = new Mock<ICloudinaryStorageService>();
        storage.Setup(x => x.UploadRawFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream content, string _, CancellationToken ct) => {
                using var copy = new MemoryStream(); await content.CopyToAsync(copy, ct); storedBytes = copy.ToArray();
                return new CloudinaryUploadResponse("https://example.test/private-document", "fixture/document", "raw", "authenticated", storedBytes.Length);
            });
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(SuppliersController).Assembly);
        builder.Services.AddAuthorization(); builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); builder.Services.AddProblemDetails();
        builder.Services.AddRateLimiter(options => options.AddPolicy("supplier-verification", _ =>
            RateLimitPartition.GetNoLimiter("pdf-test")));
        builder.Services.AddScoped(_ => fixture.Create());
        builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
        builder.Services.AddScoped<ISupplierService, SupplierService>();
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => DateTime.UtcNow);
        builder.Services.AddSingleton(clock.Object); builder.Services.AddSingleton(Mock.Of<ITraceContext>());
        builder.Services.AddSingleton(storage.Object);
        builder.Services.AddSingleton<IVerificationDocumentGateway>(new VerificationDocumentGateway(
            Mock.Of<ICloudinaryUploadApi>(), new HttpClient(), clock.Object,
            Options.Create(new CloudinaryOptions { MaxFileSizeBytes = 10 * 1024 * 1024 })));
        await using var app = builder.Build();
        app.UseExceptionHandler(); app.UseRouting();
        app.Use(async (context, next) => {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "TestOnly"));
            await next();
        });
        app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        async Task<HttpResponseMessage> Upload(byte[] bytes, string name = "business-license.PDF") {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", name);
            form.Add(new StringContent("BusinessLicense"), "documentType");
            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/v1/suppliers/me/documents") { Content = form };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            return await client.SendAsync(request);
        }
        var accepted = await Upload(acceptedBytes);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(acceptedBytes, storedBytes);
        Assert.Equal("Ready", JsonDocument.Parse(await accepted.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("uploadStatus").GetString());
        Assert.Equal(HttpStatusCode.Created, (await Upload(PdfFixtureHelper.Create("/OpenAction << /S /JavaScript /JS (this.print()) >>"))).StatusCode);
        foreach (var name in new[] { "report.docx", "archive.zip", "data.custom", "README", "not-really-a-pdf.pdf", "program.exe" }) {
            byte[] payload = [0, 1, 2, 3, 4];
            Assert.Equal(HttpStatusCode.Created, (await Upload(payload, name)).StatusCode);
            Assert.Equal(payload, storedBytes);
        }
        Assert.Equal(HttpStatusCode.Created, (await Upload(new byte[10 * 1024 * 1024], "maximum.bin")).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Upload([])).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Upload(new byte[10 * 1024 * 1024 + 1])).StatusCode);
        storage.Verify(x => x.UploadRawFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(9));
        await using var read = fixture.Create();
        Assert.Equal(9, await read.SupplierVerificationDocuments.CountAsync(x => x.OwnerUserId == user.Id && x.UploadStatus == "Ready"));
        Assert.Equal(0, await read.SupplierVerificationDocuments.CountAsync(x => x.OwnerUserId == user.Id && x.UploadStatus == "Quarantined"));
        await app.StopAsync();
    }
}
