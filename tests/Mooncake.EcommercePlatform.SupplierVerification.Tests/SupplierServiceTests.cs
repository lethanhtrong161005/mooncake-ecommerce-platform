namespace Mooncake.EcommercePlatform.SupplierVerification.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public class SupplierServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CustomerCanApplyWithoutReceivingSupplierRole()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.ApplyAsync(repository.User.Id, ValidApplication());

        Assert.Equal(SupplierVerificationStatus.Pending, result.VerificationStatus);
        Assert.Equal(UserRole.Customer, repository.User.Role);
        Assert.Equal(UserRole.Supplier, repository.User.RequestedRole);
        Assert.Equal("contact@example.com", result.ContactEmail);
        Assert.Equal(2, result.Documents.Count);
    }

    [Fact]
    public async Task ApplicationRequiresBothLegalDocuments()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var request = ValidApplication() with
        {
            Documents = [new VerificationDocumentRequest
            {
                DocumentType = "BusinessLicense", FileUrl = "https://example.com/license.pdf"
            }]
        };

        var error = await Assert.ThrowsAsync<HttpException>(() => service.ApplyAsync(repository.User.Id, request));

        Assert.Equal(400, error.StatusCode);
        Assert.Null(repository.Profile);
    }

    [Fact]
    public async Task PendingApplicationCannotBeSilentlyReplaced()
    {
        var repository = new FakeRepository { Profile = PendingProfile() };
        var service = CreateService(repository);

        var error = await Assert.ThrowsAsync<HttpException>(() => service.ApplyAsync(repository.User.Id, ValidApplication()));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal("Công ty thử nghiệm", repository.Profile!.CompanyName);
    }

    [Fact]
    public async Task RejectedApplicationCanBeCorrectedAndResubmitted()
    {
        var repository = new FakeRepository { Profile = PendingProfile().WithStatus(SupplierVerificationStatus.Rejected) };
        repository.Profile.RejectionReason = "Thiếu tài liệu hợp lệ.";
        var service = CreateService(repository);

        var result = await service.ApplyAsync(repository.User.Id,
            ValidApplication() with { CompanyName = "Công ty đã bổ sung" });

        Assert.Equal(SupplierVerificationStatus.Pending, result.VerificationStatus);
        Assert.Equal("Công ty đã bổ sung", result.CompanyName);
        Assert.Null(result.RejectionReason);
        Assert.Equal(UserRole.Customer, repository.User.Role);
        Assert.Equal(2, result.Documents.Count);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("0123456789-12")]
    [InlineData("012345678A")]
    public async Task InvalidTaxCodeIsRejected(string taxCode)
    {
        var repository = new FakeRepository();
        var error = await Assert.ThrowsAsync<HttpException>(() =>
            CreateService(repository).ApplyAsync(repository.User.Id, ValidApplication() with { TaxCode = taxCode }));

        Assert.Equal(400, error.StatusCode);
        Assert.Null(repository.Profile);
    }

    [Fact]
    public async Task DocumentLinkMustUseHttps()
    {
        var repository = new FakeRepository();
        var request = ValidApplication();
        request.Documents[0] = request.Documents[0] with { FileUrl = "http://example.com/license.pdf" };

        var error = await Assert.ThrowsAsync<HttpException>(() =>
            CreateService(repository).ApplyAsync(repository.User.Id, request));

        Assert.Equal(400, error.StatusCode);
        Assert.Null(repository.Profile);
    }

    [Fact]
    public async Task ApprovalChangesRoleAndRecordsDecision()
    {
        var repository = new FakeRepository { Profile = PendingProfile() };
        repository.Documents.AddRange(ValidDocuments(repository.Profile.Id));
        var email = new FakeEmailSender();
        var service = CreateService(repository, email);

        var result = await service.ReviewAsync(repository.Profile.Id, Guid.NewGuid(),
            new ReviewSupplierRequest { Approve = true });

        Assert.Equal(SupplierVerificationStatus.Verified, result.VerificationStatus);
        Assert.Equal(UserRole.Supplier, repository.User.Role);
        Assert.Equal(SupplierVerificationStatus.Pending, repository.ExpectedStatus);
        Assert.Equal("Verified", repository.Workflow!.ToStatus);
        Assert.Equal("Approved", Assert.Single(result.Documents.Where(x => x.DocumentType == "BusinessLicense")).ReviewStatus);
        Assert.Equal(repository.User.Id, repository.Notification!.UserId);
        Assert.Equal("Verified", repository.Audit!.Action);
        Assert.Single(email.Messages);
    }

    [Fact]
    public async Task RejectionRequiresReasonAndKeepsCustomerRole()
    {
        var repository = new FakeRepository { Profile = PendingProfile() };
        var service = CreateService(repository);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.ReviewAsync(
            repository.Profile.Id, Guid.NewGuid(),
            new ReviewSupplierRequest { Approve = false, RejectionReason = "Thiếu" }));

        Assert.Equal(400, error.StatusCode);
        Assert.Null(repository.Workflow);

        var result = await service.ReviewAsync(repository.Profile.Id, Guid.NewGuid(),
            new ReviewSupplierRequest { Approve = false, RejectionReason = "Thiếu giấy chứng nhận ATTP." });

        Assert.Equal(SupplierVerificationStatus.Rejected, result.VerificationStatus);
        Assert.Equal(UserRole.Customer, repository.User.Role);
        Assert.Equal("Thiếu giấy chứng nhận ATTP.", result.RejectionReason);
        Assert.Equal("Rejected", repository.Workflow!.ToStatus);
    }

    [Fact]
    public async Task AlreadyReviewedApplicationCannotBeApprovedAgain()
    {
        var repository = new FakeRepository { Profile = PendingProfile().WithStatus(SupplierVerificationStatus.Verified) };

        var error = await Assert.ThrowsAsync<HttpException>(() => CreateService(repository).ReviewAsync(
            repository.Profile.Id, Guid.NewGuid(), new ReviewSupplierRequest { Approve = true }));

        Assert.Equal(409, error.StatusCode);
        Assert.Null(repository.Workflow);
    }

    [Fact]
    public async Task RevocationRemovesSupplierRoleEvenWhenEmailFails()
    {
        var repository = new FakeRepository
        {
            Profile = PendingProfile().WithStatus(SupplierVerificationStatus.Verified)
        };
        repository.User.Role = UserRole.Supplier;
        repository.Documents.AddRange(ValidDocuments(repository.Profile.Id));
        var email = new FakeEmailSender { Fail = true };
        var service = CreateService(repository, email);

        var result = await service.RevokeAsync(repository.Profile.Id, Guid.NewGuid(), "Giấy phép kinh doanh không còn hiệu lực.");

        Assert.Equal(SupplierVerificationStatus.Revoked, result.VerificationStatus);
        Assert.Equal(UserRole.Customer, repository.User.Role);
        Assert.Equal(SupplierVerificationStatus.Verified, repository.ExpectedStatus);
        Assert.Equal("SupplierVerificationRevoked", repository.Workflow!.EventType);
        Assert.NotNull(repository.Notification);
        Assert.All(result.Documents, document => Assert.Equal("Revoked", document.ReviewStatus));
    }

    private static SupplierService CreateService(FakeRepository repository, FakeEmailSender? email = null) =>
        new(repository, new FakeClock(), email ?? new FakeEmailSender(), NullLogger<SupplierService>.Instance);

    private static ApplySupplierRequest ValidApplication() => new()
    {
        CompanyName = "Công ty thử nghiệm",
        TaxCode = "0123456789",
        Address = "Hà Nội",
        ContactEmail = "Contact@Example.com",
        Documents =
        [
            new VerificationDocumentRequest
            {
                DocumentType = "BusinessLicense", FileUrl = "https://example.com/license.pdf"
            },
            new VerificationDocumentRequest
            {
                DocumentType = "FoodSafetyCertificate", FileUrl = "https://example.com/food.pdf"
            }
        ]
    };

    private static SupplierProfile PendingProfile() => new()
    {
        Id = Guid.NewGuid(), UserId = FakeRepository.TestUserId,
        CompanyName = "Công ty thử nghiệm", TaxCode = "0123456789",
        Address = "Hà Nội", ContactEmail = "contact@example.com",
        VerificationStatus = SupplierVerificationStatus.Pending,
        CreatedAtUtc = Now, UpdatedAtUtc = Now
    };

    private static List<SupplierVerificationDocument> ValidDocuments(Guid profileId) =>
    [
        new() { Id = Guid.NewGuid(), SupplierProfileId = profileId, DocumentType = "BusinessLicense", FileUrl = "https://example.com/license.pdf" },
        new() { Id = Guid.NewGuid(), SupplierProfileId = profileId, DocumentType = "FoodSafetyCertificate", FileUrl = "https://example.com/food.pdf" }
    ];

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool Fail { get; init; }
        public List<string> Messages { get; } = [];

        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("SMTP unavailable");
            Messages.Add(recipient);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : ISupplierRepository
    {
        public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public User User { get; } = new() { Id = TestUserId, Email = "customer@example.com", Role = UserRole.Customer, IsActive = true };
        public SupplierProfile? Profile { get; set; }
        public List<SupplierVerificationDocument> Documents { get; } = [];
        public SupplierVerificationStatus? ExpectedStatus { get; private set; }
        public WorkflowEvent? Workflow { get; private set; }
        public Notification? Notification { get; private set; }
        public AuditLog? Audit { get; private set; }

        public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(userId == User.Id ? User : null);
        public Task<SupplierProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile?.UserId == userId ? Profile : null);
        public Task<SupplierProfile?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile?.Id == profileId ? Profile : null);
        public Task<IReadOnlyList<SupplierProfile>> GetPendingProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierProfile>>(Profile?.VerificationStatus == SupplierVerificationStatus.Pending ? [Profile] : []);
        public Task<IReadOnlyList<SupplierProfile>> GetVerifiedProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierProfile>>(Profile?.VerificationStatus == SupplierVerificationStatus.Verified ? [Profile] : []);
        public Task<IReadOnlyList<SupplierVerificationDocument>> GetDocumentsAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierVerificationDocument>>(Documents.Where(x => x.SupplierProfileId == profileId && !x.IsDeleted).ToList());
        public Task SaveApplicationAsync(User user, SupplierProfile profile, IReadOnlyList<SupplierVerificationDocument> documents, CancellationToken cancellationToken = default)
        {
            if (profile.Id == Guid.Empty) profile.Id = Guid.NewGuid();
            Profile = profile;
            Documents.Clear();
            foreach (var document in documents)
            {
                document.Id = Guid.NewGuid();
                document.SupplierProfileId = profile.Id;
                Documents.Add(document);
            }
            return Task.CompletedTask;
        }
        public Task ChangeStatusAsync(User user, SupplierProfile profile, SupplierVerificationStatus expectedStatus,
            IReadOnlyList<SupplierVerificationDocument> documents, WorkflowEvent workflowEvent,
            Notification notification, AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            ExpectedStatus = expectedStatus;
            Workflow = workflowEvent;
            Notification = notification;
            Audit = auditLog;
            return Task.CompletedTask;
        }
    }
}

internal static class SupplierProfileTestExtensions
{
    public static SupplierProfile WithStatus(this SupplierProfile profile, SupplierVerificationStatus status)
    {
        profile.VerificationStatus = status;
        return profile;
    }
}
