using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.ContractId).HasColumnName("contract_id");
        builder.Property(x => x.Rating).HasColumnName("rating").HasColumnType("smallint").IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("uq_reviews_order").HasFilter("order_id IS NOT NULL");
        builder.HasIndex(x => x.ContractId).IsUnique().HasDatabaseName("uq_reviews_contract").HasFilter("contract_id IS NOT NULL");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("ix_reviews_supplier");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_reviews_customer");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_reviews_customer");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_reviews_supplier");

        builder.HasOne(x => x.Order)
            .WithOne()
            .HasForeignKey<Review>(x => x.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_reviews_order");

        builder.HasOne(x => x.Contract)
            .WithOne()
            .HasForeignKey<Review>(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_reviews_contract");
    }
}
