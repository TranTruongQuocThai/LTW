namespace Daylifood.Models;

public static class PaymentExtensions
{
    public static string ToVietnamese(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod   => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.VnPay => "Thanh toán qua VNPay",
        PaymentMethod.Momo  => "Thanh toán qua Momo",
        _                   => method.ToString()
    };

    public static string ToVietnamese(this PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid  => "Chưa thanh toán",
        PaymentStatus.Pending => "Đang chờ xác nhận thanh toán",
        PaymentStatus.Paid    => "Đã thanh toán",
        PaymentStatus.Failed  => "Thanh toán thất bại",
        _                     => status.ToString()
    };
}
