namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>EF Core persistence for supplier onboarding and verification.</summary>
public sealed class SupplierRepository(ApplicationDbContext context) : ISupplierRepository
{
    public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

    public Task<SupplierProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId && !profile.IsDeleted, cancellationToken);

    public Task<SupplierProfile?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        context.SupplierProfiles.FirstOrDefaultAsync(profile => profile.Id == profileId && !profile.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<SupplierProfile>> GetPendingProfilesAsync(CancellationToken cancellationToken = default) =>
        await context.SupplierProfiles.AsNoTracking()
            .Where(profile => !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Pending)
            .OrderBy(profile => profile.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SupplierProfile>> GetVerifiedProfilesAsync(CancellationToken cancellationToken = default) =>
        await context.SupplierProfiles.AsNoTracking()
            .Where(profile => !profile.IsDeleted && profile.VerificationStatus == SupplierVerificationStatus.Verified)
            .OrderBy(profile => profile.CompanyName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        await context.SupplierVerificationDocuments.AsNoTracking()
            .Where(document => !document.IsDeleted && document.SupplierProfileId == profileId)
            .OrderBy(document => document.CreatedAt).ToListAsync(cancellationToken);

    public async Task SaveApplicationAsync(User user, SupplierProfile profile, IReadOnlyList<SupplierVerificationDocument> documents, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            context.Users.Update(user);
            if (profile.Id == Guid.Empty)
                context.SupplierProfiles.Add(profile);
            else
            {
                context.SupplierProfiles.Update(profile);
                var oldDocuments = await context.SupplierVerificationDocuments
                    .Where(document => document.SupplierProfileId == profile.Id && !document.IsDeleted)
                    .ToListAsync(cancellationToken);
                foreach (var document in oldDocuments)
                    document.IsDeleted = true;
            }

            await context.SaveChangesAsync(cancellationToken);
            foreach (var document in documents)
                document.SupplierProfileId = profile.Id;
            context.SupplierVerificationDocuments.AddRange(documents);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new HttpException(409, postgres.ConstraintName == "supplier_profiles_tax_code_key"
                ? MessageConstant.SupplierTaxCodeExists : MessageConstant.SupplierReviewConflict);
        }
    }

    public async Task ChangeStatusAsync(User user, SupplierProfile profile, SupplierVerificationStatus expectedStatus,
        IReadOnlyList<SupplierVerificationDocument> documents, WorkflowEvent workflowEvent,
        Notification notification, AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var changed = await context.SupplierProfiles
            .Where(x => x.Id == profile.Id && !x.IsDeleted && x.VerificationStatus == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.VerificationStatus, profile.VerificationStatus)
                .SetProperty(x => x.VerifiedAt, profile.VerifiedAt)
                .SetProperty(x => x.ReviewedAt, profile.ReviewedAt)
                .SetProperty(x => x.ReviewedByUserId, profile.ReviewedByUserId)
                .SetProperty(x => x.RejectionReason, profile.RejectionReason)
                .SetProperty(x => x.UpdatedAt, profile.UpdatedAt), cancellationToken);
        if (changed != 1) throw new HttpException(409, MessageConstant.SupplierReviewConflict);
        context.Entry(profile).State = EntityState.Detached;
        context.Users.Update(user);
        context.SupplierVerificationDocuments.UpdateRange(documents);
        context.WorkflowEvents.Add(workflowEvent);
        context.Notifications.Add(notification);
        context.AuditLogs.Add(auditLog);
        if (profile.VerificationStatus == SupplierVerificationStatus.Revoked)
        {
            await context.RefreshTokens.Where(token => token.UserId == user.Id && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, profile.ReviewedAt), cancellationToken);
            await context.RefreshSessions.Where(session => session.UserId == user.Id && session.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, profile.ReviewedAt), cancellationToken);
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
