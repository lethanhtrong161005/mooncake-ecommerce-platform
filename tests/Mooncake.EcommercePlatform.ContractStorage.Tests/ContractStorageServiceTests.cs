namespace Mooncake.EcommercePlatform.ContractStorage.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Options;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class ContractStorageServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Now = new(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UploadPdfStoresHashAndRemovesPreviousObject()
    {
        var repository = new FakeContractRepository
        {
            Contract = DraftContract().WithDocument("old/document.pdf")
        };
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);
        var bytes = "%PDF-1.4\ncontract\n%%EOF"u8.ToArray();

        var result = await service.UploadDocumentAsync(UserId, repository.Contract.Id,
            new MemoryStream(bytes), "contract.pdf", "application/pdf", bytes.Length);

        Assert.True(result.HasDocument);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), result.SignedDocumentSha256);
        Assert.Single(storage.UploadedPaths);
        Assert.Contains("old/document.pdf", storage.DeletedPaths);
    }

    [Fact]
    public async Task UploadRejectsContentThatIsNotPdf()
    {
        var repository = new FakeContractRepository { Contract = DraftContract() };
        var storage = new FakeStorage();
        var bytes = "not-a-pdf"u8.ToArray();

        var error = await Assert.ThrowsAsync<HttpException>(() => CreateService(repository, storage)
            .UploadDocumentAsync(UserId, repository.Contract.Id, new MemoryStream(bytes),
                "contract.pdf", "application/pdf", bytes.Length));

        Assert.Equal(400, error.StatusCode);
        Assert.Empty(storage.UploadedPaths);
    }

    [Fact]
    public async Task DownloadCreatesShortLivedUrlForContractParty()
    {
        var repository = new FakeContractRepository
        {
            Contract = DraftContract().WithDocument("contract/document.pdf")
        };
        repository.Contract.SignedDocumentSha256 = "ABC123";
        var storage = new FakeStorage();

        var result = await CreateService(repository, storage)
            .GetDocumentDownloadAsync(UserId, repository.Contract.Id);

        Assert.Equal("https://signed.example/contract.pdf", result.DownloadUrl);
        Assert.Equal(Now.AddMinutes(5), result.ExpiresAtUtc);
        Assert.Equal("contract/document.pdf", storage.LastSignedPath);
    }

    [Fact]
    public async Task ContractCannotEnterSigningWithoutPdf()
    {
        var repository = new FakeContractRepository { Contract = DraftContract() };

        var error = await Assert.ThrowsAsync<HttpException>(() => CreateService(repository, new FakeStorage())
            .BeginSignatureAsync(UserId, repository.Contract.Id));

        Assert.Equal(409, error.StatusCode);
        Assert.False(repository.BeginSignatureCalled);
    }

    private static ContractService CreateService(FakeContractRepository repository, FakeStorage storage) =>
        new(repository, new NoopAuthService(), storage, Options.Create(new SupabaseStorageOptions
        {
            ProjectUrl = "https://example.supabase.co",
            SecretKey = "test", ContractBucket = "contracts",
            MaxContractFileBytes = 10 * 1024 * 1024, SignedUrlLifetimeSeconds = 300
        }), new FakeClock(), NullLogger<ContractService>.Instance);

    private static Contract DraftContract() => new()
    {
        Id = Guid.NewGuid(), CustomerId = UserId, SupplierId = Guid.NewGuid(),
        ContractNumber = "CTR-TEST", Status = ContractStatus.Draft
    };

    private sealed class FakeClock : IDateTimeProvider { public DateTime UtcNow => Now; }

    private sealed class FakeStorage : IContractFileStorage
    {
        public List<string> UploadedPaths { get; } = [];
        public List<string> DeletedPaths { get; } = [];
        public string? LastSignedPath { get; private set; }
        public Task UploadAsync(string objectPath, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            UploadedPaths.Add(objectPath);
            return Task.CompletedTask;
        }
        public Task<string> CreateSignedDownloadUrlAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            LastSignedPath = objectPath;
            return Task.FromResult("https://signed.example/contract.pdf");
        }
        public Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            DeletedPaths.Add(objectPath);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContractRepository : IContractRepository
    {
        public Contract Contract { get; set; } = null!;
        public bool BeginSignatureCalled { get; private set; }
        public Task<Contract?> GetContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Contract?>(Contract.Id == contractId ? Contract : null);
        public Task<IReadOnlyList<Contract>> GetContractsForUserAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Contract>>([Contract]);
        public Task<bool> IsPartyAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(contract.CustomerId == userId);
        public Task<bool> HasSignedAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> BeginSignatureAsync(Guid contractId, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            BeginSignatureCalled = true;
            return Task.FromResult(true);
        }
        public Task<ContractDocumentSaveResult?> SaveDocumentAsync(Guid contractId, Guid actorUserId,
            string objectPath, string sha256, CancellationToken cancellationToken = default)
        {
            var previous = Contract.SignedDocumentUrl;
            Contract.SignedDocumentUrl = objectPath;
            Contract.SignedDocumentSha256 = sha256;
            return Task.FromResult<ContractDocumentSaveResult?>(new(Contract, previous));
        }
        public Task<Contract?> SignAsync(Guid contractId, Guid actorUserId, string? ipAddress,
            string? userAgent, CancellationToken cancellationToken = default) => Task.FromResult<Contract?>(Contract);
    }

    private sealed class NoopAuthService : IAuthService
    {
        public Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResendVerificationAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RequestContractSignatureOtpAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task VerifyContractSignatureOtpAsync(Guid userId, Guid contractId, string code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

internal static class ContractTestExtensions
{
    public static Contract WithDocument(this Contract contract, string path)
    {
        contract.SignedDocumentUrl = path;
        return contract;
    }
}
