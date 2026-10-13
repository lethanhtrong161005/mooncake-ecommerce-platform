namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence mapping for one-use Google login attempts.</summary>
public class GoogleLoginAttemptConfiguration : IEntityTypeConfiguration<GoogleLoginAttempt>
{
    public void Configure(EntityTypeBuilder<GoogleLoginAttempt> builder)
    {
        builder.ToTable("google_login_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StateHash).HasMaxLength(64).HasColumnName("state_hash");
        builder.Property(x => x.CodeVerifier).HasMaxLength(128).HasColumnName("code_verifier");
        builder.Property(x => x.Nonce).HasMaxLength(128).HasColumnName("nonce");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(x => x.StateHash).IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
