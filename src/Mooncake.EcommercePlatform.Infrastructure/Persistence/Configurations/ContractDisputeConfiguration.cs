namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ContractDispute"/>.</summary>
public class ContractDisputeConfiguration : IEntityTypeConfiguration<ContractDispute>
{
    public void Configure(EntityTypeBuilder<ContractDispute> builder)
    {
        builder.HasKey(e => e.Id).HasName("contract_disputes_pkey");
        builder.ToTable("contract_disputes");

        builder.HasIndex(e => e.ContractId, "idx_cdis_contract_id");
        builder.HasIndex(e => e.Status, "idx_cdis_status");
        builder.HasIndex(e => e.RaisedByUserId, "idx_cdis_raised_by");

        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
        builder.Property(e => e.ContractId).HasColumnName("contract_id");
        builder.Property(e => e.RaisedByUserId).HasColumnName("raised_by_user_id");
        builder.Property(e => e.DisputeReason).HasMaxLength(2000).HasColumnName("dispute_reason");
        builder.Property(e => e.EvidenceUrls).HasColumnType("text").HasColumnName("evidence_urls");
        builder.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending").HasColumnName("status");
        builder.Property(e => e.AdminVerdict).HasMaxLength(2000).HasColumnName("admin_verdict");
        builder.Property(e => e.ResolutionType).HasMaxLength(50).HasColumnName("resolution_type");
        builder.Property(e => e.RefundAmount).HasPrecision(18, 2).HasColumnName("refund_amount");
        builder.Property(e => e.RaisedAt).HasColumnName("raised_at");
        builder.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(e => e.ResolvedByUserId).HasColumnName("resolved_by_user_id");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
    }
}

