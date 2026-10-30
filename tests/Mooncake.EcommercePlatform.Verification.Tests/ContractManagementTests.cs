namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class ContractManagementTests
{
    private readonly Mock<IContractRepository> repository = new();
    private readonly Mock<IAuthService> authService = new();
    private readonly Mock<ISupplierRepository> suppliers = new();
    private readonly Mock<IDateTimeProvider> clock = new();

    private readonly Guid customerUserId = Guid.NewGuid();
    private readonly Guid supplierUserId = Guid.NewGuid();
    private readonly Guid supplierProfileId = Guid.NewGuid();
    private readonly Guid adminUserId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid milestoneId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    public ContractManagementTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        suppliers.Setup(s => s.GetProfileByIdAsync(supplierProfileId, default))
            .ReturnsAsync(new SupplierProfile
            {
                Id = supplierProfileId,
                UserId = supplierUserId,
                IsDeleted = false,
                VerificationStatus = SupplierVerificationStatus.Verified
            });
        suppliers.Setup(s => s.GetUserAsync(supplierUserId, default))
            .ReturnsAsync(new User
            {
                Id = supplierUserId,
                Role = UserRole.Supplier,
                IsActive = true,
                IsDeleted = false
            });
        suppliers.Setup(s => s.GetUserAsync(customerUserId, default))
            .ReturnsAsync(new User
            {
                Id = customerUserId,
                Role = UserRole.Customer,
                IsActive = true,
                IsDeleted = false
            });
    }

    private ContractService CreateService() => new(repository.Object, authService.Object, suppliers.Object, clock.Object);

    private Contract CreateActiveContract() => new()
    {
        Id = contractId,
        ContractNumber = "HD-2026-TEST01",
        CustomerId = customerUserId,
        SupplierId = supplierProfileId,
        TotalValue = 50000000m,
        Status = ContractStatus.Active,
        DeliveryDeadline = DateOnly.FromDateTime(fixedNow.AddDays(15)),
        SignedByCustomerAt = fixedNow.AddDays(-2),
        SignedBySupplierAt = fixedNow.AddDays(-2)
    };

    [Fact]
    public async Task SubmitMilestoneProgress_ThrowsWhenCustomerTriesToSubmit()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, customerUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new SubmitMilestoneProgressRequest
        {
            ProgressNotes = "Done first batch",
            EvidenceUrls = ["https://example.com/batch1.jpg"]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.SubmitMilestoneProgressAsync(customerUserId, contractId, milestoneId, request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitMilestoneProgress_ThrowsWhenContractNotActive()
    {
        var contract = CreateActiveContract();
        contract.Status = ContractStatus.PendingDeposit;
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new SubmitMilestoneProgressRequest
        {
            ProgressNotes = "Done batch",
            EvidenceUrls = ["https://example.com/batch.jpg"]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.SubmitMilestoneProgressAsync(supplierUserId, contractId, milestoneId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task VerifyMilestone_ThrowsWhenSupplierTriesToVerify()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new VerifyMilestoneRequest { IsApproved = true };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.VerifyMilestoneAsync(supplierUserId, contractId, milestoneId, request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task VerifyMilestone_MarksCompletedOnTime_WhenApproved()
    {
        var contract = CreateActiveContract();
        var milestone = new ContractMilestone
        {
            Id = milestoneId,
            ContractId = contractId,
            SequenceNumber = 2,
            Name = "Production & Delivery M2",
            Status = MilestoneStatus.PendingReview,
            DueDate = DateOnly.FromDateTime(fixedNow.AddDays(5))
        };

        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, customerUserId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetMilestoneAsync(contractId, milestoneId, default)).ReturnsAsync(milestone);

        var verified = new ContractMilestone
        {
            Id = milestoneId,
            ContractId = contractId,
            SequenceNumber = 2,
            Name = "Production & Delivery M2",
            Status = MilestoneStatus.CompletedOnTime,
            CompletionDate = DateOnly.FromDateTime(fixedNow)
        };
        repository.Setup(r => r.VerifyMilestoneAsync(contractId, milestoneId, true, null, customerUserId, default))
            .ReturnsAsync(verified);

        var service = CreateService();
        var result = await service.VerifyMilestoneAsync(customerUserId, contractId, milestoneId, new VerifyMilestoneRequest { IsApproved = true });

        Assert.Equal(MilestoneStatus.CompletedOnTime, result.Status);
    }

    [Fact]
    public async Task RequestExtension_ThrowsWhenProposedDateInPast()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new RequestContractExtensionRequest
        {
            ProposedDueDate = DateOnly.FromDateTime(fixedNow.AddDays(-1)),
            Reason = "Materials delay"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.RequestExtensionAsync(supplierUserId, contractId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task RequestExtension_ThrowsWhenExtensionAlreadyPending()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetPendingExtensionAsync(contractId, default))
            .ReturnsAsync(new ContractExtension { Id = Guid.NewGuid(), ContractId = contractId, Status = "Pending" });

        var service = CreateService();
        var request = new RequestContractExtensionRequest
        {
            ProposedDueDate = DateOnly.FromDateTime(fixedNow.AddDays(10)),
            Reason = "Custom packaging mold adjustment"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.RequestExtensionAsync(supplierUserId, contractId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task RespondExtension_ThrowsWhenSupplierTriesToRespond()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new RespondContractExtensionRequest { IsApproved = true };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.RespondExtensionAsync(supplierUserId, contractId, request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task RaiseDispute_ThrowsWhenDisputeAlreadyActive()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, customerUserId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetActiveDisputeAsync(contractId, default))
            .ReturnsAsync(new ContractDispute { Id = Guid.NewGuid(), ContractId = contractId, Status = "Pending" });

        var service = CreateService();
        var request = new RaiseContractDisputeRequest
        {
            DisputeReason = "Damaged packaging and moldy crust upon delivery.",
            EvidenceUrls = ["https://example.com/damage.jpg"]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.RaiseDisputeAsync(customerUserId, contractId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task ConfirmFinalAcceptance_ThrowsWhenInspectionNotConfirmed()
    {
        var service = CreateService();
        var request = new ConfirmFinalAcceptanceRequest { ConfirmedQualityAndQuantity = false };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ConfirmFinalAcceptanceAsync(customerUserId, contractId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ConfirmFinalAcceptance_ThrowsWhenSupplierTriesToConfirm()
    {
        var contract = CreateActiveContract();
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);
        repository.Setup(r => r.IsPartyAsync(contract, supplierUserId, default)).ReturnsAsync(true);

        var service = CreateService();
        var request = new ConfirmFinalAcceptanceRequest { ConfirmedQualityAndQuantity = true };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ConfirmFinalAcceptanceAsync(supplierUserId, contractId, request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveDispute_ThrowsWhenContractNotDisputed()
    {
        var contract = CreateActiveContract();
        contract.Status = ContractStatus.Active;
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);

        var service = CreateService();
        var request = new AdminResolveDisputeRequest
        {
            ResolutionType = "FullRefund",
            RefundAmount = 15000000m,
            AdminVerdict = "Supplier admitted severe production fault."
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.ResolveDisputeAsync(adminUserId, contractId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveDispute_SetsBreachedAndDeductsReputation()
    {
        var contract = CreateActiveContract();
        contract.Status = ContractStatus.Disputed;
        repository.Setup(r => r.GetContractAsync(contractId, default)).ReturnsAsync(contract);

        var resolvedDispute = new ContractDispute
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            RaisedByUserId = customerUserId,
            DisputeReason = "Failed food safety check",
            Status = "Resolved",
            ResolutionType = "FullRefund",
            RefundAmount = 15000000m,
            AdminVerdict = "Supplier breached food safety requirements.",
            RaisedAt = fixedNow.AddDays(-2),
            ResolvedAt = fixedNow,
            ResolvedByUserId = adminUserId
        };

        repository.Setup(r => r.ResolveDisputeAsync(contractId, "FullRefund", 15000000m,
                "Supplier breached food safety requirements.", adminUserId, default))
            .ReturnsAsync(resolvedDispute);

        var service = CreateService();
        var result = await service.ResolveDisputeAsync(adminUserId, contractId, new AdminResolveDisputeRequest
        {
            ResolutionType = "FullRefund",
            RefundAmount = 15000000m,
            AdminVerdict = "Supplier breached food safety requirements."
        });

        Assert.Equal("Resolved", result.Status);
        Assert.Equal("FullRefund", result.ResolutionType);
        Assert.Equal(15000000m, result.RefundAmount);
    }
}
