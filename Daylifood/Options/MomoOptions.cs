namespace Daylifood.Options;

public sealed class MomoOptions
{
    public const string SectionName = "Momo";

    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// captureMoMoWallet (ví Momo) hoặc payWithATM (ATM nội địa).
    /// Mặc định: captureMoMoWallet theo tài liệu MOMOPROJECT.
    /// </summary>
    public string RequestType { get; set; } = "captureWallet";

    /// <summary>API v2 — khuyên dùng</summary>
    public string PaymentUrl { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    /// <summary>API v1 legacy — gw_payment/transactionProcessor</summary>
    public string LegacyApiUrl { get; set; } = "https://test-payment.momo.vn/gw_payment/transactionProcessor";

    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
}
