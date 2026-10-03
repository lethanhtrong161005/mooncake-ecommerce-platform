using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class PriceNegotiationConfiguration : IEntityTypeConfiguration<PriceNegotiation>
{
    public void Configure(EntityTypeBuilder<PriceNegotiation> builder)
    {
        builder.ToTable("price_negotiations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.QuotationId).HasColumnName("quotation_id").IsRequired();
        builder.Property(x => x.RoundNo).HasColumnName("round_no").IsRequired();
        builder.Property(x => x.ProposedBy)
            .HasColumnName("proposed_by")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<NegotiationProposedBy>());
        builder.Property(x => x.ProposedAmount).HasColumnName("proposed_amount").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.Message).HasColumnName("message");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(NegotiationStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<NegotiationStatus>());
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.QuotationId, x.RoundNo }).IsUnique().HasDatabaseName("uq_negotiation_round");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.PriceNegotiations)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_negotiations_quotation");
    }
}
