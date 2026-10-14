namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => entity.ActorUserId);
        builder.Property(entity => entity.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(entity => entity.Action).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.EntityName).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.EntityId).HasMaxLength(128);
        builder.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(entity => entity.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);
        builder.HasOne<User>().WithMany().HasForeignKey(entity => entity.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
