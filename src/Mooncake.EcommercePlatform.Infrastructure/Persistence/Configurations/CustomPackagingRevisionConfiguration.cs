namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="CustomPackagingRevision"/>.</summary>
public class CustomPackagingRevisionConfiguration : IEntityTypeConfiguration<CustomPackagingRevision>
{
    public void Configure(EntityTypeBuilder<CustomPackagingRevision> builder)
    {
        builder.ToTable("custom_packaging_revisions");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.CustomPackagingId, e.RevisionNumber }).IsUnique();
        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.DesignFee).HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasOne<CustomPackaging>().WithMany().HasForeignKey(e => e.CustomPackagingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.SubmittedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
