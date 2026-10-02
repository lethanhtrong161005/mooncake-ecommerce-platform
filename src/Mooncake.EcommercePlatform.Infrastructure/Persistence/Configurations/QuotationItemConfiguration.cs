using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
{
    public void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        builder.ToTable("quotation_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.QuotationId).HasColumnName("quotation_id").IsRequired();
        builder.Property(x => x.RfqItemId).HasColumnName("rfq_item_id").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.Note).HasColumnName("note");

        builder.HasIndex(x => new { x.QuotationId, x.RfqItemId }).IsUnique().HasDatabaseName("uq_quotation_items");
        builder.HasIndex(x => x.RfqItemId).HasDatabaseName("ix_quotation_items_rfq_item");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.QuotationItems)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_qi_quotation");

        builder.HasOne(x => x.RfqItem)
            .WithMany()
            .HasForeignKey(x => x.RfqItemId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_qi_rfq_item");
    }
}
