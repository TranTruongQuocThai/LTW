namespace Daylifood.Models;

public class VnPayCallbackResult
{
    public bool IsSignatureValid { get; set; }
    public bool IsSuccess { get; set; }
    public int OrderId { get; set; }
    public long Amount { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
}
