namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Shared column mapping for chat entities deriving from <see cref="BaseEntity"/>.</summary>
internal static class ChatBaseEntityMapping
{
    public static void MapBase<T>(this EntityTypeBuilder<T> builder, string pkName) where T : BaseEntity
    {
        builder.HasKey(e => e.Id).HasName(pkName);
        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}

/// <summary>EF Core configuration for <see cref="ChatThread"/>.</summary>
public class ChatThreadConfiguration : IEntityTypeConfiguration<ChatThread>
{
    public void Configure(EntityTypeBuilder<ChatThread> builder)
    {
        builder.ToTable("chat_threads");
        builder.MapBase("chat_threads_pkey");

        builder.HasIndex(e => new { e.CustomerId, e.ShopId }, "uq_chat_threads_customer_shop").IsUnique();
        builder.HasIndex(e => new { e.CustomerId, e.LastActivityAt, e.Id }, "idx_chat_threads_customer_activity")
            .IsDescending(false, true, true);
        builder.HasIndex(e => new { e.SupplierId, e.LastActivityAt, e.Id }, "idx_chat_threads_supplier_activity")
            .IsDescending(false, true, true);

        builder.Property(e => e.CustomerId).HasColumnName("customer_id");
        builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
        builder.Property(e => e.ShopId).HasColumnName("shop_id");
        builder.Property(e => e.LastMessageId).HasColumnName("last_message_id");
        builder.Property(e => e.LastMessageSenderId).HasColumnName("last_message_sender_id");
        builder.Property(e => e.LastMessagePreview).HasMaxLength(160).HasColumnName("last_message_preview");
        builder.Property(e => e.LastActivityAt).HasDefaultValueSql("now()").HasColumnName("last_activity_at");
        builder.Property(e => e.BlockedByUserId).HasColumnName("blocked_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Shop>().WithMany().HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.BlockedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>EF Core configuration for <see cref="ChatMessage"/>.</summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.MapBase("chat_messages_pkey");

        builder.HasIndex(e => new { e.ThreadId, e.CreatedAt, e.Id }, "idx_chat_messages_thread_cursor")
            .IsDescending(false, true, true);
        builder.HasIndex(e => new { e.ThreadId, e.SenderId, e.ClientMessageId }, "uq_chat_messages_client_id")
            .IsUnique();

        builder.Property(e => e.ThreadId).HasColumnName("thread_id");
        builder.Property(e => e.SenderId).HasColumnName("sender_id");
        builder.Property(e => e.SenderRole).HasConversion<string>().HasMaxLength(16).HasColumnName("sender_role");
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(16).HasColumnName("type");
        builder.Property(e => e.Body).HasMaxLength(4000).HasColumnName("body");
        builder.Property(e => e.ClientMessageId).HasColumnName("client_message_id");
        builder.Property(e => e.ProductId).HasColumnName("product_id");
        builder.Property(e => e.ProductName).HasMaxLength(255).HasColumnName("product_name");
        builder.Property(e => e.ProductImageUrl).HasMaxLength(1024).HasColumnName("product_image_url");
        builder.Property(e => e.ProductPrice).HasPrecision(18, 2).HasColumnName("product_price");
        builder.Property(e => e.EditedAt).HasColumnName("edited_at");
        builder.Property(e => e.RecalledAt).HasColumnName("recalled_at");

        builder.HasOne<ChatThread>().WithMany().HasForeignKey(e => e.ThreadId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core configuration for <see cref="ChatAttachment"/>.</summary>
public class ChatAttachmentConfiguration : IEntityTypeConfiguration<ChatAttachment>
{
    public void Configure(EntityTypeBuilder<ChatAttachment> builder)
    {
        builder.ToTable("chat_attachments");
        builder.MapBase("chat_attachments_pkey");

        builder.HasIndex(e => new { e.MessageId, e.SortOrder }, "idx_chat_attachments_message");
        builder.HasIndex(e => new { e.UploaderId, e.MessageId }, "idx_chat_attachments_uploader_pending");

        builder.Property(e => e.UploaderId).HasColumnName("uploader_id");
        builder.Property(e => e.MessageId).HasColumnName("message_id");
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16).HasColumnName("kind");
        builder.Property(e => e.Url).HasMaxLength(1024).HasColumnName("url");
        builder.Property(e => e.PublicId).HasMaxLength(512).HasColumnName("public_id");
        builder.Property(e => e.FileName).HasMaxLength(255).HasColumnName("file_name");
        builder.Property(e => e.MimeType).HasMaxLength(128).HasColumnName("mime_type");
        builder.Property(e => e.SizeBytes).HasColumnName("size_bytes");
        builder.Property(e => e.Width).HasColumnName("width");
        builder.Property(e => e.Height).HasColumnName("height");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UploaderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ChatMessage>().WithMany().HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core configuration for <see cref="ChatParticipantState"/>.</summary>
public class ChatParticipantStateConfiguration : IEntityTypeConfiguration<ChatParticipantState>
{
    public void Configure(EntityTypeBuilder<ChatParticipantState> builder)
    {
        builder.ToTable("chat_participant_states");
        builder.MapBase("chat_participant_states_pkey");

        builder.HasIndex(e => new { e.ThreadId, e.UserId }, "uq_chat_participant_states_thread_user").IsUnique();
        builder.HasIndex(e => new { e.UserId, e.UnreadCount }, "idx_chat_participant_states_user_unread");

        builder.Property(e => e.ThreadId).HasColumnName("thread_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.LastReadMessageId).HasColumnName("last_read_message_id");
        builder.Property(e => e.LastReadAt).HasColumnName("last_read_at");
        builder.Property(e => e.UnreadCount).HasDefaultValue(0).HasColumnName("unread_count");

        builder.HasOne<ChatThread>().WithMany().HasForeignKey(e => e.ThreadId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core configuration for <see cref="UserPresence"/>.</summary>
public class UserPresenceConfiguration : IEntityTypeConfiguration<UserPresence>
{
    public void Configure(EntityTypeBuilder<UserPresence> builder)
    {
        builder.ToTable("user_presences");
        builder.HasKey(e => e.UserId).HasName("user_presences_pkey");

        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(e => e.ShowActiveStatus).HasDefaultValue(true).HasColumnName("show_active_status");

        builder.HasOne<User>().WithOne().HasForeignKey<UserPresence>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

