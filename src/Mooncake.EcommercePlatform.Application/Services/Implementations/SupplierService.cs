namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements supplier applications and admin review.</summary>
public sealed class SupplierService(ISupplierRepository repository, IDateTimeProvider dateTimeProvider) : ISupplierService
{
    private const string AggregateType = "SupplierProfile";

    public async Task<SupplierProfileResponse> ApplyAsync(Guid userId, ApplySupplierRequest request, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetUserAsync(userId, cancellationToken)
                   ?? throw new HttpException(404, "User account was not found.");
        if (!user.IsActive || user.IsDeleted)
            throw new HttpException(403, "The account is inactive.");
        if (user.RequestedRole != UserRole.Supplier && user.Role != UserRole.Supplier)
            throw new HttpException(403, "This account was not registered for supplier onboarding.");

        var profile = await repository.GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile?.VerificationStatus == SupplierVerificationStatus.Verified)
            throw new HttpException(409, "A verified supplier profile cannot be replaced.");

        profile ??= new SupplierProfile { UserId = userId };
        profile.CompanyName = request.CompanyName.Trim();
        profile.TaxCode = request.TaxCode.Trim();
        profile.Address = request.Address.Trim();
        profile.Description = request.Description?.Trim();
        profile.VerificationStatus = SupplierVerificationStatus.Pending;
        profile.VerifiedAt = null;
        profile.ReviewedAt = null;
        profile.ReviewedByUserId = null;
        profile.RejectionReason = null;

        var documents = request.Documents.Select(document => new SupplierVerificationDocument
        {
            SupplierProfileId = profile.Id,
            DocumentType = document.DocumentType.Trim(),
            FileUrl = document.FileUrl.Trim(),
            OriginalFileName = document.OriginalFileName,
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
                      ?? throw new HttpException(404, "Supplier profile was not found.");
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

    public async Task<SupplierProfileResponse> ReviewAsync(Guid profileId, Guid adminUserId, ReviewSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await repository.GetProfileByIdAsync(profileId, cancellationToken)
                      ?? throw new HttpException(404, "Supplier profile was not found.");
        if (profile.VerificationStatus != SupplierVerificationStatus.Pending)
            throw new HttpException(409, "Only pending supplier applications can be reviewed.");
        if (!request.Approve && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new HttpException(400, "A rejection reason is required when declining an application.");

        var previousStatus = profile.VerificationStatus.ToString();
        var now = dateTimeProvider.UtcNow;
        profile.VerificationStatus = request.Approve ? SupplierVerificationStatus.Verified : SupplierVerificationStatus.Rejected;
        profile.VerifiedAt = request.Approve ? now : null;
        profile.ReviewedAt = now;
        profile.ReviewedByUserId = adminUserId;
        profile.RejectionReason = request.Approve ? null : request.RejectionReason!.Trim();

        var user = await repository.GetUserAsync(profile.UserId, cancellationToken)
                   ?? throw new HttpException(404, "Supplier account was not found.");
        user.Role = request.Approve ? UserRole.Supplier : UserRole.Customer;

        var documents = await repository.GetDocumentsAsync(profile.Id, cancellationToken);
        foreach (var document in documents)
        {
            document.ReviewStatus = request.Approve ? "Approved" : "Rejected";
            document.ReviewedAt = now;
            document.ReviewedByUserId = adminUserId;
            document.RejectionReason = request.Approve ? null : profile.RejectionReason;
        }

        var workflowEvent = new WorkflowEvent
        {
            AggregateType = AggregateType,
            AggregateId = profile.Id,
            EventType = "SupplierVerificationReviewed",
            FromStatus = previousStatus,
            ToStatus = profile.VerificationStatus.ToString(),
            ActorUserId = adminUserId,
            Reason = profile.RejectionReason
        };
        await repository.ReviewAsync(user, profile, documents, workflowEvent, cancellationToken);
        return await GetProfileAsync(profile, cancellationToken);
    }

    private async Task<SupplierProfileResponse> GetProfileAsync(SupplierProfile profile, CancellationToken cancellationToken)
    {
        var documents = await repository.GetDocumentsAsync(profile.Id, cancellationToken);
        return new SupplierProfileResponse(profile.Id, profile.UserId, profile.CompanyName, profile.TaxCode, profile.Address,
            profile.Description, profile.VerificationStatus, profile.ReviewedAt, profile.RejectionReason,
            documents.Select(document => new SupplierDocumentResponse(document.Id, document.DocumentType, document.FileUrl,
                document.ReviewStatus, document.RejectionReason)).ToList());
    }
}
