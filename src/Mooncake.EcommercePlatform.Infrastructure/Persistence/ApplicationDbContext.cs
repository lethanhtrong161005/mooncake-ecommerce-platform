using Npgsql.NameTranslation;
namespace Mooncake.EcommercePlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Primary EF Core DbContext for the Mooncake platform.</summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AiAnalyticsReport> AiAnalyticsReports => Set<AiAnalyticsReport>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
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
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<PromotionRule> PromotionRules => Set<PromotionRule>();
    public DbSet<ReputationLog> ReputationLogs => Set<ReputationLog>();
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<RfqInvitation> RfqInvitations => Set<RfqInvitation>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopTemplate> ShopTemplates => Set<ShopTemplate>();
    public DbSet<SupplierProfile> SupplierProfiles => Set<SupplierProfile>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Extensions
        modelBuilder.HasPostgresExtension("extensions", "uuid-ossp");
        modelBuilder.HasPostgresExtension("extensions", "pgcrypto");
        modelBuilder.HasPostgresExtension("vector");

        // Enums (with NpgsqlNullNameTranslator for exact PascalCase matching)
        var translator = new NpgsqlNullNameTranslator();

        modelBuilder.HasPostgresEnum<UserRole>("user_role", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ShopStatus>("shop_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ProductStatus>("product_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<OrderStatus>("order_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<PaymentMethod>("payment_method", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<PaymentStatus>("payment_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DeliveryType>("delivery_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DeliveryStatus>("delivery_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ProofType>("proof_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<CustomPackagingStatus>("custom_packaging_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RfqStatus>("rfq_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<RfqInvitationStatus>("rfq_invitation_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<BidStatus>("bid_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ContractStatus>("contract_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<MilestoneStatus>("milestone_status", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<ReputationEvent>("reputation_event", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<DiscountType>("discount_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiConversationType>("ai_conversation_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiMessageRole>("ai_message_role", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiReportType>("ai_report_type", nameTranslator: translator);
        modelBuilder.HasPostgresEnum<AiReportStatus>("ai_report_status", nameTranslator: translator);

        // Apply all entity configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
