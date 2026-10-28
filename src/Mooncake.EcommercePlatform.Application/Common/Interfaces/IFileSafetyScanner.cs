namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Shared fail-closed scanner for legal documents and shop branding.</summary>
public interface IFileSafetyScanner
{
    Task ScanAsync(byte[] content, CancellationToken ct);
}
