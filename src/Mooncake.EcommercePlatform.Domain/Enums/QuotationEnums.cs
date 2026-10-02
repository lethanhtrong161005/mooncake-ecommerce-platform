namespace Mooncake.EcommercePlatform.Domain.Enums;

public enum QuotationStatus { Submitted, Negotiating, Accepted, Rejected, Withdrawn, Expired }

public enum NegotiationProposedBy { Customer, Supplier }

public enum NegotiationStatus { Pending, Accepted, Rejected, Superseded }
