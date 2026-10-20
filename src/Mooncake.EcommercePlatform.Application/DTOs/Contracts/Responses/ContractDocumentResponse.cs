namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;

public sealed record ContractDocumentResponse(
    string DownloadUrl,
    DateTime ExpiresAtUtc,
    string Sha256);
