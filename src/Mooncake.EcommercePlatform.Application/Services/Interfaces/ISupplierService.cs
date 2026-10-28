namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

/// <summary>Versioned supplier verification commands and authorized reads.</summary>
public interface ISupplierService
{
    Task<Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses.AuthenUserResponse> StartOnboardingAsync(Guid userId, CancellationToken cancellationToken);
    Task<SupplierProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SupplierProfileResponse> GetProfileAsync(Guid actorId, Guid profileId, CancellationToken cancellationToken);
    Task<SupplierSubmissionResponse> GetSubmissionAsync(Guid actorId, Guid? profileId, Guid submissionId, CancellationToken cancellationToken);
    Task<VerificationPage<SupplierApplicationResponse>> ListAsync(Guid actorId, SupplierApplicationQuery query, CancellationToken cancellationToken);
    Task<VerificationPage<SupplierHistoryResponse>> HistoryAsync(Guid actorId, Guid? profileId, int page, int pageSize, CancellationToken cancellationToken);
    Task<VerificationCommandResponse> ApplyAsync(Guid userId, string key, ApplySupplierRequest request, CancellationToken cancellationToken);
    Task<VerificationCommandResponse> ReviewAsync(Guid profileId, Guid actorId, string key, ReviewSupplierRequest request, CancellationToken cancellationToken);
    Task<VerificationCommandResponse> RevokeAsync(Guid profileId, Guid actorId, string key, RevokeSupplierRequest request, CancellationToken cancellationToken);
    Task<SupplierDocumentResponse> UploadAsync(Guid userId, string key, Stream stream, string fileName, string documentType, DateOnly? expiresOn, CancellationToken cancellationToken);
    Task<SupplierDocumentResponse> GetDocumentAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
    Task DeleteDocumentAsync(Guid userId, Guid documentId, string key, CancellationToken cancellationToken);
    Task<(byte[] Content, string ContentType, string FileName)> ReadDocumentAsync(Guid actorId, Guid documentId, CancellationToken cancellationToken);
}
