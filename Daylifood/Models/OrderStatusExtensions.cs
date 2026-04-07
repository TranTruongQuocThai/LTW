namespace Daylifood.Models;

public static class OrderStatusExtensions
{
    public static string ToVietnamese(this OrderStatus s) => s switch
    {
        OrderStatus.AwaitingPayment => "Chờ thanh toán VNPay",
        OrderStatus.Pending => "Chờ chủ quán xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận — chờ shipper",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };
}
