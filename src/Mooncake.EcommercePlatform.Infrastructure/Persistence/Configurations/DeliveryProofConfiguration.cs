using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class DeliveryProofConfiguration : IEntityTypeConfiguration<DeliveryProof>
{
    public void Configure(EntityTypeBuilder<DeliveryProof> builder)
    {
        builder.ToTable("delivery_proofs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.DeliveryId).HasColumnName("delivery_id").IsRequired();
        builder.Property(x => x.ProofType)
            .HasColumnName("proof_type")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<ProofType>());
        builder.Property(x => x.PhotoUrl).HasColumnName("photo_url").IsRequired().HasMaxLength(1000);
        builder.Property(x => x.TakenByUserId).HasColumnName("taken_by_user_id");
        builder.Property(x => x.TakenAtUtc)
            .HasColumnName("taken_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("decimal(9,6)");
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("decimal(9,6)");
        builder.Property(x => x.Note).HasColumnName("note");

        builder.HasIndex(x => x.DeliveryId).HasDatabaseName("ix_delivery_proofs_delivery");

        builder.HasOne(x => x.Delivery)
            .WithMany(x => x.DeliveryProofs)
            .HasForeignKey(x => x.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_proofs_delivery");

        builder.HasOne(x => x.TakenByUser)
            .WithMany()
            .HasForeignKey(x => x.TakenByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_proofs_user");
    }
}
