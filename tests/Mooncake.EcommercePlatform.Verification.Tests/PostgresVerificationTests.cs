namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Npgsql;
using Npgsql.NameTranslation;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;

/// <summary>Only an explicitly supplied loopback PostgreSQL test server is used. No appsettings connection is read.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_PG_CONNECTION_STRING")))
            Skip = "Set TEST_PG_CONNECTION_STRING to a disposable local pgvector-enabled PostgreSQL server.";
    }
}

public sealed class PostgresVerificationFixture : IAsyncLifetime
{
    private string? _maintenance;
    private string? _database;
    private NpgsqlDataSource? _source;

    public ApplicationDbContext Create(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_source!, x => x.UseVector())
            .AddInterceptors(interceptors).Options);

    public Task InitializeAsync() => InitializeAsync(false);
    public Task InitializeLegacyAsync() => InitializeAsync(true);
    public ApplicationDbContext CreateLegacy() => new PreVerificationDbContext(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_source!, x => x.UseVector()).Options);

    private async Task InitializeAsync(bool legacy)
    {
        _maintenance = Environment.GetEnvironmentVariable("TEST_PG_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(_maintenance)) return;
        var connection = new NpgsqlConnectionStringBuilder(_maintenance);
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Verification integration tests require an explicitly configured loopback test server.");
        _database = $"sv_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(_maintenance))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            await command.ExecuteNonQueryAsync();
        }
        connection.Database = _database;
        connection.SearchPath = "public,extensions";
        var builder = new NpgsqlDataSourceBuilder(connection.ConnectionString);
        builder.UseVector();
        var translator = new NpgsqlNullNameTranslator();
        builder.MapEnum<UserRole>("user_role", translator);
        builder.MapEnum<ShopStatus>("shop_status", translator);
        builder.MapEnum<ProductStatus>("product_status", translator);
        builder.MapEnum<OrderStatus>("order_status", translator);
        builder.MapEnum<PaymentMethod>("payment_method", translator);
        builder.MapEnum<PaymentStatus>("payment_status", translator);
        builder.MapEnum<DeliveryType>("delivery_type", translator);
        builder.MapEnum<DeliveryStatus>("delivery_status", translator);
        builder.MapEnum<ProofType>("proof_type", translator);
        builder.MapEnum<CustomPackagingStatus>("custom_packaging_status", translator);
        builder.MapEnum<RfqStatus>("rfq_status", translator);
        builder.MapEnum<RfqInvitationStatus>("rfq_invitation_status", translator);
        builder.MapEnum<BidStatus>("bid_status", translator);
        builder.MapEnum<ContractStatus>("contract_status", translator);
        builder.MapEnum<MilestoneStatus>("milestone_status", translator);
        builder.MapEnum<ReputationEvent>("reputation_event", translator);
        builder.MapEnum<DiscountType>("discount_type", translator);
        builder.MapEnum<AiConversationType>("ai_conversation_type", translator);
        builder.MapEnum<AiMessageRole>("ai_message_role", translator);
        builder.MapEnum<AiReportType>("ai_report_type", translator);
        builder.MapEnum<AiReportStatus>("ai_report_status", translator);
        _source = builder.Build();
        await using var db = Create();
        await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS extensions");
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public");
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\" WITH SCHEMA extensions");
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA extensions");
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public");
        if (!legacy) await db.Database.EnsureCreatedAsync();
        else
        {
            var assembly = db.GetService<IMigrationsAssembly>();
            var prior = assembly.Migrations.Where(x => string.CompareOrdinal(x.Key, "20261024035322_SupplierVerificationWorkflow") < 0).OrderBy(x => x.Key).ToArray();
            await using var baseline = CreateLegacy();
            await baseline.Database.EnsureCreatedAsync();
            await baseline.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" ("MigrationId" character varying(150) NOT NULL PRIMARY KEY,
                    "ProductVersion" character varying(32) NOT NULL)
                """);
            var productVersion = "8.0.11";
            foreach (var item in prior)
                await baseline.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({item.Key}, {productVersion})");
        }
    }

    public async Task DisposeAsync()
    {
        if (_source != null) await _source.DisposeAsync();
        if (_database == null || _maintenance == null) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(_database, "^sv_test_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing to drop an unrecognized test database.");
        await using var admin = new NpgsqlConnection(_maintenance);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    public static SupplierService Service(ApplicationDbContext db) => new(new SupplierRepository(db),
        new TestClock(), Mock.Of<ICloudinaryStorageService>(), Mock.Of<IVerificationDocumentGateway>(), Mock.Of<ITraceContext>());

    public async Task<(Guid Owner, Guid Admin, Guid Profile, Guid Submission, long Version)> SeedAsync()
    {
        await using var db = Create();
        var now = DateTime.UtcNow;
        var owner = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", Role = UserRole.Customer,
            RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = now };
        var admin = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", Role = UserRole.Admin, IsActive = true };
        var profile = new SupplierProfile { Id = Guid.NewGuid(), UserId = owner.Id };
        db.AddRange(owner, admin, profile);
        var documentIds = new List<Guid>();
        foreach (var type in new[] { "BusinessLicense", "FoodSafetyCertificate" })
        {
            var doc = new SupplierVerificationDocument { Id = Guid.NewGuid(), OwnerUserId = owner.Id,
                SupplierProfileId = profile.Id, DocumentType = type, UploadStatus = "Ready",
                UploadOperationId = Guid.NewGuid(), FileSizeBytes = 100, OriginalFileName = "certificate.pdf" };
            db.Add(doc); documentIds.Add(doc.Id);
        }
        await db.SaveChangesAsync();
        var result = await Service(db).ApplyAsync(owner.Id, Guid.NewGuid().ToString(), new() {
            ExpectedVersion = 0, CompanyName = "Integration Bakery", Address = "Test Address",
            TaxCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000000000).ToString("D10"),
            DocumentIds = documentIds }, default);
        return (owner.Id, admin.Id, profile.Id, result.SubmissionId!.Value, result.Version);
    }

    private sealed class TestClock : IDateTimeProvider { public DateTime UtcNow => DateTime.UtcNow; }
}

public sealed class PostgresVerificationTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    [PostgresFact]
    public async Task MigrationBackfillsLegacyDocumentsWithoutPublishingTheirUrls()
    {
        var legacy = new PostgresVerificationFixture();
        try
        {
            await legacy.InitializeLegacyAsync();
            var owner = new User { Id = Guid.NewGuid(), Email = "legacy@example.test", Role = UserRole.Supplier,
                RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = DateTime.UtcNow };
            var profile = new SupplierProfile { Id = Guid.NewGuid(), UserId = owner.Id, CompanyName = "Legacy Bakery",
                TaxCode = "0312345678", Address = "Legacy address", VerificationStatus = SupplierVerificationStatus.Verified };
            var first = Guid.NewGuid();
            await using (var baseline = legacy.CreateLegacy())
            {
                baseline.Users.Add(owner);
                baseline.SupplierProfiles.Add(profile);
                foreach (var type in new[] { "BusinessLicense", "FoodSafetyCertificate" })
                    baseline.SupplierVerificationDocuments.Add(new SupplierVerificationDocument {
                        Id = type == "BusinessLicense" ? first : Guid.NewGuid(), SupplierProfileId = profile.Id,
                        DocumentType = type, FileUrl = "https://example.invalid/legacy.pdf",
                        ReviewStatus = "Approved", SubmittedAt = DateTime.UtcNow });
                await baseline.SaveChangesAsync();
            }
            await using var current = legacy.Create();
            await current.Database.MigrateAsync();
            var imported = await current.SupplierProfiles.SingleAsync(x => x.Id == profile.Id);
            Assert.Equal(profile.Id, imported.CurrentSubmissionId);
            Assert.Equal(SupplierVerificationStatus.Verified, imported.VerificationStatus);
            Assert.Equal("LegacyImport", (await current.SupplierVerificationSubmissions.SingleAsync()).Source);
            var docs = await current.SupplierVerificationDocuments.ToListAsync();
            Assert.Equal(2, docs.Select(x => x.UploadOperationId).Distinct().Count());
            Assert.All(docs, doc => {
                Assert.Equal(owner.Id, doc.OwnerUserId);
                Assert.Equal("LegacyNeedsReconciliation", doc.UploadStatus);
                Assert.Equal(profile.Id, doc.SubmissionId);
            });
        }
        finally { await legacy.DisposeAsync(); }
    }
    [PostgresFact]
    public async Task TwoAdministratorsCannotCommitDifferentDecisionsForTheSameVersion()
    {
        var seed = await fixture.SeedAsync();
        var key1 = Guid.NewGuid().ToString(); var key2 = Guid.NewGuid().ToString();
        async Task<int> Review(bool approve, string key)
        {
            await using var db = fixture.Create();
            try
            {
                await PostgresVerificationFixture.Service(db).ReviewAsync(seed.Profile, seed.Admin, key, new() {
                    SubmissionId = seed.Submission, ExpectedVersion = seed.Version, Approve = approve,
                    RejectionReason = approve ? null : "Concurrent reviewer rejection." }, default);
                return 200;
            }
            catch (HttpException error) { return error.StatusCode; }
        }
        var responses = await Task.WhenAll(Review(true, key1), Review(false, key2));
        Assert.Equal(new[] { 200, 409 }, responses.Order().ToArray());
        await using var check = fixture.Create();
        Assert.Equal(1, await check.AuditLogs.CountAsync(x => x.EntityId == seed.Profile.ToString()));
        Assert.Equal(2, await check.VerificationOutboxMessages.CountAsync(x => x.AggregateId == seed.Profile &&
            x.EventType != "SupplierApplicationSubmitted"));
    }

    [PostgresFact]
    public async Task RetryAfterCommitReturnsOriginalDecisionWithoutNewSideEffects()
    {
        var seed = await fixture.SeedAsync();
        var key = Guid.NewGuid().ToString();
        var request = new Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests.ReviewSupplierRequest {
            SubmissionId = seed.Submission, ExpectedVersion = seed.Version, Approve = true };
        await using (var first = fixture.Create())
            await PostgresVerificationFixture.Service(first).ReviewAsync(seed.Profile, seed.Admin, key, request, default);
        await using var second = fixture.Create();
        var replay = await PostgresVerificationFixture.Service(second).ReviewAsync(seed.Profile, seed.Admin, key, request, default);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(1, await second.AuditLogs.CountAsync(x => x.EntityId == seed.Profile.ToString()));
    }

    [PostgresFact]
    public async Task QueueQueryCountIsConstantAndSearchIsParameterized()
    {
        var seed = await fixture.SeedAsync();
        var counter = new QueryCounter();
        await using var db = fixture.Create(counter);
        var service = PostgresVerificationFixture.Service(db);
        await service.ListAsync(seed.Admin, new() { PageSize = 100 }, default);
        Assert.InRange(counter.Reads, 2, 3);
        counter.Reads = 0;
        var result = await service.ListAsync(seed.Admin, new() { Q = "' OR 1=1 --" }, default);
        Assert.Empty(result.Items);
        Assert.InRange(counter.Reads, 2, 3);
    }

    [PostgresFact]
    public async Task NotificationDedupAndReadAllPreserveNewerUnreadNotice()
    {
        var seed = await fixture.SeedAsync();
        var message = new VerificationOutboxMessage { Id = Guid.NewGuid(), EventId = Guid.NewGuid(),
            EventType = "SupplierVerificationApproved", AggregateId = seed.Profile, AggregateVersion = 2, OwnerUserId = seed.Owner };
        async Task Deliver()
        {
            await using var db = fixture.Create();
            await new VerificationNotificationRepository(db).MaterializeAsync(message, [seed.Owner], false, DateTime.UtcNow, default);
        }
        await Task.WhenAll(Deliver(), Deliver());
        var protector = new VerificationReadTokenProtector(new EphemeralDataProtectionProvider());
        string boundary;
        await using (var snapshot = fixture.Create())
        {
            var service = Notifications(snapshot, protector);
            boundary = (await service.InboxAsync(seed.Owner, 1, 20, default)).ReadThroughToken;
            Assert.Equal(1, await snapshot.Notifications.CountAsync(x => x.UserId == seed.Owner && x.SourceEventId == message.EventId));
        }
        await using (var newer = fixture.Create())
            await new VerificationNotificationRepository(newer).MaterializeAsync(new VerificationOutboxMessage {
                Id = Guid.NewGuid(), EventId = Guid.NewGuid(), EventType = "SupplierVerificationRevoked", AggregateId = seed.Profile,
                AggregateVersion = 3, OwnerUserId = seed.Owner }, [seed.Owner], false, DateTime.UtcNow, default);
        await using var read = fixture.Create();
        await Notifications(read, protector).ReadAllAsync(seed.Owner, boundary, Guid.NewGuid().ToString(), default);
        Assert.Equal(1, await read.Notifications.CountAsync(x => x.UserId == seed.Owner && x.SourceEventId != null && !x.IsRead));
    }

    private static VerificationNotificationService Notifications(ApplicationDbContext db, IVerificationReadTokenProtector protector)
    {
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => DateTime.UtcNow);
        return new(new SupplierRepository(db), new VerificationNotificationRepository(db), protector,
            Mock.Of<IVerificationRealtimePublisher>(), Mock.Of<IEmailSender>(), clock.Object);
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        public int Reads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Reads++; return ValueTask.FromResult(result); }
    }
}

/// <summary>Pre-feature runtime schema: existing business mappings with only this feature's expansion excluded.</summary>
public sealed class PreVerificationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        ShopTestSchemaHelper.RemoveExpansion(model);
        model.Ignore<SupplierVerificationSubmission>();
        model.Ignore<VerificationOutboxMessage>();
        model.Ignore<VerificationIdempotencyRecord>();
        var profile = model.Entity<SupplierProfile>();
        foreach (var name in new[] { "Version", "CurrentSubmissionId", "LastApprovedSubmissionId",
            "LastSubmittedAtUtc", "RevokedAtUtc", "RevokedByUserId", "RevocationReason" }) profile.Ignore(name);
        var document = model.Entity<SupplierVerificationDocument>();
        foreach (var name in new[] { "OwnerUserId", "SubmissionId", "UploadStatus", "UploadOperationId",
            "UploadRequestHash", "Sha256", "ProviderPublicId", "ProviderResourceType", "ProviderDeliveryType",
            "ValidationCompletedAtUtc" }) document.Ignore(name);
        model.Entity<User>().Ignore(x => x.NotificationSequence);
        var notification = model.Entity<Notification>();
        foreach (var name in new[] { "SourceEventId", "AggregateVersion", "AudienceKind", "Sequence", "ReadAtUtc" })
            notification.Ignore(name);
    }
}
