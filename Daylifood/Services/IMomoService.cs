namespace Daylifood.Services;

public interface IMomoService
{
    /// <summary>Tạo URL thanh toán Momo và trả về payUrl để redirect khách.</summary>
    Task<string> CreatePaymentUrlAsync(Daylifood.Models.Order order, CancellationToken cancellationToken = default);

    /// <summary>Parse query string IPN/Return từ Momo callback.</summary>
    MomoCallbackResult ParseCallback(Microsoft.AspNetCore.Http.IQueryCollection query);
}

public sealed record MomoCallbackResult(
    bool IsSignatureValid,
    bool IsSuccess,
    int OrderId,
    long Amount,
    string ResultCode,
    string Message,
    string TransactionId,
    string OrderInfo
);
