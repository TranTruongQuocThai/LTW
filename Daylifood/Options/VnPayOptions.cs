namespace Daylifood.Options;

public class VnPayOptions
{
    public const string SectionName = "VnPay";

    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string Locale { get; set; } = "vn";
    public string Version { get; set; } = "2.1.0";
}
