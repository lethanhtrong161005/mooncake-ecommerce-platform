namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence primitives; application services decide workflow invariants inside the transaction callback.</summary>
public interface ISupplierRepository
{
    Task<T> InTransactionAsync<T>(IReadOnlyList<Guid> userIds, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierProfile?> GetProfileByUserIdAsync(Guid userId, bool forUpdate, CancellationToken cancellationToken);
    Task<SupplierProfile?> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierVerificationSubmission?> GetSubmissionAsync(Guid id, CancellationToken cancellationToken);
    Task<int> GetNextRevisionAsync(Guid profileId, CancellationToken cancellationToken);
    Task<List<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, Guid? submissionId, CancellationToken cancellationToken);
    Task<List<SupplierVerificationDocument>> GetSelectedDocumentsAsync(Guid profileId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
    Task<SupplierVerificationDocument?> GetDocumentAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierVerificationDocument?> GetUploadAsync(Guid userId, Guid operationId, CancellationToken cancellationToken);
    Task<bool> TaxCodeExistsAsync(string taxCode, Guid exceptProfileId, CancellationToken cancellationToken);
    Task<int> CountCommandsAsync(Guid actor, bool submission, DateTime since, CancellationToken cancellationToken);
    Task<(int Recent, int Active)> UploadUsageAsync(Guid actor, DateTime since, DateTime activeSince, CancellationToken cancellationToken);
    Task<VerificationIdempotencyRecord?> GetIdempotencyAsync(Guid actor, string operation, Guid resource, Guid key, CancellationToken cancellationToken);
    Task<VerificationPage<SupplierApplicationResponse>> ListAsync(SupplierApplicationQuery query, CancellationToken cancellationToken);
    Task<VerificationPage<SupplierHistoryResponse>> HistoryAsync(Guid profileId, int page, int pageSize, CancellationToken cancellationToken);
    void AddProfile(SupplierProfile profile);
    void AddSubmission(SupplierVerificationSubmission submission);
    void AddDocument(SupplierVerificationDocument document);
    void AddEvent(WorkflowEvent workflowEvent);
    void AddAudit(AuditLog audit);
    void AddOutbox(VerificationOutboxMessage message);
    void AddIdempotency(VerificationIdempotencyRecord record);
}
