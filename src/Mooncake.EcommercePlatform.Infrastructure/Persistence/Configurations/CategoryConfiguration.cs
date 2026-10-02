using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("uq_categories_slug");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("ix_categories_parent");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_categories_parent");
    }
}
