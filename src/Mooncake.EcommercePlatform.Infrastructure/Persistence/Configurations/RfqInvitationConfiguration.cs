using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class RfqInvitationConfiguration : IEntityTypeConfiguration<RfqInvitation>
{
    public void Configure(EntityTypeBuilder<RfqInvitation> builder)
    {
        builder.ToTable("rfq_invitations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.RfqId).HasColumnName("rfq_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(RfqInvitationStatus.Invited)
            .HasConversion(new SnakeCaseEnumConverter<RfqInvitationStatus>());
        builder.Property(x => x.InvitedAtUtc).HasColumnName("invited_at").HasColumnType("timestamptz(3)").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.RespondedAtUtc).HasColumnName("responded_at").HasColumnType("timestamptz(3)");

        builder.HasIndex(x => new { x.RfqId, x.SupplierId }).IsUnique().HasDatabaseName("uq_rfq_invitations");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("ix_rfq_invitations_supplier");

        builder.HasOne(x => x.RequestForQuotation)
            .WithMany(x => x.RfqInvitations)
            .HasForeignKey(x => x.RfqId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_rfq_inv_rfq");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_rfq_inv_supplier");
    }
}
