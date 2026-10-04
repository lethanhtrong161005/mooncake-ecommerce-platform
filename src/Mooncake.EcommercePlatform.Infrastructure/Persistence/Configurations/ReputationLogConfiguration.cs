namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="ReputationLog"/>.</summary>
public class ReputationLogConfiguration : IEntityTypeConfiguration<ReputationLog>
{
    public void Configure(EntityTypeBuilder<ReputationLog> builder)
    {
            builder.HasKey(e => e.Id).HasName("reputation_logs_pkey");

            builder.ToTable("reputation_logs");

            builder.HasIndex(e => new { e.ReferenceId, e.ReferenceType }, "idx_rl_reference").HasFilter("(reference_id IS NOT NULL)");

            builder.HasIndex(e => new { e.SupplierId, e.CreatedAt }, "idx_rl_supplier_id").IsDescending(false, true);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Reason).HasColumnName("reason");
            builder.Property(e => e.ReferenceId).HasColumnName("reference_id");
            builder.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .HasColumnName("reference_type");
            builder.Property(e => e.ScoreDelta)
                .HasPrecision(5, 2)
                .HasColumnName("score_delta");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.EventType).HasColumnName("event_type");
        builder.HasIndex(e => e.EventType, "idx_rl_event_type");
    }
}
