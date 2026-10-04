namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="DeliveryProof"/>.</summary>
public class DeliveryProofConfiguration : IEntityTypeConfiguration<DeliveryProof>
{
    public void Configure(EntityTypeBuilder<DeliveryProof> builder)
    {
            builder.HasKey(e => e.Id).HasName("delivery_proofs_pkey");

            builder.ToTable("delivery_proofs");

            builder.HasIndex(e => e.DeliveryId, "idx_dp_delivery_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.DeliveryId).HasColumnName("delivery_id");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.LocationLat)
                .HasPrecision(10, 8)
                .HasColumnName("location_lat");
            builder.Property(e => e.LocationLng)
                .HasPrecision(11, 8)
                .HasColumnName("location_lng");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            builder.Property(e => e.SignatureUrl).HasColumnName("signature_url");
            builder.Property(e => e.TakenAt).HasColumnName("taken_at");
            builder.Property(e => e.TakenBy)
                .HasMaxLength(255)
                .HasColumnName("taken_by");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.ProofType).HasColumnName("proof_type");
        builder.HasIndex(e => e.ProofType, "idx_dp_proof_type");
    }
}
