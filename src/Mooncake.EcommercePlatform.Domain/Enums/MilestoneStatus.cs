namespace Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Enum representing milestone_status.</summary>
public enum MilestoneStatus
{
    Pending,
    AwaitingPayment,
    Paid,
    InProgress,
    PendingReview,
    CompletedOnTime,
    CompletedLate,
    Overdue,
    Failed
}
