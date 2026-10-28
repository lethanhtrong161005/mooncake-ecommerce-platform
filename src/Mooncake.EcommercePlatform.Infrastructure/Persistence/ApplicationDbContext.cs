using Npgsql.NameTranslation;
namespace Mooncake.EcommercePlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Primary EF Core DbContext for the Mooncake platform.</summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AiAnalyticsReport> AiAnalyticsReports => Set<AiAnalyticsReport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<CustomPackagingRevision> CustomPackagingRevisions => Set<CustomPackagingRevision>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<BidItem> BidItems => Set<BidItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractDepositRule> ContractDepositRules => Set<ContractDepositRule>();
    public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();
    public DbSet<CustomPackaging> CustomPackagings => Set<CustomPackaging>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCatalogOutboxEvent> ProductCatalogOutboxEvents => Set<ProductCatalogOutboxEvent>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductInventoryMovement> ProductInventoryMovements => Set<ProductInventoryMovement>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductReviewLike> ProductReviewLikes => Set<ProductReviewLike>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductSearchDocument> ProductSearchDocuments => Set<ProductSearchDocument>();
    public DbSet<ProductViewEvent> ProductViewEvents => Set<ProductViewEvent>();
    public DbSet<PromotionRule> PromotionRules => Set<PromotionRule>();
    public DbSet<ReputationLog> ReputationLogs => Set<ReputationLog>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<GoogleLoginAttempt> GoogleLoginAttempts => Set<GoogleLoginAttempt>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<RfqInvitation> RfqInvitations => Set<RfqInvitation>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopTemplate> ShopTemplates => Set<ShopTemplate>();
    public DbSet<ShopTemplateRevision> ShopTemplateRevisions => Set<ShopTemplateRevision>();
    public DbSet<ShopMediaAsset> ShopMediaAssets => Set<ShopMediaAsset>();
    public DbSet<ShopIdempotencyRecord> ShopIdempotencyRecords => Set<ShopIdempotencyRecord>();
    public DbSet<ShopTemplateCatalogState> ShopTemplateCatalogStates => Set<ShopTemplateCatalogState>();
    public DbSet<SupplierProfile> SupplierProfiles => Set<SupplierProfile>();
    public DbSet<SupplierVerificationSubmission> SupplierVerificationSubmissions => Set<SupplierVerificationSubmission>();
    public DbSet<VerificationOutboxMessage> VerificationOutboxMessages => Set<VerificationOutboxMessage>();
    public DbSet<VerificationIdempotencyRecord> VerificationIdempotencyRecords => Set<VerificationIdempotencyRecord>();
    public DbSet<SupplierVerificationDocument> SupplierVerificationDocuments => Set<SupplierVerificationDocument>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Extensions
        modelBuilder.HasPostgresExtension("extensions", "uuid-ossp");
        modelBuilder.HasPostgresExtension("extensions", "pgcrypto");
        modelBuilder.HasPostgresExtension("vector");

        // Enums (with NpgsqlNullNameTranslator for exact PascalCase matching)
        var translator = new NpgsqlNullNameTranslator();

        modelBuilder.HasPostgresEnum<UserRole>(schema: null, name: "user_role", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ShopStatus>(schema: null, name: "shop_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ProductStatus>(schema: null, name: "product_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<OrderStatus>(schema: null, name: "order_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<PaymentMethod>(schema: null, name: "payment_method", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<PaymentStatus>(schema: null, name: "payment_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DeliveryType>(schema: null, name: "delivery_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DeliveryStatus>(schema: null, name: "delivery_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ProofType>(schema: null, name: "proof_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<CustomPackagingStatus>(schema: null, name: "custom_packaging_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RfqStatus>(schema: null, name: "rfq_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RfqInvitationStatus>(schema: null, name: "rfq_invitation_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<BidStatus>(schema: null, name: "bid_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ContractStatus>(schema: null, name: "contract_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<MilestoneStatus>(schema: null, name: "milestone_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ReputationEvent>(schema: null, name: "reputation_event", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DiscountType>(schema: null, name: "discount_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiConversationType>(schema: null, name: "ai_conversation_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiMessageRole>(schema: null, name: "ai_message_role", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiReportType>(schema: null, name: "ai_report_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiReportStatus>(schema: null, name: "ai_report_status", nameTranslator: translator);

        // Apply all entity configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations.VerificationConfiguration.ConfigureVerification(modelBuilder);
        Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations.ShopFeatureConfiguration.ConfigureShops(modelBuilder);
        Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations.ProductCatalogConfiguration.ConfigureCatalog(modelBuilder);
    }
}
