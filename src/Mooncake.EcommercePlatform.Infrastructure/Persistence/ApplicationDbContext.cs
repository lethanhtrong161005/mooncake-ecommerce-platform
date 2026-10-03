namespace Mooncake.EcommercePlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Primary EF Core DbContext for the Mooncake platform (30 tables).</summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // ── A. Identity ──────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // ── B. Shop & Catalog ────────────────────────────────────────────────
    public DbSet<ShopTemplate> ShopTemplates => Set<ShopTemplate>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<PromotionRule> PromotionRules => Set<PromotionRule>();

    // ── C. Orders ────────────────────────────────────────────────────────
    public DbSet<CustomPackaging> CustomPackagings => Set<CustomPackaging>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // ── D. RFQ & Contract ────────────────────────────────────────────────
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<RfqInvitation> RfqInvitations => Set<RfqInvitation>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<PriceNegotiation> PriceNegotiations => Set<PriceNegotiation>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();

    // ── E. Payment & Delivery ────────────────────────────────────────────
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();

    // ── F. Review & Reputation ───────────────────────────────────────────
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReputationLog> ReputationLogs => Set<ReputationLog>();

    // ── G. Notification & AI ─────────────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiAnalyticsReport> AiAnalyticsReports => Set<AiAnalyticsReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
