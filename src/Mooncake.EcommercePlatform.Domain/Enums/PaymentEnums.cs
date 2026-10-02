namespace Mooncake.EcommercePlatform.Domain.Enums;

public enum PaymentType { Deposit, Balance, Full, Refund }

public enum PaymentMethod { BankTransfer, CreditCard, EWallet, Cod }

public enum PaymentStatus { Pending, Succeeded, Failed, Refunded, Cancelled }
