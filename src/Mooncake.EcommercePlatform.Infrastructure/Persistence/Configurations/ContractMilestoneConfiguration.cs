using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ContractMilestoneConfiguration : IEntityTypeConfiguration<ContractMilestone>
{
    public void Configure(EntityTypeBuilder<ContractMilestone> builder)
    {
        builder.ToTable("contract_milestones");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ContractId).HasColumnName("contract_id").IsRequired();
        builder.Property(x => x.MilestoneNo).HasColumnName("milestone_no").HasColumnType("smallint").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.MilestoneType)
            .HasColumnName("milestone_type")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(MilestoneType.Progress)
            .HasConversion(new SnakeCaseEnumConverter<MilestoneType>());
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").HasColumnType("date");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(MilestoneStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<MilestoneStatus>());
        builder.Property(x => x.PaidAtUtc).HasColumnName("paid_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.ContractId, x.MilestoneNo }).IsUnique().HasDatabaseName("uq_milestone_no");
        builder.HasIndex(x => x.ContractId).IsUnique().HasDatabaseName("uq_one_deposit_per_contract").HasFilter("milestone_type = 'deposit'");

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.ContractMilestones)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_milestones_contract");
    }
}
