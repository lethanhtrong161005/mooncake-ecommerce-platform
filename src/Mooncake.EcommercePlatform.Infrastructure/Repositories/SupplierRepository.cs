namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Npgsql;

/// <summary>Bounded projections and parameterized PostgreSQL locking for verification.</summary>
public sealed class SupplierRepository(ApplicationDbContext context) : ISupplierRepository
{
    public async Task<T> InTransactionAsync<T>(IReadOnlyList<Guid> userIds,
        Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in userIds.Distinct().Order())
            {
                // All writers participating in eligibility changes use the same User-first lock order.
                var user = await context.Users.FromSqlInterpolated(
                    $"SELECT * FROM users WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
                if (user is not null) await context.Entry(user).ReloadAsync(cancellationToken);
            }
            var result = await work(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw new HttpException(409, SupplierVerificationConstants.Error, SupplierVerificationConstants.Conflict);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pg &&
            pg.ConstraintName == "supplier_profiles_tax_code_key")
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw new HttpException(409, SupplierVerificationConstants.Error, SupplierVerificationConstants.TaxConflict);
        }
    }
    public Task<User?> GetUserAsync(Guid id, CancellationToken ct) =>
        context.Users.Local.FirstOrDefault(x => x.Id == id && !x.IsDeleted) is { } tracked ?
            Task.FromResult<User?>(tracked) :
            context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    public Task<SupplierProfile?> GetProfileByUserIdAsync(Guid userId, bool forUpdate, CancellationToken ct) =>
        forUpdate ? context.SupplierProfiles.FromSqlInterpolated(
            $"SELECT * FROM supplier_profiles WHERE user_id = {userId} AND is_deleted = false FOR UPDATE").SingleOrDefaultAsync(ct) :
            context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, ct);
    public Task<SupplierProfile?> GetProfileByIdAsync(Guid id, CancellationToken ct) =>
        context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    public Task<SupplierVerificationSubmission?> GetSubmissionAsync(Guid id, CancellationToken ct) =>
        context.SupplierVerificationSubmissions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    public async Task<int> GetNextRevisionAsync(Guid profileId, CancellationToken ct) =>
        (await context.SupplierVerificationSubmissions.Where(x => x.SupplierProfileId == profileId)
            .MaxAsync(x => (int?)x.Revision, ct) ?? 0) + 1;
    public Task<List<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, Guid? submissionId, CancellationToken ct) =>
        context.SupplierVerificationDocuments.AsNoTracking().Where(x => x.SupplierProfileId == profileId &&
            x.SubmissionId == submissionId && !x.IsDeleted).OrderBy(x => x.CreatedAt).ToListAsync(ct);
    public Task<List<SupplierVerificationDocument>> GetSelectedDocumentsAsync(Guid profileId, IReadOnlyList<Guid> ids, CancellationToken ct) =>
        context.SupplierVerificationDocuments.Where(x => x.SupplierProfileId == profileId && ids.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct);
    public Task<SupplierVerificationDocument?> GetDocumentAsync(Guid id, CancellationToken ct) =>
        context.SupplierVerificationDocuments.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<SupplierVerificationDocument?> GetUploadAsync(Guid userId, Guid operationId, CancellationToken ct) =>
        context.SupplierVerificationDocuments.FirstOrDefaultAsync(x => x.OwnerUserId == userId && x.UploadOperationId == operationId, ct);
    public Task<bool> TaxCodeExistsAsync(string taxCode, Guid exceptProfileId, CancellationToken ct) =>
        context.SupplierProfiles.AnyAsync(x => x.TaxCode == taxCode && x.Id != exceptProfileId, ct);
    public Task<int> CountCommandsAsync(Guid actor, bool submission, DateTime since, CancellationToken ct) =>
        context.WorkflowEvents.AsNoTracking().CountAsync(x => x.ActorUserId == actor &&
            x.AggregateType == SupplierVerificationConstants.Aggregate && x.CreatedAt >= since &&
            (submission ? x.EventType == SupplierVerificationConstants.Submitted || x.EventType == SupplierVerificationConstants.Resubmitted :
                x.EventType == SupplierVerificationConstants.Approved || x.EventType == SupplierVerificationConstants.Rejected ||
                x.EventType == SupplierVerificationConstants.Revoked), ct);
    public async Task<(int Recent, int Active)> UploadUsageAsync(Guid actor, DateTime since, DateTime activeSince, CancellationToken ct)
    {
        var usage = await context.SupplierVerificationDocuments.AsNoTracking()
            .Where(x => x.OwnerUserId == actor && x.CreatedAt >= since).GroupBy(x => 1)
            .Select(g => new { Recent = g.Count(), Active = g.Count(x => x.UploadStatus == SupplierVerificationConstants.Uploading &&
                x.UpdatedAt >= activeSince) }).SingleOrDefaultAsync(ct);
        return (usage?.Recent ?? 0, usage?.Active ?? 0);
    }
    public Task<VerificationIdempotencyRecord?> GetIdempotencyAsync(Guid actor, string operation, Guid resource, Guid key, CancellationToken ct) =>
        context.VerificationIdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.ActorUserId == actor &&
            x.Operation == operation && x.ResourceId == resource && x.Key == key, ct);
    public async Task<VerificationPage<SupplierApplicationResponse>> ListAsync(SupplierApplicationQuery request, CancellationToken ct)
    {
        var rows = context.SupplierProfiles.AsNoTracking().Where(x => !x.IsDeleted && x.CurrentSubmissionId != null);
        if (request.Status.HasValue) rows = rows.Where(x => x.VerificationStatus == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var search = request.Q.Trim().ToLowerInvariant();
            rows = rows.Where(x => (x.CompanyName ?? "").ToLower().Contains(search) || (x.TaxCode ?? "").Contains(search));
        }
        var count = await rows.CountAsync(ct);
        var ordered = request.Sort switch
        {
            "submittedAtDesc" => rows.OrderByDescending(x => x.LastSubmittedAtUtc).ThenBy(x => x.Id),
            "companyNameAsc" => rows.OrderBy(x => x.CompanyName).ThenBy(x => x.Id),
            _ => rows.OrderBy(x => x.LastSubmittedAtUtc).ThenBy(x => x.Id)
        };
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new SupplierApplicationResponse(x.Id, x.CurrentSubmissionId,
                context.SupplierVerificationSubmissions.Where(s => s.Id == x.CurrentSubmissionId).Select(s => s.Revision).FirstOrDefault(),
                x.Version, x.CompanyName, x.TaxCode, x.VerificationStatus, x.LastSubmittedAtUtc, x.ReviewedAt,
                context.SupplierVerificationDocuments.Count(d => d.SubmissionId == x.CurrentSubmissionId && !d.IsDeleted))).ToListAsync(ct);
        return new(items, request.Page, request.PageSize, count);
    }
    public async Task<VerificationPage<SupplierHistoryResponse>> HistoryAsync(Guid profileId, int page, int pageSize, CancellationToken ct)
    {
        var rows = context.WorkflowEvents.AsNoTracking().Where(x => x.AggregateType == SupplierVerificationConstants.Aggregate &&
            x.AggregateId == profileId && !x.IsDeleted);
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierHistoryResponse(
                x.Id, x.EventType, x.FromStatus, x.ToStatus, x.ActorUserId, x.Reason, x.CreatedAt, x.Metadata)).ToListAsync(ct);
        return new(items, page, pageSize, count);
    }
    public void AddProfile(SupplierProfile value) => context.SupplierProfiles.Add(value);
    public void AddSubmission(SupplierVerificationSubmission value) => context.SupplierVerificationSubmissions.Add(value);
    public void AddDocument(SupplierVerificationDocument value) => context.SupplierVerificationDocuments.Add(value);
    public void AddEvent(WorkflowEvent value) => context.WorkflowEvents.Add(value);
    public void AddAudit(AuditLog value) => context.AuditLogs.Add(value);
    public void AddOutbox(VerificationOutboxMessage value) => context.VerificationOutboxMessages.Add(value);
    public void AddIdempotency(VerificationIdempotencyRecord value) => context.VerificationIdempotencyRecords.Add(value);
}
