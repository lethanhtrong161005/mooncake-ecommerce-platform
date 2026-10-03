using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class RfqItemConfiguration : IEntityTypeConfiguration<RfqItem>
{
    public void Configure(EntityTypeBuilder<RfqItem> builder)
    {
        builder.ToTable("rfq_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.RfqId).HasColumnName("rfq_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.ItemName).HasColumnName("item_name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Specification).HasColumnName("specification");
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.CustomPackagingId).HasColumnName("custom_packaging_id");
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.RfqId).HasDatabaseName("ix_rfq_items_rfq");

        builder.HasOne(x => x.RequestForQuotation)
            .WithMany(x => x.RfqItems)
            .HasForeignKey(x => x.RfqId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_rfq_items_rfq");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_rfq_items_product");

        builder.HasOne(x => x.CustomPackaging)
            .WithMany()
            .HasForeignKey(x => x.CustomPackagingId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_rfq_items_packaging");
    }
}
