namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="AiMessage"/>.</summary>
public class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
            builder.HasKey(e => e.Id).HasName("ai_messages_pkey");

            builder.ToTable("ai_messages");

            builder.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "idx_aim_conversation_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Content).HasColumnName("content");
            builder.Property(e => e.ConversationId).HasColumnName("conversation_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.ModelUsed)
                .HasMaxLength(100)
                .HasColumnName("model_used");
            builder.Property(e => e.TokensUsed).HasColumnName("tokens_used");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");
    }
}
