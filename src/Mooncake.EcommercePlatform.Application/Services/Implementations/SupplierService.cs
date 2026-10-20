namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements supplier applications and admin review.</summary>
public sealed class SupplierService(
    ISupplierRepository repository, IDateTimeProvider dateTimeProvider,
    IEmailSender emailSender, ILogger<SupplierService> logger) : ISupplierService
{
    private const string AggregateType = "SupplierProfile";
    private static readonly HashSet<string> RequiredDocumentTypes =
        ["BusinessLicense", "FoodSafetyCertificate"];

    public async Task<SupplierProfileResponse> ApplyAsync(Guid userId, ApplySupplierRequest request, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetUserAsync(userId, cancellationToken)
                   ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        if (!user.IsActive || user.IsDeleted)
            throw new HttpException(403, MessageConstant.SupplierAccountInactive);
        if (user.Role == UserRole.Admin)
            throw new HttpException(403, MessageConstant.SupplierApplyInvalid);

        if (request is null || string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.TaxCode) || string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.ContactEmail) || request.Documents is null)
            throw new HttpException(400, MessageConstant.SupplierApplyInvalid);
        var contactEmail = request.ContactEmail.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(contactEmail) || contactEmail.Length > 255 ||
            request.CompanyName.Trim().Length > 255 || request.TaxCode.Trim().Length > 50 ||
            request.Address.Trim().Length > 500 || request.Description?.Trim().Length > 2000)
            throw new HttpException(400, MessageConstant.SupplierApplyInvalid);
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.TaxCode.Trim(), @"^[0-9]{10}(?:-[0-9]{3})?$"))
            throw new HttpException(400, MessageConstant.SupplierTaxCodeInvalid);
        if (request.Documents.Count is < 2 or > 10 ||
            !RequiredDocumentTypes.IsSubsetOf(request.Documents.Select(document => document.DocumentType)))
            throw new HttpException(400, MessageConstant.SupplierDocumentsInvalid);
        foreach (var document in request.Documents)
        {
            if (string.IsNullOrWhiteSpace(document.DocumentType) || document.DocumentType.Length > 50 ||
                !Uri.TryCreate(document.FileUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0 || document.FileUrl.Length > 1000)
                throw new HttpException(400, MessageConstant.SupplierDocumentsInvalid);
        }

        var profile = await repository.GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile?.VerificationStatus == SupplierVerificationStatus.Verified)
            throw new HttpException(409, MessageConstant.SupplierAlreadyVerified);
        if (profile?.VerificationStatus == SupplierVerificationStatus.Pending)
            throw new HttpException(409, MessageConstant.SupplierAlreadyPending);

        profile ??= new SupplierProfile { UserId = userId };
        profile.CompanyName = request.CompanyName.Trim();
        profile.TaxCode = request.TaxCode.Trim();
        profile.Address = request.Address.Trim();
        profile.ContactEmail = contactEmail;
        profile.Description = request.Description?.Trim();
        profile.VerificationStatus = SupplierVerificationStatus.Pending;
        profile.VerifiedAt = null;
        profile.ReviewedAt = null;
        profile.ReviewedByUserId = null;
        profile.RejectionReason = null;
        profile.UpdatedAtUtc = dateTimeProvider.UtcNow;
        user.RequestedRole = UserRole.Supplier;
        user.UpdatedAtUtc = dateTimeProvider.UtcNow;

        var documents = request.Documents.Select(document => new SupplierVerificationDocument
        {
            SupplierProfileId = profile.Id,
            DocumentType = document.DocumentType.Trim(),
            FileUrl = document.FileUrl.Trim(),
            OriginalFileName = document.OriginalFileName?.Trim(),
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            SubmittedAt = dateTimeProvider.UtcNow,
            ReviewStatus = "Pending"
        }).ToList();

        await repository.SaveApplicationAsync(user, profile, documents, cancellationToken);
        return await GetProfileAsync(profile, cancellationToken);
    }

    public async Task<SupplierProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await repository.GetProfileByUserIdAsync(userId, cancellationToken)
                      ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        return await GetProfileAsync(profile, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierProfileResponse>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await repository.GetPendingProfilesAsync(cancellationToken);
        var responses = new List<SupplierProfileResponse>(profiles.Count);
        foreach (var profile in profiles)
            responses.Add(await GetProfileAsync(profile, cancellationToken));
        return responses;
    }

    public async Task<IReadOnlyList<SupplierProfileResponse>> GetVerifiedAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await repository.GetVerifiedProfilesAsync(cancellationToken);
        var responses = new List<SupplierProfileResponse>(profiles.Count);
        foreach (var profile in profiles)
            responses.Add(await GetProfileAsync(profile, cancellationToken));
        return responses;
    }

    public async Task<SupplierProfileResponse> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await repository.GetProfileByIdAsync(profileId, cancellationToken)
            ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        return await GetProfileAsync(profile, cancellationToken);
    }

    public async Task<SupplierProfileResponse> ReviewAsync(Guid profileId, Guid adminUserId, ReviewSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new HttpException(400, MessageConstant.SupplierApplyInvalid);
        var profile = await repository.GetProfileByIdAsync(profileId, cancellationToken)
                      ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        if (profile.VerificationStatus != SupplierVerificationStatus.Pending)
            throw new HttpException(409, MessageConstant.SupplierReviewConflict);
        if (!request.Approve && !ValidReason(request.RejectionReason))
            throw new HttpException(400, MessageConstant.SupplierReasonRequired);

        var now = dateTimeProvider.UtcNow;
        profile.VerificationStatus = request.Approve ? SupplierVerificationStatus.Verified : SupplierVerificationStatus.Rejected;
        profile.VerifiedAt = request.Approve ? now : null;
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.RejectionReason = request.Approve ? null : request.RejectionReason!.Trim();
        profile.UpdatedAtUtc = now;

        var user = await repository.GetUserAsync(profile.UserId, cancellationToken)
                   ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        user.Role = request.Approve ? UserRole.Supplier : UserRole.Customer;
        user.UpdatedAtUtc = now;

        var documents = await repository.GetDocumentsAsync(profile.Id, cancellationToken);
        foreach (var document in documents)
        {
            document.ReviewStatus = request.Approve ? "Approved" : "Rejected";
            document.ReviewedAt = now;
            document.ReviewedByUserId = adminUserId;
            document.RejectionReason = request.Approve ? null : profile.RejectionReason;
            document.UpdatedAtUtc = now;
        }

        await PersistDecisionAsync(user, profile, SupplierVerificationStatus.Pending, documents,
            adminUserId, profile.RejectionReason, cancellationToken);
        return await GetProfileAsync(profile, cancellationToken);
    }

    public async Task<SupplierProfileResponse> RevokeAsync(Guid profileId, Guid adminUserId, string reason,
        CancellationToken cancellationToken = default)
    {
        if (!ValidReason(reason))
            throw new HttpException(400, MessageConstant.SupplierReasonRequired);
        var profile = await repository.GetProfileByIdAsync(profileId, cancellationToken)
            ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        if (profile.VerificationStatus != SupplierVerificationStatus.Verified)
            throw new HttpException(409, MessageConstant.SupplierReviewConflict);
        var user = await repository.GetUserAsync(profile.UserId, cancellationToken)
            ?? throw new HttpException(404, MessageConstant.SupplierNotFound);
        var now = dateTimeProvider.UtcNow;
        profile.VerificationStatus = SupplierVerificationStatus.Revoked;
        profile.VerifiedAt = null;
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.RejectionReason = reason.Trim();
        profile.UpdatedAtUtc = now;
        user.Role = UserRole.Customer;
        user.UpdatedAtUtc = now;
        var documents = await repository.GetDocumentsAsync(profile.Id, cancellationToken);
        foreach (var document in documents)
        {
            document.ReviewStatus = "Revoked";
            document.ReviewedAt = now;
            document.ReviewedByUserId = adminUserId;
            document.RejectionReason = profile.RejectionReason;
            document.UpdatedAtUtc = now;
        }
        await PersistDecisionAsync(user, profile, SupplierVerificationStatus.Verified, documents,
            adminUserId, profile.RejectionReason, cancellationToken);
        return await GetProfileAsync(profile, cancellationToken);
    }

    private async Task PersistDecisionAsync(User user, SupplierProfile profile,
        SupplierVerificationStatus expectedStatus, IReadOnlyList<SupplierVerificationDocument> documents,
        Guid adminUserId, string? reason, CancellationToken cancellationToken)
    {
        var status = profile.VerificationStatus;
        var title = status switch
        {
            SupplierVerificationStatus.Verified => "Hồ sơ nhà cung cấp được phê duyệt",
            SupplierVerificationStatus.Rejected => "Hồ sơ nhà cung cấp bị từ chối",
            _ => "Xác minh nhà cung cấp đã bị thu hồi"
        };
        var content = status == SupplierVerificationStatus.Verified
            ? "Hồ sơ của bạn đã được duyệt. Bạn có thể sử dụng kênh nhà cung cấp sau khi làm mới phiên đăng nhập."
            : $"{title}. Lý do: {reason}";
        var now = dateTimeProvider.UtcNow;
        await repository.ChangeStatusAsync(user, profile, expectedStatus, documents,
            new WorkflowEvent
            {
                Id = Guid.NewGuid(), AggregateType = AggregateType, AggregateId = profile.Id,
                EventType = status == SupplierVerificationStatus.Revoked ? "SupplierVerificationRevoked" : "SupplierVerificationReviewed",
                FromStatus = expectedStatus.ToString(), ToStatus = status.ToString(),
                ActorUserId = adminUserId, Reason = reason, CreatedAtUtc = now, UpdatedAtUtc = now
            },
            new Notification
            {
                Id = Guid.NewGuid(), UserId = user.Id, Type = "SupplierVerification",
                Title = title, Content = content, ReferenceId = profile.Id,
                ReferenceType = AggregateType, CreatedAtUtc = now, UpdatedAtUtc = now
            },
            new AuditLog
            {
                Id = Guid.NewGuid(), ActorUserId = adminUserId, Action = status.ToString(),
                EntityName = AggregateType, EntityId = profile.Id.ToString(),
                MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { from = expectedStatus.ToString(), to = status.ToString(), reason }),
                CreatedAtUtc = now, UpdatedAtUtc = now
            }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(profile.ContactEmail))
        {
            try
            {
                await emailSender.SendAsync(profile.ContactEmail, title, content, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not send supplier verification email for profile {ProfileId}", profile.Id);
            }
        }
    }

    private static bool ValidReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length is >= 10 and <= 1000;

    private async Task<SupplierProfileResponse> GetProfileAsync(SupplierProfile profile, CancellationToken cancellationToken)
    {
        var documents = await repository.GetDocumentsAsync(profile.Id, cancellationToken);
        return new SupplierProfileResponse(profile.Id, profile.UserId, profile.CompanyName, profile.TaxCode, profile.Address,
            profile.ContactEmail,
            profile.Description, profile.VerificationStatus, profile.CreatedAt, profile.VerifiedAt,
            profile.ReviewedAt, profile.RejectionReason,
            documents.Select(document => new SupplierDocumentResponse(document.Id, document.DocumentType, document.FileUrl,
                document.ReviewStatus, document.RejectionReason)).ToList());
    }
}
