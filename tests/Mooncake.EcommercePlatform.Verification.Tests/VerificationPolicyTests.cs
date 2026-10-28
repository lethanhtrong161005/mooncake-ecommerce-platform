namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using H = Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper;

public sealed class VerificationPolicyTests
{
    [Theory]
    [InlineData("Customer", true)]
    [InlineData("Supplier", false)]
    [InlineData("Admin", false)]
    public void RegistrationOnlyAcceptsCustomerAccounts(string role, bool expected)
    {
        var request = new Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests.RegisterRequest {
            Email = "registration@example.test", Phone = "0901234567", Password = "TestPassword123!", Role = role };
        Assert.Equal(expected, Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    private static User Applicant() => new() { Id = Guid.NewGuid(), IsActive = true, EmailVerifiedAt = DateTime.UtcNow,
        RequestedRole = UserRole.Supplier, Role = UserRole.Customer };

    [Fact]
    public void RequestedSupplierCanReadOnboardingButCannotSell()
    {
        var user = Applicant();
        Assert.Same(user, H.RequireOnboarding(user));
        var response = H.AuthUser(user, null);
        Assert.True(response.Capabilities!.CanReadSupplierVerification);
        Assert.True(response.Capabilities.CanSubmitSupplierVerification);
        Assert.False(response.Capabilities.CanCreateShop);
        Assert.Equal("NotSubmitted", response.SupplierVerification!.OnboardingState);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void SellerEligibilityNeedsActiveAccountAndVerifiedProfile(bool active, bool verified, bool expected)
    {
        var user = Applicant(); user.IsActive = active; user.Role = UserRole.Supplier;
        var profile = new SupplierProfile { VerificationStatus = verified ? SupplierVerificationStatus.Verified : SupplierVerificationStatus.Pending };
        Assert.Equal(expected, H.CanSell(user, profile));
    }

    [Fact]
    public void PlainCustomerCannotEnterOnboarding()
    {
        var user = Applicant(); user.RequestedRole = UserRole.Customer;
        Assert.Equal(403, Assert.Throws<HttpException>(() => H.RequireOnboarding(user)).StatusCode);
    }

    [Fact]
    public void UnverifiedEmailCannotSubmitEvenWithRequestedRole()
    {
        var user = Applicant(); user.EmailVerifiedAt = null;
        Assert.Equal(403, Assert.Throws<HttpException>(() => H.RequireOnboarding(user)).StatusCode);
    }

    [Fact]
    public void AdministratorCannotReviewOwnProfile()
    {
        var admin = Applicant(); admin.Role = UserRole.Admin;
        Assert.Equal(403, Assert.Throws<HttpException>(() => H.RequireAdmin(admin, admin.Id)).StatusCode);
    }

    [Fact]
    public void StaleVersionIsConflict()
    {
        var error = Assert.Throws<HttpException>(() => H.CheckVersion(new() { Version = 8 }, 7));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal("SUPPLIER_VERSION_CONFLICT", error.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too short")]
    public void RejectReasonMustBeMeaningful(string reason) => Assert.Throws<HttpException>(() => H.Reason(reason));

    [Fact]
    public void NormalizationCanonicalizesTaxCodeAndSortsDocumentIds()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var result = H.Normalize(new() { CompanyName = "  Bakery  ", Address = "  Address  ",
            TaxCode = "0312345678-001", ExpectedVersion = 0, DocumentIds = [first, second] });
        Assert.Equal("Bakery", result.CompanyName);
        Assert.Equal("0312345678001", result.TaxCode);
        Assert.Equal(new[] { first, second }.Order().ToArray(), result.DocumentIds);
    }

    [Theory]
    [InlineData("' OR 1=1 --")]
    [InlineData("123456789")]
    [InlineData("１２３４５６７８９０")]
    public void TaxCodeInjectionAndInvalidFormatsAreRejected(string value) =>
        Assert.Throws<HttpException>(() => H.Normalize(new() { CompanyName = "Bakery", Address = "Address",
            TaxCode = value, ExpectedVersion = 0, DocumentIds = [Guid.NewGuid()] }));

    [Fact]
    public void ReviewDecisionMustBePresent()
    {
        var values = new List<ValidationResult>();
        var request = new ReviewSupplierRequest { SubmissionId = Guid.NewGuid(), ExpectedVersion = 1 };
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), values, true);
        Assert.False(valid);
        Assert.Contains(values, result => result.MemberNames.Contains(nameof(ReviewSupplierRequest.Approve)));
    }

    [Fact]
    public void LegacyFileUrlPayloadIsNotSilentlyAccepted() => Assert.Throws<JsonException>(() =>
        JsonSerializer.Deserialize<ApplySupplierRequest>(
            """{"CompanyName":"Bakery","Address":"Address","TaxCode":"0312345678","ExpectedVersion":0,"Documents":[{"FileUrl":"http://127.0.0.1/private"}]}"""));

    [Theory]
    [InlineData("<html>script</html>", "file.pdf")]
    [InlineData("%PDF-1.7 safe", "file.svg")]
    [InlineData("text document", "file.docx")]
    [InlineData("binary document", "no-extension")]
    public void AllDocumentFormatsAreAcceptedWithinSizeLimit(string value, string name) =>
        Assert.Equal(name, H.ValidateContent(Encoding.ASCII.GetBytes(value), name).Name);

    [Theory]
    [InlineData(0)]
    [InlineData(10 * 1024 * 1024 + 1)]
    public void DocumentSizeOutsideTheAllowedRangeIsRejected(int length) =>
        Assert.Equal(413, Assert.Throws<HttpException>(() => H.ValidateContent(new byte[length], "file.anything")).StatusCode);

    [Fact]
    public async Task NonSeekableSizedReadCannotExceedBound()
    {
        using var stream = new MemoryStream(new byte[10 * 1024 * 1024 + 1]);
        Assert.Equal(413, (await Assert.ThrowsAsync<HttpException>(() => H.ReadBoundedAsync(stream, default))).StatusCode);
    }

    [Fact]
    public void ExpiredOrForeignDocumentsCannotBeSubmitted()
    {
        var owner = Guid.NewGuid();
        var docs = new[] { new SupplierVerificationDocument { OwnerUserId = Guid.NewGuid(), UploadStatus = "Ready", DocumentType = "BusinessLicense" } };
        Assert.Throws<HttpException>(() => H.CheckDocuments(docs, 1, owner, DateTime.UtcNow, true));
    }

    [Fact]
    public void RevokedSupplierCanOnlyFulfillOlderTransactions()
    {
        var now = DateTime.UtcNow;
        var user = Applicant();
        var profile = new SupplierProfile { VerificationStatus = SupplierVerificationStatus.Revoked, RevokedAtUtc = now };
        Assert.True(H.CanFulfill(user, profile, now.AddDays(-1)));
        Assert.False(H.CanFulfill(user, profile, now.AddDays(1)));
        user.IsActive = false;
        Assert.False(H.CanFulfill(user, profile, now.AddDays(-1)));
    }

    [Fact]
    public async Task MiddlewareReplacesStaleSupplierClaimWithCurrentCustomer()
    {
        var user = Applicant();
        var repository = new Mock<ISupplierRepository>();
        repository.Setup(x => x.GetUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, "Supplier")
        }, "test"));
        var called = false;
        await new CurrentAccountMiddleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context, repository.Object);
        Assert.True(called);
        Assert.False(context.User.IsInRole("Supplier"));
        Assert.True(context.User.IsInRole("Customer"));
    }
}
