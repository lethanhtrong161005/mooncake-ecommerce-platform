namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>English API and domain messages for shipment and Proof-of-Delivery workflows.</summary>
public static class DeliveryMessages
{
    public const string DeliveriesRetrieved = "Deliveries retrieved successfully.";
    public const string DeliveryRetrieved = "Delivery details retrieved successfully.";
    public const string ContractDeliveriesRetrieved = "Contract deliveries retrieved successfully.";
    public const string OrderDeliveriesRetrieved = "Order deliveries retrieved successfully.";
    public const string DeliveryCreated = "Delivery created successfully.";
    public const string DeliveryStatusUpdated = "Delivery status updated successfully.";
    public const string CarrierInfoUpdated = "Delivery carrier information updated successfully.";
    public const string ProofSubmitted = "Proof of delivery submitted successfully.";
    public const string ProofsRetrieved = "Delivery proofs retrieved successfully.";
    public const string ProofRetrieved = "Delivery proof details retrieved successfully.";
    public const string ProofVerified = "Delivery proof integrity verified successfully.";
    public const string ValidSessionRequired = "A valid authenticated user session is required.";

    public const string DeliveryNotFound = "Delivery record was not found.";
    public const string ProofNotFound = "Proof of delivery record was not found.";
    public const string AccessDenied = "You do not have permission to access this delivery.";
    public const string ManageDenied = "You do not have permission to manage this delivery.";
    public const string ProofSubmitDenied = "You do not have permission to submit proof for this delivery.";
    public const string TargetLinkInvalid = "A delivery must be linked to exactly one contract or one order.";
    public const string FromAddressRequired = "Dispatch origin address is required.";
    public const string ToAddressRequired = "Destination delivery address is required.";
    public const string RecipientNameRequired = "Recipient name is required.";
    public const string RecipientPhoneInvalid = "Recipient phone number must be a valid 10-digit Vietnamese mobile number.";
    public const string ScheduledDateInPast = "Scheduled delivery date cannot be in the past.";
    public const string ScheduledDateRequired = "A scheduled delivery date is required.";
    public const string ScheduledDateTooSoon = "Scheduled delivery date must be at least 24 hours after the shipment is created.";
    public const string SingleDeliveryLimitExceeded = "Direct bulk orders support only a single active shipment in v1.";
    public const string ProofRequiredBeforeDelivered = "A delivery proof photo is required before marking the shipment as delivered.";
    public const string DeliveryAlreadyCompleted = "This delivery has already been completed.";
    public const string LatitudeInvalid = "Latitude must be within [-90, 90].";
    public const string LongitudeInvalid = "Longitude must be within [-180, 180].";
    public const string PhotoRequired = "On-site delivery photo evidence is required.";
    public const string SignatureRequiredForContract = "Recipient signature is required when submitting contract delivery proof.";
    public const string DefaultDriverLabel = "Delivery Driver";
    public const string ShareLinkRetrieved = "Delivery PoD share link status retrieved successfully.";
    public const string ShareLinkCreated = "Delivery PoD share link created successfully.";
    public const string ShareLinkRevoked = "Delivery PoD share link revoked successfully.";
    public const string ShareLinkTtlInvalid = "Share link expiration must be between 1 and 168 hours.";
    public const string ShareLinkNotFound = "Delivery PoD share link is invalid or has been replaced.";
    public const string ShareLinkRevokedError = "This delivery PoD share link has been revoked by the supplier.";
    public const string ShareLinkExpiredError = "This delivery PoD share link has expired. Please request a new link from the supplier.";
    public const string DeliveryCannotAcceptShareLink = "Completed or returned deliveries cannot generate a new driver PoD link.";
    public const string DriverPhotoUploaded = "Driver PoD photo uploaded successfully.";
}

