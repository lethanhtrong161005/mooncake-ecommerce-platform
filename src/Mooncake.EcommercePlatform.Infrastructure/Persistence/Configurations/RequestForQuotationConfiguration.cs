namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="RequestForQuotation"/>.</summary>
public class RequestForQuotationConfiguration : IEntityTypeConfiguration<RequestForQuotation>
{
    public void Configure(EntityTypeBuilder<RequestForQuotation> builder)
    {
            builder.HasKey(e => e.Id).HasName("request_for_quotations_pkey");

            builder.ToTable("request_for_quotations");

            builder.HasIndex(e => e.BidDeadline, "idx_rfq_bid_deadline").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.CustomerId, "idx_rfq_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.RfqNumber, "request_for_quotations_rfq_number_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BidDeadline).HasColumnName("bid_deadline");
            builder.Property(e => e.BudgetRangeMax)
                .HasPrecision(12, 2)
                .HasColumnName("budget_range_max");
            builder.Property(e => e.BudgetRangeMin)
                .HasPrecision(12, 2)
                .HasColumnName("budget_range_min");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.DeliveryAddress).HasColumnName("delivery_address");
            builder.Property(e => e.DeliveryDateRequired).HasColumnName("delivery_date_required");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.RfqNumber)
                .HasMaxLength(50)
                .HasColumnName("rfq_number");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_rfq_status");
    }
}
