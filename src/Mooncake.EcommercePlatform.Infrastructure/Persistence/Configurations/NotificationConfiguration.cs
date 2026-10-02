using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Body).HasColumnName("body");
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50);
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.IsRead).HasColumnName("is_read").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReadAtUtc).HasColumnName("read_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAtUtc }).IsDescending(false, false, true).HasDatabaseName("ix_notifications_user");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notifications_user");
    }
}
