namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => entity.Key).IsUnique();
        builder.Property(entity => entity.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(entity => entity.Key).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.ValueJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(entity => entity.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);
    }
}
