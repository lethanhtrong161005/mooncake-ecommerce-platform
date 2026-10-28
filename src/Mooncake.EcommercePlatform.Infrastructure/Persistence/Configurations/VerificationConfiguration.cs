namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Schema expansion for immutable submissions, delivery and command deduplication.</summary>
public static class VerificationConfiguration
{
    public static void ConfigureVerification(this ModelBuilder model)
    {
        var profile = model.Entity<SupplierProfile>();
        profile.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        profile.Property(x => x.CurrentSubmissionId).HasColumnName("current_submission_id");
        profile.Property(x => x.LastApprovedSubmissionId).HasColumnName("last_approved_submission_id");
        profile.Property(x => x.LastSubmittedAtUtc).HasColumnName("last_submitted_at_utc");
        profile.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        profile.Property(x => x.RevokedByUserId).HasColumnName("revoked_by_user_id");
        profile.Property(x => x.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(1000);
        profile.HasIndex(x => new { x.VerificationStatus, x.LastSubmittedAtUtc, x.Id })
            .HasFilter("is_deleted = false AND current_submission_id IS NOT NULL");
        var submission = model.Entity<SupplierVerificationSubmission>();
        ConfigureNames(submission, "supplier_verification_submissions");
        submission.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        submission.Property(x => x.CompanyName).HasMaxLength(255);
        submission.Property(x => x.TaxCode).HasMaxLength(50);
        submission.Property(x => x.Address).HasMaxLength(500);
        submission.Property(x => x.Description).HasMaxLength(2000);
        submission.Property(x => x.RejectionReason).HasMaxLength(1000);
        submission.HasIndex(x => new { x.SupplierProfileId, x.Revision }).IsUnique();
        submission.HasIndex(x => x.SupplierProfileId).IsUnique().HasFilter("status = 'Pending' AND is_deleted = false");
        submission.HasOne<SupplierProfile>().WithMany().HasForeignKey(x => x.SupplierProfileId).OnDelete(DeleteBehavior.Restrict);
        var doc = model.Entity<SupplierVerificationDocument>();
        doc.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        doc.Property(x => x.SubmissionId).HasColumnName("submission_id");
        doc.Property(x => x.UploadStatus).HasColumnName("upload_status").HasMaxLength(30);
        doc.Property(x => x.UploadOperationId).HasColumnName("upload_operation_id");
        doc.Property(x => x.UploadRequestHash).HasColumnName("upload_request_hash").HasMaxLength(64);
        doc.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64);
        doc.Property(x => x.ProviderPublicId).HasColumnName("provider_public_id").HasMaxLength(1000);
        doc.Property(x => x.ProviderResourceType).HasColumnName("provider_resource_type").HasMaxLength(20);
        doc.Property(x => x.ProviderDeliveryType).HasColumnName("provider_delivery_type").HasMaxLength(30);
        doc.Property(x => x.ValidationCompletedAtUtc).HasColumnName("validation_completed_at_utc");
        doc.HasIndex(x => new { x.OwnerUserId, x.UploadOperationId }).IsUnique();
        doc.HasIndex(x => x.SubmissionId);
        doc.HasOne<SupplierVerificationSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        var outbox = model.Entity<VerificationOutboxMessage>();
        ConfigureNames(outbox, "verification_outbox_messages");
        outbox.HasIndex(x => new { x.EventId, x.Channel }).IsUnique();
        outbox.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        var key = model.Entity<VerificationIdempotencyRecord>();
        ConfigureNames(key, "verification_idempotency_records");
        key.HasIndex(x => new { x.ActorUserId, x.Operation, x.ResourceId, x.Key }).IsUnique();
        key.Property(x => x.RequestHash).HasMaxLength(64);
        key.Property(x => x.Operation).HasMaxLength(100);
        model.Entity<User>().Property(x => x.NotificationSequence).HasColumnName("notification_sequence");
        model.Entity<User>().Property(x => x.NotificationSequence).Metadata
            .SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        var notification = model.Entity<Notification>();
        notification.Property(x => x.SourceEventId).HasColumnName("source_event_id");
        notification.Property(x => x.AggregateVersion).HasColumnName("aggregate_version");
        notification.Property(x => x.AudienceKind).HasColumnName("audience_kind").HasMaxLength(20);
        notification.Property(x => x.Sequence).HasColumnName("sequence");
        notification.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc");
        notification.HasIndex(x => new { x.UserId, x.SourceEventId, x.Type }).IsUnique().HasFilter("source_event_id IS NOT NULL");
        notification.HasIndex(x => new { x.UserId, x.Sequence }).IsUnique().HasFilter("sequence IS NOT NULL");
    }

    private static void ConfigureNames<T>(EntityTypeBuilder<T> builder, string table) where T : class
    {
        builder.ToTable(table);
        foreach (var property in builder.Metadata.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
