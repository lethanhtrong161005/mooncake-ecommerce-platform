namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Stable messages for bulk ordering, pricing, and custom packaging workflows.</summary>
public static class B2BOrderingMessages
{
    public const string QuoteCalculated = "Quote preview calculated successfully.";
    public const string PackagingCreated = "Custom packaging request created successfully.";
    public const string PackagingLoaded = "Custom packaging details retrieved successfully.";
    public const string PackagingRevisionSubmitted = "Packaging mockup revision submitted successfully.";
    public const string PackagingRevisionReviewed = "Packaging mockup revision reviewed successfully.";
    public const string PackagingNotFound = "Custom packaging request was not found.";
    public const string PackagingAlreadyExists = "Custom packaging is already registered for this order.";
    public const string PackagingOrderStatusInvalid = "Custom packaging can only be added to pending or confirmed orders.";
    public const string PackagingRevisionLocked = "A new revision cannot be submitted after the design is approved.";
    public const string PackagingRevisionNotFound = "Packaging revision was not found.";
    public const string PackagingRevisionStatusInvalid = "Only a design under review can be reviewed.";
    public const string PackagingRevisionNotesRequired = "Revision notes must contain at least 10 characters.";
    public const string PackagingProductionBlocked = "Custom packaging design must be approved before production can start.";
    public const string OrderNotFound = "Order was not found.";
    public const string PromotionRangeOverlap = "The quantity range overlaps with an existing active promotion rule.";
}
