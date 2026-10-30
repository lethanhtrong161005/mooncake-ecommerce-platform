namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ContractExtension"/>.</summary>
public class ContractExtensionConfiguration : IEntityTypeConfiguration<ContractExtension>
{
    public void Configure(EntityTypeBuilder<ContractExtension> builder)
    {
        builder.HasKey(e => e.Id).HasName("contract_extensions_pkey");
        builder.ToTable("contract_extensions");

        builder.HasIndex(e => e.ContractId, "idx_cext_contract_id");
        builder.HasIndex(e => e.Status, "idx_cext_status");

        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
        builder.Property(e => e.ContractId).HasColumnName("contract_id");
        builder.Property(e => e.ProposedDueDate).HasColumnName("proposed_due_date");
        builder.Property(e => e.Reason).HasMaxLength(1000).HasColumnName("reason");
        builder.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending").HasColumnName("status");
        builder.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(e => e.RequestedAt).HasColumnName("requested_at");
        builder.Property(e => e.RespondedAt).HasColumnName("responded_at");
        builder.Property(e => e.RespondedByUserId).HasColumnName("responded_by_user_id");
        builder.Property(e => e.ResponseNote).HasMaxLength(500).HasColumnName("response_note");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
    }
}

