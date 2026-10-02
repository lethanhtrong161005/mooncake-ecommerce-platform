using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class AiAnalyticsReportConfiguration : IEntityTypeConfiguration<AiAnalyticsReport>
{
    public void Configure(EntityTypeBuilder<AiAnalyticsReport> builder)
    {
        builder.ToTable("ai_analytics_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.ReportType).HasColumnName("report_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").HasColumnType("date");
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").HasColumnType("date");
        builder.Property(x => x.Parameters)
            .HasColumnName("parameters")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(AiReportStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<AiReportStatus>());
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at").HasColumnType("timestamptz(3)");

        builder.HasIndex(x => x.SupplierId).HasDatabaseName("ix_ai_reports_supplier");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_ai_reports_supplier");
    }
}
