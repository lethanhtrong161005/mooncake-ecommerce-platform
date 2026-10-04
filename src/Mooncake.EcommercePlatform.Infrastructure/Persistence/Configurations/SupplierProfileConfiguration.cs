namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="SupplierProfile"/>.</summary>
public class SupplierProfileConfiguration : IEntityTypeConfiguration<SupplierProfile>
{
    public void Configure(EntityTypeBuilder<SupplierProfile> builder)
    {
            builder.HasKey(e => e.Id).HasName("supplier_profiles_pkey");

            builder.ToTable("supplier_profiles");

            builder.HasIndex(e => e.ReputationScore, "idx_sp_reputation_score")
                .IsDescending()
                .HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.UserId, "idx_sp_user_id");

            builder.HasIndex(e => e.Verified, "idx_sp_verified").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.TaxCode, "supplier_profiles_tax_code_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Address).HasColumnName("address");
            builder.Property(e => e.AvgRating)
                .HasPrecision(3, 2)
                .HasColumnName("avg_rating");
            builder.Property(e => e.CompanyLogoUrl).HasColumnName("company_logo_url");
            builder.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("company_name");
            builder.Property(e => e.ContractsBreached)
                .HasDefaultValue(0)
                .HasColumnName("contracts_breached");
            builder.Property(e => e.ContractsLate)
                .HasDefaultValue(0)
                .HasColumnName("contracts_late");
            builder.Property(e => e.ContractsOnTime)
                .HasDefaultValue(0)
                .HasColumnName("contracts_on_time");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.ReputationScore)
                .HasPrecision(5, 2)
                .HasColumnName("reputation_score");
            builder.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");
            builder.Property(e => e.TotalContracts)
                .HasDefaultValue(0)
                .HasColumnName("total_contracts");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.UserId).HasColumnName("user_id");
            builder.Property(e => e.Verified)
                .HasDefaultValue(false)
                .HasColumnName("verified");
            builder.Property(e => e.VerifiedAt).HasColumnName("verified_at");
    }
}
