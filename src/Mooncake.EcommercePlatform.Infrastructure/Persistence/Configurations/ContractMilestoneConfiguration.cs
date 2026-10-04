namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="ContractMilestone"/>.</summary>
public class ContractMilestoneConfiguration : IEntityTypeConfiguration<ContractMilestone>
{
    public void Configure(EntityTypeBuilder<ContractMilestone> builder)
    {
            builder.HasKey(e => e.Id).HasName("contract_milestones_pkey");

            builder.ToTable("contract_milestones");

            builder.HasIndex(e => e.ContractId, "idx_cm_contract_id");

            builder.HasIndex(e => e.DueDate, "idx_cm_due_date").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => new { e.ContractId, e.IsDeposit }, "idx_cm_is_deposit");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            builder.Property(e => e.CompletionDate).HasColumnName("completion_date");
            builder.Property(e => e.ContractId).HasColumnName("contract_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.DueDate).HasColumnName("due_date");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.IsDeposit)
                .HasDefaultValue(false)
                .HasColumnName("is_deposit");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.Percentage)
                .HasPrecision(5, 2)
                .HasColumnName("percentage");
            builder.Property(e => e.SequenceNumber)
                .HasDefaultValue(1)
                .HasColumnName("sequence_number");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_cm_status");
    }
}
