namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ContractDepositRule"/>.</summary>
public class ContractDepositRuleConfiguration : IEntityTypeConfiguration<ContractDepositRule>
{
    public void Configure(EntityTypeBuilder<ContractDepositRule> builder)
    {
            builder.HasKey(e => e.Id).HasName("contract_deposit_rules_pkey");

            builder.ToTable("contract_deposit_rules");

            builder.HasIndex(e => e.IsMandatory, "idx_cdr_is_mandatory").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.AppliesToAll)
                .HasDefaultValue(true)
                .HasColumnName("applies_to_all");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.DepositPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("deposit_percentage");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.IsMandatory)
                .HasDefaultValue(true)
                .HasColumnName("is_mandatory");
            builder.Property(e => e.MinContractValue)
                .HasPrecision(12, 2)
                .HasColumnName("min_contract_value");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.PaymentDeadlineHours)
                .HasDefaultValue(48)
                .HasColumnName("payment_deadline_hours");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
