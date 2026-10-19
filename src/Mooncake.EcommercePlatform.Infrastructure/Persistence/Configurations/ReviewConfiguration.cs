namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="Review"/>.</summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
            builder.HasKey(e => e.Id).HasName("reviews_pkey");

            builder.ToTable("reviews");

            builder.HasIndex(e => e.ContractId, "idx_rev_contract_id").HasFilter("(contract_id IS NOT NULL)");

            builder.HasIndex(e => e.ContractId, "reviews_contract_id_key").IsUnique()
                .HasFilter("((contract_id IS NOT NULL) AND (is_deleted = false))");

            builder.HasIndex(e => e.CustomerId, "idx_rev_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.OrderId, "idx_rev_order_id").HasFilter("(order_id IS NOT NULL)");

            builder.HasIndex(e => e.OrderId, "reviews_order_id_key").IsUnique()
                .HasFilter("((order_id IS NOT NULL) AND (is_deleted = false))");

            builder.HasIndex(e => new { e.SupplierId, e.OverallRating }, "idx_rev_overall_rating").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.SupplierId, "idx_rev_supplier_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Comment).HasColumnName("comment");
            builder.Property(e => e.ContractId).HasColumnName("contract_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.DeliveryRating).HasColumnName("delivery_rating");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            builder.Property(e => e.IsAnonymous).HasDefaultValue(false).HasColumnName("is_anonymous");
            builder.Property(e => e.IsHiddenByAdmin).HasDefaultValue(false).HasColumnName("is_hidden_by_admin");
            builder.Property(e => e.MediaUrls).HasColumnType("jsonb").HasColumnName("media_urls");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.OverallRating).HasColumnName("overall_rating");
            builder.Property(e => e.QualityRating).HasColumnName("quality_rating");
            builder.Property(e => e.ServiceRating).HasColumnName("service_rating");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.SupplierReply).HasColumnName("supplier_reply");
            builder.Property(e => e.SupplierRepliedAt).HasColumnName("supplier_replied_at");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
