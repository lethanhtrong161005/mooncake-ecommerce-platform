namespace Mooncake.EcommercePlatform.Domain.Enums;

public enum OrderStatus
{
    Pending,
    AwaitingDeposit,
    Confirmed,
    Preparing,
    Shipping,
    Delivered,
    Completed,
    Cancelled
}
