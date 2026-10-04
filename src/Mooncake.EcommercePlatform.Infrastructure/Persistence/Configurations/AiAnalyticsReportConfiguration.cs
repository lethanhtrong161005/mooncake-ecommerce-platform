namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="AiAnalyticsReport"/>.</summary>
public class AiAnalyticsReportConfiguration : IEntityTypeConfiguration<AiAnalyticsReport>
{
    public void Configure(EntityTypeBuilder<AiAnalyticsReport> builder)
    {
            builder.HasKey(e => e.Id).HasName("ai_analytics_reports_pkey");

            builder.ToTable("ai_analytics_reports");

            builder.HasIndex(e => e.RequestedBy, "idx_aar_requested_by").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Parameters)
                .HasColumnType("jsonb")
                .HasColumnName("parameters");
            builder.Property(e => e.RequestedBy).HasColumnName("requested_by");
            builder.Property(e => e.ResultData)
                .HasColumnType("jsonb")
                .HasColumnName("result_data");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.ReportType).HasColumnName("report_type");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_aar_status");
        builder.HasIndex(e => e.ReportType, "idx_aar_report_type");
    }
}
