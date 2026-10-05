namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

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

    public async Task<IReadOnlyList<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        await context.SupplierVerificationDocuments.AsNoTracking()
            .Where(document => !document.IsDeleted && document.SupplierProfileId == profileId)
            .OrderBy(document => document.CreatedAt).ToListAsync(cancellationToken);

    public async Task SaveApplicationAsync(User user, SupplierProfile profile, IReadOnlyList<SupplierVerificationDocument> documents, CancellationToken cancellationToken = default)
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

    public async Task ReviewAsync(SupplierProfile profile, IReadOnlyList<SupplierVerificationDocument> documents, WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.SupplierProfiles.Update(profile);
        context.SupplierVerificationDocuments.UpdateRange(documents);
        context.WorkflowEvents.Add(workflowEvent);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
