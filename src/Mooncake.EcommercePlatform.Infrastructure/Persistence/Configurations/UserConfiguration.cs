namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Fluent API configuration for the User entity.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
               .HasColumnName("user_id")
               .HasColumnType("char(36)")   // UUID stored as CHAR(36)
               .ValueGeneratedNever();

        // ── User fields ───────────────────────────────────────────────────────
        builder.Property(u => u.Username)
               .HasColumnName("username")
               .HasMaxLength(50)
               .IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Email)
               .HasColumnName("email")
               .HasMaxLength(255)
               .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FullName)
               .HasColumnName("full_name")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(u => u.PasswordHash)
               .HasColumnName("password_hash")
               .HasMaxLength(512)
               .IsRequired();

        // ── Role stored as string enum ────────────────────────────────────────
        builder.Property(u => u.Role)
               .HasColumnName("role")
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(UserRole.USER)
               .IsRequired();

        // ── Audit fields ──────────────────────────────────────────────────────
        builder.Property(u => u.CreatedAtUtc)
               .HasColumnName("created_at")
               .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
               .HasColumnName("updated_at")
               .IsRequired();

        builder.Property(u => u.CreatedBy)
               .HasColumnName("created_by")
               .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
               .HasColumnName("updated_by")
               .HasMaxLength(100);

        // ── Soft delete ───────────────────────────────────────────────────────
        builder.Property(u => u.IsDeleted)
               .HasColumnName("is_deleted")
               .HasDefaultValue(false)
               .IsRequired();
    }
}
