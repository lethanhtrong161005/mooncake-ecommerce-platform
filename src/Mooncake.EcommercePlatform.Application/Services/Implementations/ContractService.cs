namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class ContractService(
    IContractRepository repository,
    IAuthService authService,
    IContractFileStorage fileStorage,
    Microsoft.Extensions.Options.IOptions<SupabaseStorageOptions> storageOptions,
    IDateTimeProvider clock,
    ILogger<ContractService> logger) : IContractService
{
    private readonly SupabaseStorageOptions storage = storageOptions.Value;

    public async Task<IReadOnlyList<ContractResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default)
    {
        var contracts = await repository.GetContractsForUserAsync(userId, supplier, cancellationToken);
        return contracts.Select(ToResponse).ToArray();
    }

    public async Task<ContractResponse> GetAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        return ToResponse(contract);
    }

    public async Task<ContractResponse> RequestSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status != ContractStatus.PendingSignature)
            throw new HttpException(409, "The contract must be pending signature before requesting an OTP.");
        if (await repository.HasSignedAsync(contract, userId, cancellationToken))
            throw new HttpException(409, "This party has already signed the contract.");
        await authService.RequestContractSignatureOtpAsync(userId, contractId, cancellationToken);
        return ToResponse(contract);
    }

    public async Task<ContractResponse> BeginSignatureAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var owned = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (string.IsNullOrWhiteSpace(owned.SignedDocumentUrl))
            throw new HttpException(409, "Vui lòng tải file hợp đồng PDF trước khi gửi ký.");
        if (!await repository.BeginSignatureAsync(contractId, userId, cancellationToken))
        {
            var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
            if (contract.Status != ContractStatus.PendingSignature)
                throw new HttpException(409, "Only a draft contract can be sent for signature.");
        }
        return await GetAsync(userId, contractId, cancellationToken);
    }

    public async Task<ContractResponse> UploadDocumentAsync(Guid userId, Guid contractId, Stream content,
        string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status != ContractStatus.Draft)
            throw new HttpException(409, "Chỉ được thay đổi file khi hợp đồng còn ở trạng thái nháp.");
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new HttpException(400, "File hợp đồng phải có định dạng PDF.");
        if (fileSize <= 0 || fileSize > storage.MaxContractFileBytes)
            throw new HttpException(400, $"File hợp đồng không được vượt quá {storage.MaxContractFileBytes / 1024 / 1024} MB.");

        await using var buffer = new MemoryStream((int)fileSize);
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != fileSize || buffer.Length < 5)
            throw new HttpException(400, "File hợp đồng không đầy đủ hoặc bị lỗi.");
        var bytes = buffer.ToArray();
        if (!bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            throw new HttpException(400, "Nội dung file không phải tài liệu PDF hợp lệ.");

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        var objectPath = $"{contract.Id:N}/{clock.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.pdf";
        buffer.Position = 0;
        await fileStorage.UploadAsync(objectPath, buffer, "application/pdf", cancellationToken);
        ContractDocumentSaveResult? saved;
        try
        {
            saved = await repository.SaveDocumentAsync(contract.Id, userId, objectPath, sha256, cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(objectPath, cancellationToken);
            throw;
        }
        if (saved is null)
        {
            await TryDeleteAsync(objectPath, cancellationToken);
            throw new HttpException(409, "Trạng thái hợp đồng đã thay đổi. Vui lòng tải lại.");
        }
        if (!string.IsNullOrWhiteSpace(saved.PreviousObjectPath))
            await TryDeleteAsync(saved.PreviousObjectPath, cancellationToken);
        return ToResponse(saved.Contract);
    }

    public async Task<ContractDocumentResponse> GetDocumentDownloadAsync(Guid userId, Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (string.IsNullOrWhiteSpace(contract.SignedDocumentUrl) || string.IsNullOrWhiteSpace(contract.SignedDocumentSha256))
            throw new HttpException(404, "Hợp đồng chưa có file PDF.");
        var url = await fileStorage.CreateSignedDownloadUrlAsync(contract.SignedDocumentUrl, cancellationToken);
        return new ContractDocumentResponse(url, clock.UtcNow.AddSeconds(storage.SignedUrlLifetimeSeconds),
            contract.SignedDocumentSha256);
    }

    public async Task<ContractResponse> SignAsync(Guid userId, Guid contractId, string code, bool confirmTerms, string? ipAddress,
        string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!confirmTerms) throw new HttpException(400, "Confirming the contract terms is required.");
        var contract = await GetOwnedAsync(userId, contractId, cancellationToken);
        if (contract.Status != ContractStatus.PendingSignature)
            throw new HttpException(409, "The contract is not awaiting signatures.");
        if (await repository.HasSignedAsync(contract, userId, cancellationToken))
            return ToResponse(contract);
        await authService.VerifyContractSignatureOtpAsync(userId, contractId, code, cancellationToken);
        var signed = await repository.SignAsync(contractId, userId, ipAddress, userAgent, cancellationToken)
                     ?? throw new HttpException(409, "The contract state changed while signing. Refresh and try again.");
        return ToResponse(signed);
    }

    private async Task<Contract> GetOwnedAsync(Guid userId, Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await repository.GetContractAsync(contractId, cancellationToken);
        if (contract is null || contract.IsDeleted || !await repository.IsPartyAsync(contract, userId, cancellationToken))
            throw new HttpException(404, "Contract was not found.");
        return contract;
    }

    private static ContractResponse ToResponse(Contract contract) => new(contract.Id, contract.ContractNumber, contract.RfqId,
        contract.Title, contract.TotalValue, contract.DeliveryDeadline, contract.TermsAndConditions, contract.Status,
        contract.SignedByCustomerAt, contract.SignedBySupplierAt,
        !string.IsNullOrWhiteSpace(contract.SignedDocumentUrl), contract.SignedDocumentSha256);

    private async Task TryDeleteAsync(string objectPath, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteAsync(objectPath, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not delete obsolete contract object {ObjectPath}", objectPath);
        }
    }
}
