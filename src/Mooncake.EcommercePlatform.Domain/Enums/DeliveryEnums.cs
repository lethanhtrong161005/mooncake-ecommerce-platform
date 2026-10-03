namespace Mooncake.EcommercePlatform.Domain.Enums;

public enum DeliveryDirection { SupplierToCustomer, CustomerToSupplier }

public enum DeliveryStatus { Pending, InTransit, Delivered, Failed, Returned }

public enum ProofType { Pickup, Delivery }
