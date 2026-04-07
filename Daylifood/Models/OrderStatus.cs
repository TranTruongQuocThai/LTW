namespace Daylifood.Models;

public enum OrderStatus
{
    Pending = 0,
    Shipping = 1,
    Delivered = 2,
    Cancelled = 3,
    Confirmed = 4,
    AwaitingPayment = 5
}
