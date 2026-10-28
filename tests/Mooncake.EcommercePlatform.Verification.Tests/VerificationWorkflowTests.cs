namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class VerificationWorkflowTests
{
    private readonly Mock<ISupplierRepository> _repository = new();
    private readonly User _admin = new() { Id = Guid.NewGuid(), Role = UserRole.Admin, IsActive = true };
    private readonly User _owner = new() { Id = Guid.NewGuid(), RequestedRole = UserRole.Supplier,
        Role = UserRole.Customer, IsActive = true, EmailVerifiedAt = DateTime.UtcNow };
    private readonly SupplierProfile _profile;
    private readonly SupplierVerificationSubmission _submission;
    private readonly SupplierService _service;
    private readonly List<VerificationOutboxMessage> _outbox = [];
    private readonly List<WorkflowEvent> _events = [];

    public VerificationWorkflowTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);
        _profile = new() { Id = Guid.NewGuid(), UserId = _owner.Id, Version = 7, VerificationStatus = SupplierVerificationStatus.Pending };
        _submission = new() { Id = Guid.NewGuid(), SupplierProfileId = _profile.Id, Revision = 2, Status = SupplierVerificationStatus.Pending };
        _profile.CurrentSubmissionId = _submission.Id;
        _repository.Setup(x => x.GetUserAsync(_admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_admin);
        _repository.Setup(x => x.GetUserAsync(_owner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_owner);
        _repository.Setup(x => x.GetProfileByIdAsync(_profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_profile);
        _repository.Setup(x => x.GetProfileByUserIdAsync(_owner.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(_profile);
        _repository.Setup(x => x.GetSubmissionAsync(_submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_submission);
        var docs = new List<SupplierVerificationDocument> {
            new() { Id = Guid.NewGuid(), OwnerUserId = _owner.Id, UploadStatus = "Ready", FileSizeBytes = 100,
                DocumentType = "BusinessLicense", SubmissionId = _submission.Id },
            new() { Id = Guid.NewGuid(), OwnerUserId = _owner.Id, UploadStatus = "Ready", FileSizeBytes = 100,
                DocumentType = "FoodSafetyCertificate", SubmissionId = _submission.Id },
        };
        _repository.Setup(x => x.GetDocumentsAsync(_profile.Id, _submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        _repository.Setup(x => x.GetSelectedDocumentsAsync(_profile.Id, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        _repository.Setup(x => x.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<Func<CancellationToken, Task<VerificationCommandResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<Guid> _, Func<CancellationToken, Task<VerificationCommandResponse>> work, CancellationToken ct) => work(ct));
        _repository.Setup(x => x.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<Func<CancellationToken, Task<Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses.AuthenUserResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<Guid> _, Func<CancellationToken, Task<Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses.AuthenUserResponse>> work, CancellationToken ct) => work(ct));
        _repository.Setup(x => x.AddOutbox(It.IsAny<VerificationOutboxMessage>())).Callback<VerificationOutboxMessage>(_outbox.Add);
        _repository.Setup(x => x.AddEvent(It.IsAny<WorkflowEvent>())).Callback<WorkflowEvent>(_events.Add);
        _service = new(_repository.Object, clock.Object, Mock.Of<ICloudinaryStorageService>(), Mock.Of<IVerificationDocumentGateway>(), Mock.Of<ITraceContext>());
    }

    [Fact]
    public async Task CustomerCanOptIntoOnboardingWithoutReceivingSupplierAuthority()
    {
        _owner.RequestedRole = UserRole.Customer;
        var result = await _service.StartOnboardingAsync(_owner.Id, default);
        await _service.StartOnboardingAsync(_owner.Id, default);
        Assert.Equal(UserRole.Customer, _owner.Role);
        Assert.Equal(UserRole.Supplier, _owner.RequestedRole);
        Assert.True(result.Capabilities!.CanReadSupplierVerification);
        Assert.False(result.Capabilities.CanCreateShop);
        _repository.Verify(x => x.AddAudit(It.Is<AuditLog>(a => a.Action == "SupplierOnboardingStarted")), Times.Once);
        _repository.Verify(x => x.AddSubmission(It.IsAny<SupplierVerificationSubmission>()), Times.Never);
        Assert.Empty(_outbox);
    }

    [Fact]
    public async Task AdministratorCannotOptIntoSupplierOnboarding()
    {
        var error = await Assert.ThrowsAsync<HttpException>(() => _service.StartOnboardingAsync(_admin.Id, default));
        Assert.Equal(403, error.StatusCode);
        _repository.Verify(x => x.AddAudit(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UnverifiedCustomerCannotStartSupplierOnboarding()
    {
        _owner.RequestedRole = UserRole.Customer;
        _owner.EmailVerifiedAt = null;
        var error = await Assert.ThrowsAsync<HttpException>(() => _service.StartOnboardingAsync(_owner.Id, default));
        Assert.Equal(403, error.StatusCode);
        Assert.Equal(UserRole.Customer, _owner.RequestedRole);
    }

    [Fact]
    public async Task ApprovalChangesRoleAndQueuesIndependentChannels()
    {
        var response = await _service.ReviewAsync(_profile.Id, _admin.Id, Guid.NewGuid().ToString(),
            new() { SubmissionId = _submission.Id, ExpectedVersion = 7, Approve = true }, default);
        Assert.Equal(SupplierVerificationStatus.Verified, response.VerificationStatus);
        Assert.Equal(8, response.Version);
        Assert.Equal(UserRole.Supplier, _owner.Role);
        Assert.Equal(SupplierVerificationStatus.Verified, _submission.Status);
        Assert.Equal(2, _outbox.Count);
        Assert.Single(_events);
        _repository.Verify(x => x.AddAudit(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task RejectPreservesSubmittedSnapshotAndProvidesReason()
    {
        var before = _submission.CompanyName;
        await _service.ReviewAsync(_profile.Id, _admin.Id, Guid.NewGuid().ToString(),
            new() { SubmissionId = _submission.Id, ExpectedVersion = 7, Approve = false,
                RejectionReason = "Please provide a readable certificate." }, default);
        Assert.Equal(SupplierVerificationStatus.Rejected, _submission.Status);
        Assert.Equal(before, _submission.CompanyName);
        Assert.Equal(UserRole.Customer, _owner.Role);
        Assert.NotNull(_submission.RejectionReason);
    }

    [Fact]
    public async Task SecondDecisionAgainstSameVersionIsRejected()
    {
        var request = new ReviewSupplierRequest { SubmissionId = _submission.Id, ExpectedVersion = 7, Approve = true };
        await _service.ReviewAsync(_profile.Id, _admin.Id, Guid.NewGuid().ToString(), request, default);
        var error = await Assert.ThrowsAsync<HttpException>(() => _service.ReviewAsync(
            _profile.Id, _admin.Id, Guid.NewGuid().ToString(), request with { Approve = false, RejectionReason = "A different concurrent decision." }, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Single(_events);
    }

    [Fact]
    public async Task RevocationKeepsOriginalApprovalSnapshot()
    {
        _profile.VerificationStatus = SupplierVerificationStatus.Verified;
        _submission.Status = SupplierVerificationStatus.Verified;
        _owner.Role = UserRole.Supplier;
        await _service.RevokeAsync(_profile.Id, _admin.Id, Guid.NewGuid().ToString(),
            new(7, "Verification evidence requires reassessment."), default);
        Assert.Equal(SupplierVerificationStatus.Revoked, _profile.VerificationStatus);
        Assert.Equal(SupplierVerificationStatus.Verified, _submission.Status);
        Assert.Equal(UserRole.Customer, _owner.Role);
        Assert.NotNull(_profile.RevokedAtUtc);
        Assert.Equal(2, _outbox.Count);
    }

    [Fact]
    public async Task AForeignSubmissionCannotBeReviewed()
    {
        var error = await Assert.ThrowsAsync<HttpException>(() => _service.ReviewAsync(_profile.Id, _admin.Id,
            Guid.NewGuid().ToString(), new() { ExpectedVersion = 7, SubmissionId = Guid.NewGuid(), Approve = true }, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Empty(_events);
    }
}
