namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

/// <summary>Purpose-separated read-all boundaries using the host's protected shared key ring.</summary>
public sealed class VerificationReadTokenProtector(IDataProtectionProvider provider) : IVerificationReadTokenProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SupplierVerification.NotificationRead.v1");
    public string Protect(VerificationReadBoundary boundary) => _protector.Protect(JsonSerializer.Serialize(boundary));
    public VerificationReadBoundary? Unprotect(string token)
    {
        try { return JsonSerializer.Deserialize<VerificationReadBoundary>(_protector.Unprotect(token)); }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or JsonException or ArgumentException) { return null; }
    }
}
