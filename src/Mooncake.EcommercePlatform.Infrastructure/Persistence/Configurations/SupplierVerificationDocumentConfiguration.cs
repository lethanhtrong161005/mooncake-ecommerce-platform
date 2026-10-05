namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="SupplierVerificationDocument"/>.</summary>
public class SupplierVerificationDocumentConfiguration : IEntityTypeConfiguration<SupplierVerificationDocument>
{
    public void Configure(EntityTypeBuilder<SupplierVerificationDocument> builder)
    {
        builder.ToTable("supplier_verification_documents");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.SupplierProfileId, e.ReviewStatus });
        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FileUrl).IsRequired();
        builder.Property(e => e.OriginalFileName).HasMaxLength(255);
        builder.Property(e => e.ContentType).HasMaxLength(150);
        builder.Property(e => e.ReviewStatus).HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasOne<SupplierProfile>().WithMany().HasForeignKey(e => e.SupplierProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
