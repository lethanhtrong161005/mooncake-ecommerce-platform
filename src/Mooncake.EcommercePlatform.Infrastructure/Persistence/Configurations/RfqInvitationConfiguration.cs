namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="RfqInvitation"/>.</summary>
public class RfqInvitationConfiguration : IEntityTypeConfiguration<RfqInvitation>
{
    public void Configure(EntityTypeBuilder<RfqInvitation> builder)
    {
            builder.HasKey(e => e.Id).HasName("rfq_invitations_pkey");

            builder.ToTable("rfq_invitations");

            builder.HasIndex(e => e.RfqId, "idx_rfqinv_rfq_id");

            builder.HasIndex(e => e.SupplierId, "idx_rfqinv_supplier_id");

            builder.HasIndex(e => new { e.RfqId, e.SupplierId }, "rfq_invitations_rfq_id_supplier_id_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.InvitedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("invited_at");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.RespondedAt).HasColumnName("responded_at");
            builder.Property(e => e.RfqId).HasColumnName("rfq_id");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_rfqinv_status");
    }
}
