namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="Notification"/>.</summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
            builder.HasKey(e => e.Id).HasName("notifications_pkey");

            builder.ToTable("notifications");

            builder.HasIndex(e => e.Type, "idx_notif_type").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => new { e.UserId, e.IsRead }, "idx_notif_unread").HasFilter("((is_deleted = false) AND (is_read = false))");

            builder.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_notif_user_id")
                .IsDescending(false, true)
                .HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Content).HasColumnName("content");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            builder.Property(e => e.ReferenceId).HasColumnName("reference_id");
            builder.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .HasColumnName("reference_type");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.UserId).HasColumnName("user_id");
    }
}
