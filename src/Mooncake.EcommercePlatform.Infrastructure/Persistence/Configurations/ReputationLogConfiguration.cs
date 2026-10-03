using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ReputationLogConfiguration : IEntityTypeConfiguration<ReputationLog>
{
    public void Configure(EntityTypeBuilder<ReputationLog> builder)
    {
        builder.ToTable("reputation_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<ReputationEventType>());
        builder.Property(x => x.ScoreDelta).HasColumnName("score_delta").IsRequired();
        builder.Property(x => x.ReviewId).HasColumnName("review_id");
        builder.Property(x => x.ContractId).HasColumnName("contract_id");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.SupplierId, x.CreatedAtUtc }).IsDescending(false, true).HasDatabaseName("ix_reputation_logs_supplier");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_replog_supplier");

        builder.HasOne(x => x.Review)
            .WithMany()
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_replog_review");

        builder.HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_replog_contract");
    }
}
