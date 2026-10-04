namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="CustomerProfile"/>.</summary>
public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
            builder.HasKey(e => e.Id).HasName("customer_profiles_pkey");

            builder.ToTable("customer_profiles");

            builder.HasIndex(e => e.UserId, "idx_cp_user_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BillingAddress).HasColumnName("billing_address");
            builder.Property(e => e.CompanyLogoUrl).HasColumnName("company_logo_url");
            builder.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("company_name");
            builder.Property(e => e.ContactPerson)
                .HasMaxLength(255)
                .HasColumnName("contact_person");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.UserId).HasColumnName("user_id");
    }
}
