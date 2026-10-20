namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Persistence operations for supplier onboarding and verification.</summary>
public interface ISupplierRepository
{
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SupplierProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SupplierProfile?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierProfile>> GetPendingProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierProfile>> GetVerifiedProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task SaveApplicationAsync(User user, SupplierProfile profile, IReadOnlyList<SupplierVerificationDocument> documents, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(User user, SupplierProfile profile, SupplierVerificationStatus expectedStatus,
        IReadOnlyList<SupplierVerificationDocument> documents, WorkflowEvent workflowEvent,
        Notification notification, AuditLog auditLog, CancellationToken cancellationToken = default);
}
