namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Normalization, policy predicates and response mapping without persistence dependencies.</summary>
public static class SupplierVerificationHelper
{
    public static bool CanSell(User? owner, SupplierProfile? profile) =>
        owner is { IsActive: true, IsDeleted: false, Role: UserRole.Supplier } &&
        profile is { IsDeleted: false, VerificationStatus: SupplierVerificationStatus.Verified };

    public static bool CanFulfill(User? owner, SupplierProfile? profile, DateTime transactionCreatedAt) =>
        owner is { IsActive: true, IsDeleted: false } && profile is { IsDeleted: false } &&
        (CanSell(owner, profile) || (profile.RevokedAtUtc.HasValue &&
            transactionCreatedAt < profile.RevokedAtUtc.Value));
    public static Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses.AuthenUserResponse AuthUser(User user, SupplierProfile? profile)
    {
        var eligible = user.IsActive && !user.IsDeleted && user.Role != UserRole.Admin &&
            (user.RequestedRole == UserRole.Supplier || user.Role == UserRole.Supplier);
        var verified = eligible && profile?.VerificationStatus == SupplierVerificationStatus.Verified &&
            user.Role == UserRole.Supplier;
        var canSubmit = eligible && user.EmailVerifiedAt.HasValue && (profile?.CurrentSubmissionId == null ||
            profile.VerificationStatus is SupplierVerificationStatus.Rejected or SupplierVerificationStatus.Revoked);
        return new(user.Id, user.Phone, user.FullName, user.Role, user.RequestedRole,
            new(eligible, canSubmit, verified, verified, eligible && profile?.RevokedAtUtc != null),
            eligible ? new(profile?.Id, profile?.CurrentSubmissionId == null ? C.NotSubmitted : profile.VerificationStatus.ToString(),
                profile?.CurrentSubmissionId == null ? null : profile.VerificationStatus, profile?.Version ?? 0) : null);
    }
    public static void Require(bool condition, int status = 400, string code = C.Invalid)
    {
        if (!condition) throw new HttpException(status, C.Error, code);
    }
    public static User RequireActive(User? user)
    {
        Require(user is { IsActive: true, IsDeleted: false }, 403, C.Forbidden);
        return user!;
    }
    public static User RequireOnboarding(User? user)
    {
        var active = RequireActive(user);
        Require(active.Role != UserRole.Admin && active.EmailVerifiedAt.HasValue &&
            (active.RequestedRole == UserRole.Supplier || active.Role == UserRole.Supplier), 403, C.Forbidden);
        return active;
    }
    public static void RequireAdmin(User? user, Guid? targetUser = null)
    {
        var active = RequireActive(user);
        Require(active.Role == UserRole.Admin && active.Id != targetUser, 403, C.Forbidden);
    }
    public static Guid ParseKey(string? value)
    {
        Require(Guid.TryParse(value, out var key) && key != Guid.Empty);
        return key;
    }
    public static string Hash(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    public static string NormalizeText(string? value, int max)
    {
        var result = value?.Trim().Normalize(NormalizationForm.FormKC) ?? string.Empty;
        Require(result.Length > 0 && result.Length <= max && !result.Any(char.IsControl));
        return result;
    }
    public static ApplySupplierRequest Normalize(ApplySupplierRequest request)
    {
        var taxCode = Regex.Replace(request.TaxCode ?? "", @"[\s-]", "");
        Require(Regex.IsMatch(taxCode, "^(?:[0-9]{10}|[0-9]{13})$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)));
        Require(request.ExpectedVersion is >= 0 && request.DocumentIds is { Count: > 0 and <= C.MaxDocuments } &&
            request.DocumentIds.All(x => x != Guid.Empty) && request.DocumentIds.Distinct().Count() == request.DocumentIds.Count);
        Require(request.Description?.Length is null or <= 2000);
        return request with { CompanyName = NormalizeText(request.CompanyName, 255), TaxCode = taxCode,
            Address = NormalizeText(request.Address, 500), Description = request.Description?.Trim(),
            DocumentIds = request.DocumentIds.Order().ToList() };
    }
    public static string Reason(string? value)
    {
        var result = value?.Trim().Normalize(NormalizationForm.FormKC) ?? string.Empty;
        Require(result.Length is >= 10 and <= 1000 && !result.Any(c =>
            char.IsControl(c) && c is not ('\n' or '\r' or '\t')));
        return result.Replace("\r\n", "\n").Replace("\r", "\n");
    }
    public static void CheckVersion(SupplierProfile profile, long? version) =>
        Require(version.HasValue && profile.Version == version.Value, 409, C.Conflict);
    public static void CheckDocuments(IReadOnlyList<SupplierVerificationDocument> documents,
        int count, Guid owner, DateTime now, bool staged)
    {
        Require(documents.Count == count && documents.Count is > 0 and <= C.MaxDocuments, 409, C.DocumentNotReady);
        Require(documents.All(d => d.OwnerUserId == owner && !d.IsDeleted &&
            d.UploadStatus == C.Ready && (!staged || d.SubmissionId == null) &&
            (!d.ExpiresOn.HasValue || d.ExpiresOn.Value >= DateOnly.FromDateTime(now))), 409, C.DocumentNotReady);
        Require(documents.Sum(d => d.FileSizeBytes ?? C.MaxSubmissionBytes) <= C.MaxSubmissionBytes, 413, C.DocumentTooLarge);
        Require(documents.Any(d => d.DocumentType == C.BusinessLicense) &&
            documents.Any(d => d.DocumentType == C.FoodSafety), 400, C.Invalid);
    }
    public static SupplierDocumentResponse Document(SupplierVerificationDocument d) =>
        new(d.Id, d.DocumentType, d.OriginalFileName, d.ContentType, d.FileSizeBytes,
            d.UploadStatus, d.ExpiresOn, d.ReviewStatus, d.RejectionReason);
    public static SupplierProfileResponse Profile(SupplierProfile? p, SupplierVerificationSubmission? s,
        IReadOnlyList<SupplierVerificationDocument> documents, IReadOnlyList<SupplierVerificationDocument> staged)
    {
        var canSubmit = p?.CurrentSubmissionId == null ||
            p.VerificationStatus is SupplierVerificationStatus.Rejected or SupplierVerificationStatus.Revoked;
        SupplierSubmissionResponse? snapshot = s is null ? null : new(s.Id, s.Revision, s.CompanyName,
            s.TaxCode, s.Address, s.Description, s.Status, s.SubmittedAtUtc, s.ReviewedAtUtc,
            s.RejectionReason, documents.Select(Document).ToList(), s.Source);
        return new(p?.Id, p?.Version ?? 0, p?.CurrentSubmissionId == null ? C.NotSubmitted : p.VerificationStatus.ToString(),
            p?.CurrentSubmissionId == null ? null : p.VerificationStatus, p?.CompanyName, p?.TaxCode,
            p?.Address, p?.Description, snapshot, staged.Select(Document).ToList(),
            canSubmit ? ["UploadDocument", "Submit"] : [], p?.VerifiedAt, p?.RevokedAtUtc, p?.RevocationReason);
    }
    public static (string Name, string Mime) ValidateContent(byte[] content, string fileName)
    {
        Require(content.Length > 0 && content.Length <= C.MaxFileBytes, 413, C.DocumentTooLarge);
        var name = string.Concat(Path.GetFileName(fileName.Replace('\\', '/')).Where(character => !char.IsControl(character))).Trim();
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..") name = "document";
        if (name.Length > 255) name = name[..255];
        // Identify browser-preview metadata without accepting or rejecting a file based on its format.
        var png = content.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10});
        var jpeg = content.Length > 4 && content[0] == 255 && content[1] == 216 && content[^2] == 255 && content[^1] == 217;
        var pdf = content.AsSpan().StartsWith("%PDF-"u8);
        return (name, pdf ? "application/pdf" : png ? "image/png" : jpeg ? "image/jpeg" : "application/octet-stream");
    }
    public static async Task<byte[]> ReadBoundedAsync(Stream content, CancellationToken ct)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, ct)) != 0)
        {
            Require(output.Length + read <= C.MaxFileBytes, 413, C.DocumentTooLarge);
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return output.ToArray();
    }
}
