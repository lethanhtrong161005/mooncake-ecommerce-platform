namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence mapping for refresh tokens.</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("user_id");
        builder.Property(token => token.TokenHash).HasMaxLength(64).HasColumnName("token_hash");
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64).HasColumnName("replaced_by_token_hash");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at");
        builder.Property(token => token.UpdatedAt).HasColumnName("updated_at");
        builder.Property(token => token.CreatedBy).HasColumnName("created_by");
        builder.Property(token => token.UpdatedBy).HasColumnName("updated_by");
        builder.Property(token => token.IsDeleted).HasColumnName("is_deleted");
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasOne(token => token.User).WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
