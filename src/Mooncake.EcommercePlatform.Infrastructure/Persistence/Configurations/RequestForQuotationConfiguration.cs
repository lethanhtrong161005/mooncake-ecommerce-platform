using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class RequestForQuotationConfiguration : IEntityTypeConfiguration<RequestForQuotation>
{
    public void Configure(EntityTypeBuilder<RequestForQuotation> builder)
    {
        builder.ToTable("request_for_quotations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Visibility)
            .HasColumnName("visibility")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(RfqVisibility.InviteOnly)
            .HasConversion(new SnakeCaseEnumConverter<RfqVisibility>());
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(RfqStatus.Draft)
            .HasConversion(new SnakeCaseEnumConverter<RfqStatus>());
        builder.Property(x => x.QuoteDeadline).HasColumnName("quote_deadline").HasColumnType("timestamptz(3)");
        builder.Property(x => x.RequiredDeliveryDate).HasColumnName("required_delivery_date").HasColumnType("date");
        builder.Property(x => x.DeliveryAddress).HasColumnName("delivery_address").IsRequired();
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_rfq_customer");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_rfq_status");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_rfq_customer");
    }
}
