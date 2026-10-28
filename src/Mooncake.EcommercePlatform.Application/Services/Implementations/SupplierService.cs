namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper;

/// <summary>Owns verification state transitions; persistence and network adapters are abstractions.</summary>
public sealed class SupplierService(ISupplierRepository repository, IDateTimeProvider clock,
    ICloudinaryStorageService storage, IVerificationDocumentGateway documents, ITraceContext trace) : ISupplierService
{
    public Task<Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses.AuthenUserResponse> StartOnboardingAsync(
        Guid userId, CancellationToken ct) => repository.InTransactionAsync([userId], async token =>
    {
        var user = H.RequireActive(await repository.GetUserAsync(userId, token));
        H.Require(user.Role is UserRole.Customer or UserRole.Supplier && user.EmailVerifiedAt.HasValue, 403, C.Forbidden);
        if (user.RequestedRole != UserRole.Supplier)
        {
            user.RequestedRole = UserRole.Supplier;
            user.UpdatedAt = clock.UtcNow;
            repository.AddAudit(new AuditLog {
                Id = Guid.NewGuid(), ActorUserId = userId, EntityName = nameof(User),
                EntityId = userId.ToString(), Action = C.OnboardingStarted, CreatedAt = clock.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { requestedRole = UserRole.Supplier.ToString() })
            });
        }
        // Opting in never grants Supplier authority or creates a submitted verification revision.
        return H.AuthUser(user, await repository.GetProfileByUserIdAsync(userId, false, token));
    }, ct);

    public async Task<SupplierProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken ct = default)
    {
        H.RequireOnboarding(await repository.GetUserAsync(userId, ct));
        return await BuildProfileAsync(await repository.GetProfileByUserIdAsync(userId, false, ct), ct);
    }
    public async Task<SupplierProfileResponse> GetProfileAsync(Guid actorId, Guid profileId, CancellationToken ct)
    {
        H.RequireAdmin(await repository.GetUserAsync(actorId, ct));
        var profile = await repository.GetProfileByIdAsync(profileId, ct);
        H.Require(profile != null, 404, C.NotFound);
        return await BuildProfileAsync(profile, ct);
    }
    public async Task<VerificationPage<SupplierApplicationResponse>> ListAsync(Guid actorId, SupplierApplicationQuery query, CancellationToken ct)
    {
        H.RequireAdmin(await repository.GetUserAsync(actorId, ct));
        H.Require(query.Page is >= 1 and <= 100000 && query.PageSize is >= 1 and <= 100 &&
            query.Q?.Length is null or <= 100);
        H.Require(query.Sort is "submittedAtAsc" or "submittedAtDesc" or "companyNameAsc");
        H.Require(query.Status is null || Enum.IsDefined(query.Status.Value));
        return await repository.ListAsync(query, ct);
    }
    public async Task<SupplierSubmissionResponse> GetSubmissionAsync(Guid actorId, Guid? profileId, Guid submissionId, CancellationToken ct)
    {
        SupplierProfile? profile;
        if (profileId.HasValue)
        {
            H.RequireAdmin(await repository.GetUserAsync(actorId, ct));
            profile = await repository.GetProfileByIdAsync(profileId.Value, ct);
        }
        else
        {
            H.RequireOnboarding(await repository.GetUserAsync(actorId, ct));
            profile = await repository.GetProfileByUserIdAsync(actorId, false, ct);
        }
        var submission = await repository.GetSubmissionAsync(submissionId, ct);
        H.Require(profile != null && submission != null && submission.SupplierProfileId == profile.Id, 404, C.NotFound);
        var files = await repository.GetDocumentsAsync(profile!.Id, submissionId, ct);
        return H.Profile(profile, submission, files, []).CurrentSubmission!;
    }
    public async Task<VerificationPage<SupplierHistoryResponse>> HistoryAsync(Guid actorId, Guid? profileId, int page, int pageSize, CancellationToken ct)
    {
        H.Require(page is >= 1 and <= 100000 && pageSize is >= 1 and <= 100);
        SupplierProfile? profile;
        if (profileId.HasValue)
        {
            H.RequireAdmin(await repository.GetUserAsync(actorId, ct));
            profile = await repository.GetProfileByIdAsync(profileId.Value, ct);
            H.Require(profile != null, 404, C.NotFound);
        }
        else
        {
            H.RequireOnboarding(await repository.GetUserAsync(actorId, ct));
            profile = await repository.GetProfileByUserIdAsync(actorId, false, ct);
        }
        return profile == null ? new([], page, pageSize, 0) : await repository.HistoryAsync(profile.Id, page, pageSize, ct);
    }
    public async Task<VerificationCommandResponse> ApplyAsync(Guid userId, string key, ApplySupplierRequest request, CancellationToken ct)
    {
        request = H.Normalize(request);
        var parsedKey = H.ParseKey(key);
        return await repository.InTransactionAsync([userId], async token =>
        {
            var user = H.RequireOnboarding(await repository.GetUserAsync(userId, token));
            var replay = await ReplayAsync(userId, C.Submitted, userId, parsedKey, request, token);
            if (replay != null) return replay;
            H.Require(await repository.CountCommandsAsync(userId, true, clock.UtcNow.AddHours(-1), token) < 5, 429, C.RateLimited);
            var profile = await repository.GetProfileByUserIdAsync(userId, true, token);
            if (profile == null)
            {
                profile = new SupplierProfile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = clock.UtcNow };
                repository.AddProfile(profile);
            }
            H.CheckVersion(profile, request.ExpectedVersion);
            H.Require(profile.CurrentSubmissionId == null || profile.VerificationStatus != SupplierVerificationStatus.Pending, 409, C.AlreadyPending);
            H.Require(profile.VerificationStatus != SupplierVerificationStatus.Verified, 409, C.AlreadyVerified);
            H.Require(!await repository.TaxCodeExistsAsync(request.TaxCode, profile.Id, token), 409, C.TaxConflict);
            var selected = await repository.GetSelectedDocumentsAsync(profile.Id, request.DocumentIds, token);
            H.CheckDocuments(selected, request.DocumentIds.Count, userId, clock.UtcNow, true);
            var revision = await repository.GetNextRevisionAsync(profile.Id, token);
            var submission = new SupplierVerificationSubmission
            {
                Id = Guid.NewGuid(), SupplierProfileId = profile.Id, Revision = revision,
                CompanyName = request.CompanyName, TaxCode = request.TaxCode, Address = request.Address,
                Description = request.Description, Status = SupplierVerificationStatus.Pending,
                SubmittedAtUtc = clock.UtcNow, SubmittedByUserId = userId, CreatedAt = clock.UtcNow
            };
            repository.AddSubmission(submission);
            foreach (var doc in selected) { doc.SubmissionId = submission.Id; doc.ReviewStatus = C.Pending; }
            var previous = profile.CurrentSubmissionId == null ? C.NotSubmitted : profile.VerificationStatus.ToString();
            profile.CompanyName = request.CompanyName; profile.TaxCode = request.TaxCode;
            profile.Address = request.Address; profile.Description = request.Description;
            profile.CurrentSubmissionId = submission.Id; profile.LastSubmittedAtUtc = clock.UtcNow;
            profile.VerificationStatus = SupplierVerificationStatus.Pending;
            profile.ReviewedAt = null; profile.ReviewedByUserId = null; profile.RejectionReason = null;
            profile.Version++; profile.UpdatedAt = clock.UtcNow;
            RecordEvent(profile, userId, previous, revision == 1 ? C.Submitted : C.Resubmitted, null, false);
            var result = Result(profile, revision);
            StoreResult(userId, C.Submitted, userId, parsedKey, request, result);
            return result;
        }, ct);
    }
    public async Task<VerificationCommandResponse> ReviewAsync(Guid profileId, Guid actorId, string key, ReviewSupplierRequest request, CancellationToken ct)
    {
        H.Require(request.Approve.HasValue && request.ExpectedVersion is >= 0 && request.SubmissionId != Guid.Empty);
        var reason = request.Approve == false ? H.Reason(request.RejectionReason) : null;
        request = request with { RejectionReason = reason };
        var parsedKey = H.ParseKey(key);
        var initial = await repository.GetProfileByIdAsync(profileId, ct);
        H.Require(initial != null, 404, C.NotFound);
        return await repository.InTransactionAsync([actorId, initial!.UserId], async token =>
        {
            H.RequireAdmin(await repository.GetUserAsync(actorId, token), initial.UserId);
            var owner = H.RequireOnboarding(await repository.GetUserAsync(initial.UserId, token));
            var replay = await ReplayAsync(actorId, C.Approved, profileId, parsedKey, request, token);
            if (replay != null) return replay;
            H.Require(await repository.CountCommandsAsync(actorId, false, clock.UtcNow.AddMinutes(-1), token) < 60, 429, C.RateLimited);
            var profile = (await repository.GetProfileByUserIdAsync(owner.Id, true, token))!;
            H.CheckVersion(profile, request.ExpectedVersion);
            H.Require(profile.CurrentSubmissionId == request.SubmissionId &&
                profile.VerificationStatus == SupplierVerificationStatus.Pending, 409, C.Conflict);
            var submission = await repository.GetSubmissionAsync(request.SubmissionId, token);
            H.Require(submission?.SupplierProfileId == profile.Id && submission.Status == SupplierVerificationStatus.Pending, 409, C.Conflict);
            var snapshot = await repository.GetDocumentsAsync(profile.Id, submission!.Id, token);
            var selected = await repository.GetSelectedDocumentsAsync(profile.Id, snapshot.Select(x => x.Id).ToArray(), token);
            if (request.Approve == true) H.CheckDocuments(selected, snapshot.Count, owner.Id, clock.UtcNow, false);
            profile.VerificationStatus = request.Approve == true ? SupplierVerificationStatus.Verified : SupplierVerificationStatus.Rejected;
            profile.ReviewedAt = clock.UtcNow; profile.ReviewedByUserId = actorId; profile.RejectionReason = reason;
            profile.VerifiedAt = request.Approve == true ? clock.UtcNow : null;
            if (request.Approve == true) profile.LastApprovedSubmissionId = submission.Id;
            profile.Version++; profile.UpdatedAt = clock.UtcNow;
            submission.Status = profile.VerificationStatus; submission.ReviewedAtUtc = clock.UtcNow;
            submission.ReviewedByUserId = actorId; submission.RejectionReason = reason;
            owner.Role = request.Approve == true ? UserRole.Supplier : UserRole.Customer;
            owner.UpdatedAt = clock.UtcNow;
            foreach (var doc in selected)
            {
                doc.ReviewStatus = request.Approve == true ? "Approved" : "Rejected";
                doc.ReviewedAt = clock.UtcNow; doc.ReviewedByUserId = actorId; doc.RejectionReason = reason;
            }
            RecordEvent(profile, actorId, C.Pending, request.Approve == true ? C.Approved : C.Rejected, reason, true);
            var result = Result(profile, submission.Revision);
            StoreResult(actorId, C.Approved, profileId, parsedKey, request, result);
            return result;
        }, ct);
    }
    public async Task<VerificationCommandResponse> RevokeAsync(Guid profileId, Guid actorId, string key, RevokeSupplierRequest request, CancellationToken ct)
    {
        request = request with { Reason = H.Reason(request.Reason) };
        var parsedKey = H.ParseKey(key);
        var initial = await repository.GetProfileByIdAsync(profileId, ct);
        H.Require(initial != null, 404, C.NotFound);
        return await repository.InTransactionAsync([actorId, initial!.UserId], async token =>
        {
            H.RequireAdmin(await repository.GetUserAsync(actorId, token), initial.UserId);
            var owner = await repository.GetUserAsync(initial.UserId, token);
            H.Require(owner is { IsDeleted: false }, 404, C.NotFound);
            var replay = await ReplayAsync(actorId, C.Revoked, profileId, parsedKey, request, token);
            if (replay != null) return replay;
            H.Require(await repository.CountCommandsAsync(actorId, false, clock.UtcNow.AddMinutes(-1), token) < 60, 429, C.RateLimited);
            var profile = (await repository.GetProfileByUserIdAsync(owner!.Id, true, token))!;
            H.CheckVersion(profile, request.ExpectedVersion);
            H.Require(profile.VerificationStatus == SupplierVerificationStatus.Verified, 409, C.Conflict);
            profile.VerificationStatus = SupplierVerificationStatus.Revoked;
            profile.RevokedAtUtc = clock.UtcNow; profile.RevokedByUserId = actorId;
            profile.RevocationReason = request.Reason; profile.Version++; profile.UpdatedAt = clock.UtcNow;
            owner.Role = UserRole.Customer; owner.UpdatedAt = clock.UtcNow;
            RecordEvent(profile, actorId, SupplierVerificationStatus.Verified.ToString(), C.Revoked, request.Reason, true);
            var submission = profile.CurrentSubmissionId.HasValue ? await repository.GetSubmissionAsync(profile.CurrentSubmissionId.Value, token) : null;
            var result = Result(profile, submission?.Revision ?? 0);
            StoreResult(actorId, C.Revoked, profileId, parsedKey, request, result);
            return result;
        }, ct);
    }
    public async Task<SupplierDocumentResponse> UploadAsync(Guid userId, string key, Stream stream, string fileName,
        string documentType, DateOnly? expiresOn, CancellationToken ct)
    {
        H.RequireOnboarding(await repository.GetUserAsync(userId, ct));
        var parsedKey = H.ParseKey(key);
        H.Require(documentType is C.BusinessLicense or C.FoodSafety or C.OptionalOther);
        H.Require(!expiresOn.HasValue || expiresOn.Value >= DateOnly.FromDateTime(clock.UtcNow));
        var bytes = await H.ReadBoundedAsync(stream, ct);
        H.Require(bytes.Length <= documents.MaxFileBytes, 413, C.DocumentTooLarge);
        var (name, mime) = H.ValidateContent(bytes, fileName);
        var hash = H.Hash(new { ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), name, documentType, expiresOn });
        var doc = await repository.InTransactionAsync([userId], async token =>
        {
            H.RequireOnboarding(await repository.GetUserAsync(userId, token));
            var existing = await repository.GetUploadAsync(userId, parsedKey, token);
            if (existing != null)
            {
                H.Require(existing.UploadRequestHash == hash && !existing.IsDeleted, 409, C.KeyMismatch);
                H.Require(existing.UploadStatus != C.Uploading || existing.UpdatedAt < clock.UtcNow.AddMinutes(-5), 409, C.UploadInProgress);
                if (existing.UploadStatus != C.Ready) { existing.UploadStatus = C.Uploading; existing.UpdatedAt = clock.UtcNow; }
                return existing;
            }
            var usage = await repository.UploadUsageAsync(userId, clock.UtcNow.AddHours(-1), clock.UtcNow.AddMinutes(-5), token);
            H.Require(usage.Recent < 20 && usage.Active < 2, 429, C.RateLimited);
            var profile = await repository.GetProfileByUserIdAsync(userId, true, token);
            if (profile == null) { profile = new() { Id = Guid.NewGuid(), UserId = userId, CreatedAt = clock.UtcNow }; repository.AddProfile(profile); }
            H.Require(profile.CurrentSubmissionId == null || profile.VerificationStatus is SupplierVerificationStatus.Rejected or SupplierVerificationStatus.Revoked, 409, C.Conflict);
            var staged = await repository.GetDocumentsAsync(profile.Id, null, token);
            H.Require(staged.Count < C.MaxDocuments, 429, C.DocumentTooLarge);
            var created = new SupplierVerificationDocument { Id = Guid.NewGuid(), SupplierProfileId = profile.Id,
                OwnerUserId = userId, DocumentType = documentType, OriginalFileName = name, ContentType = mime,
                FileSizeBytes = bytes.Length, UploadOperationId = parsedKey, UploadRequestHash = hash,
                UploadStatus = C.Uploading, ExpiresOn = expiresOn, SubmittedAt = clock.UtcNow, CreatedAt = clock.UtcNow };
            repository.AddDocument(created);
            return created;
        }, ct);
        if (doc.UploadStatus == C.Ready) return H.Document(doc);
        try
        {
            using var content = new MemoryStream(bytes, false);
            var uploaded = await storage.UploadRawFileAsync(content, name, ct);
            return await repository.InTransactionAsync([userId], async token =>
            {
                var saved = (await repository.GetDocumentAsync(doc.Id, token))!;
                H.Require(!saved.IsDeleted, 409, C.Conflict);
                saved.FileUrl = uploaded.Url; saved.ProviderPublicId = uploaded.PublicId;
                saved.ProviderResourceType = uploaded.ResourceType; saved.ProviderDeliveryType = uploaded.DeliveryType;
                saved.Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
                saved.UploadStatus = C.Ready; saved.ValidationCompletedAtUtc = clock.UtcNow; saved.UpdatedAt = clock.UtcNow;
                return H.Document(saved);
            }, ct);
        }
        catch (Exception failure)
        {
            await repository.InTransactionAsync([userId], async token =>
            {
                var saved = await repository.GetDocumentAsync(doc.Id, token);
                if (saved != null && saved.UploadStatus == C.Uploading) {
                    saved.UploadStatus = failure is Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException { StatusCode: 415 } ?
                        C.Quarantined : C.Failed;
                    saved.UpdatedAt = clock.UtcNow;
                }
                return true;
            }, CancellationToken.None);
            if (failure is OperationCanceledException && ct.IsCancellationRequested) throw;
            if (failure is Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException { StatusCode: 415 or 503 }) throw;
            throw new Mooncake.EcommercePlatform.Application.Common.Exceptions.HttpException(503, C.Error, C.StorageUnavailable);
        }
    }
    public async Task<SupplierDocumentResponse> GetDocumentAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        H.RequireOnboarding(await repository.GetUserAsync(userId, ct));
        var doc = await repository.GetDocumentAsync(documentId, ct);
        H.Require(doc is { IsDeleted: false } && doc.OwnerUserId == userId, 404, C.NotFound);
        return H.Document(doc!);
    }
    public async Task DeleteDocumentAsync(Guid userId, Guid documentId, string key, CancellationToken ct)
    {
        H.ParseKey(key);
        await repository.InTransactionAsync([userId], async token =>
        {
            H.RequireOnboarding(await repository.GetUserAsync(userId, token));
            var doc = await repository.GetDocumentAsync(documentId, token);
            H.Require(doc != null && doc.OwnerUserId == userId, 404, C.NotFound);
            H.Require(doc!.SubmissionId == null && doc.UploadStatus != C.Uploading, 409, C.DocumentAttached);
            doc.IsDeleted = true; doc.UpdatedAt = clock.UtcNow;
            return true;
        }, ct);
    }
    public async Task<(byte[] Content, string ContentType, string FileName)> ReadDocumentAsync(Guid actorId, Guid documentId, CancellationToken ct)
    {
        var actor = H.RequireActive(await repository.GetUserAsync(actorId, ct));
        var doc = await repository.GetDocumentAsync(documentId, ct);
        H.Require(doc is { IsDeleted: false } && (doc.OwnerUserId == actorId || actor.Role == UserRole.Admin), 404, C.NotFound);
        H.Require(doc!.UploadStatus == C.Ready && doc.ProviderDeliveryType == "authenticated" && doc.ProviderPublicId != null, 409, C.DocumentNotReady);
        return (await documents.ReadAsync(doc, ct), doc.ContentType ?? "application/octet-stream", doc.OriginalFileName ?? "document");
    }
    private async Task<SupplierProfileResponse> BuildProfileAsync(SupplierProfile? profile, CancellationToken ct)
    {
        var submission = profile?.CurrentSubmissionId is Guid id ? await repository.GetSubmissionAsync(id, ct) : null;
        var snapshot = submission == null ? [] : await repository.GetDocumentsAsync(profile!.Id, submission.Id, ct);
        var staged = profile == null ? [] : await repository.GetDocumentsAsync(profile.Id, null, ct);
        return H.Profile(profile, submission, snapshot, staged);
    }
    private async Task<VerificationCommandResponse?> ReplayAsync(Guid actor, string operation, Guid resource,
        Guid key, object request, CancellationToken ct)
    {
        var old = await repository.GetIdempotencyAsync(actor, operation, resource, key, ct);
        if (old == null) return null;
        H.Require(old.RequestHash == H.Hash(request) && old.ExpiresAtUtc > clock.UtcNow, 409, C.KeyMismatch);
        return JsonSerializer.Deserialize<VerificationCommandResponse>(old.ResultJson)! with { IdempotentReplay = true };
    }
    private VerificationCommandResponse Result(SupplierProfile profile, int revision) =>
        new(profile.Id, profile.CurrentSubmissionId, revision, profile.Version, profile.VerificationStatus, C.Queued);
    private void StoreResult(Guid actor, string operation, Guid resource, Guid key, object request, VerificationCommandResponse result) =>
        repository.AddIdempotency(new() { Id = Guid.NewGuid(), ActorUserId = actor, Operation = operation, ResourceId = resource,
            Key = key, RequestHash = H.Hash(request), ResultJson = JsonSerializer.Serialize(result), ExpiresAtUtc = clock.UtcNow.AddHours(24), CreatedAt = clock.UtcNow });
    private void RecordEvent(SupplierProfile profile, Guid actor, string previous, string type, string? reason, bool email) =>
        SupplierVerificationEventHelper.Append(repository, clock.UtcNow, trace.TraceId, profile, actor, previous, type, reason, email);
}
