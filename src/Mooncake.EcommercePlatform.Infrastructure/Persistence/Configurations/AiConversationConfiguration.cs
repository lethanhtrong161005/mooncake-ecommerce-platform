namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="AiConversation"/>.</summary>
public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
            builder.HasKey(e => e.Id).HasName("ai_conversations_pkey");

            builder.ToTable("ai_conversations");

            builder.HasIndex(e => e.SessionToken, "ai_conversations_session_token_key").IsUnique();

            builder.HasIndex(e => e.UserId, "idx_aic_user_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.EndedAt).HasColumnName("ended_at");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.SessionToken)
                .HasMaxLength(255)
                .HasColumnName("session_token");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ConversationType).HasColumnName("conversation_type");
        builder.HasIndex(e => e.ConversationType, "idx_aic_type");
    }
}
