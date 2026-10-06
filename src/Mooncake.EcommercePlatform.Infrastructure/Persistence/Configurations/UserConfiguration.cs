namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="User"/>.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
            builder.HasKey(e => e.Id).HasName("users_pkey");

            builder.ToTable("users");

            builder.HasIndex(e => e.Email, "idx_users_email").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.IsActive, "idx_users_is_active").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.Email, "users_email_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            builder.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
            builder.Property(e => e.FailedLoginAttempts).HasDefaultValue(0).HasColumnName("failed_login_attempts");
            builder.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.LockoutUntil).HasColumnName("lockout_until");
            builder.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            builder.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.RequestedRole).HasColumnName("requested_role").HasDefaultValue(UserRole.Customer);
        builder.Property(e => e.EmailVerificationCodeHash).HasMaxLength(64).HasColumnName("email_verification_code_hash");
        builder.Property(e => e.EmailVerificationCodeExpiresAt).HasColumnName("email_verification_code_expires_at");
        builder.Property(e => e.EmailVerificationCodeAttempts).HasDefaultValue(0).HasColumnName("email_verification_code_attempts");
        builder.Property(e => e.PasswordResetCodeHash).HasMaxLength(64).HasColumnName("password_reset_code_hash");
        builder.Property(e => e.PasswordResetCodeExpiresAt).HasColumnName("password_reset_code_expires_at");
        builder.Property(e => e.PasswordResetCodeAttempts).HasDefaultValue(0).HasColumnName("password_reset_code_attempts");
        builder.HasIndex(e => e.Role, "idx_users_role");
    }
}
