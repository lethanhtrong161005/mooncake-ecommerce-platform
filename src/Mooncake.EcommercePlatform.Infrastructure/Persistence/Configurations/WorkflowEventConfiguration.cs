namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="WorkflowEvent"/>.</summary>
public class WorkflowEventConfiguration : IEntityTypeConfiguration<WorkflowEvent>
{
    public void Configure(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.ToTable("workflow_events");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.AggregateType, e.AggregateId, e.CreatedAt });
        builder.HasIndex(e => e.ActorUserId);
        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.AggregateType).HasMaxLength(80).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(80).IsRequired();
        builder.Property(e => e.FromStatus).HasMaxLength(40);
        builder.Property(e => e.ToStatus).HasMaxLength(40);
        builder.Property(e => e.Metadata).HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
