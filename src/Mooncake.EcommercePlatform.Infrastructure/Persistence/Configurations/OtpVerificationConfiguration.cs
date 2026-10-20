namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired().HasColumnName("phone");
        builder.Property(x => x.Purpose).HasMaxLength(50).IsRequired().HasColumnName("purpose");
        builder.Property(x => x.OtpHash).HasMaxLength(64).IsRequired().HasColumnName("otp_hash");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.LastSentAtUtc).HasColumnName("last_sent_at_utc");
        builder.Property(x => x.WindowStartedAtUtc).HasColumnName("window_started_at_utc");
        builder.Property(x => x.ResendCount).HasDefaultValue(0).HasColumnName("resend_count");
        builder.Property(x => x.FailedAttempts).HasDefaultValue(0).HasColumnName("failed_attempts");
        builder.Property(x => x.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(x => x.VerificationToken).HasMaxLength(64).HasColumnName("verification_token");
        builder.Property(x => x.TokenExpiresAtUtc).HasColumnName("token_expires_at_utc");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(x => new { x.Phone, x.Purpose }, "idx_otp_phone_purpose").IsUnique();
        builder.HasIndex(x => x.VerificationToken, "idx_otp_verification_token").IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc, "idx_otp_expires_at");
    }
}
