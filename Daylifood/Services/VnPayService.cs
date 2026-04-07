using Daylifood.Models;
using Daylifood.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Daylifood.Services;

public class VnPayService : IVnPayService
{
    private readonly VnPayOptions _options;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(IOptions<VnPayOptions> options, ILogger<VnPayService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public string CreatePaymentUrl(Order order, string clientIpAddress)
    {
        if (string.IsNullOrWhiteSpace(_options.TmnCode) ||
            string.IsNullOrWhiteSpace(_options.HashSecret) ||
            string.IsNullOrWhiteSpace(_options.ReturnUrl))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình VnPay: TmnCode, HashSecret hoặc ReturnUrl chưa được điền.");
        }

        var now      = GetVietnamNow();
        var txnRef   = order.Id.ToString(CultureInfo.InvariantCulture);
        var amount   = (long)(order.TotalPrice * 100);
        var orderInfo = $"Thanh toan don hang {order.Id} DayliFood";

        // SortedList — keys sorted ordinal A-Z (VNPay spec)
        var data = new SortedList<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"]    = string.IsNullOrWhiteSpace(_options.Version) ? "2.1.0" : _options.Version,
            ["vnp_Command"]    = "pay",
            ["vnp_TmnCode"]    = _options.TmnCode,
            ["vnp_Amount"]     = amount.ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"]   = "VND",
            ["vnp_IpAddr"]     = string.IsNullOrWhiteSpace(clientIpAddress) ? "127.0.0.1" : clientIpAddress,
            ["vnp_Locale"]     = string.IsNullOrWhiteSpace(_options.Locale) ? "vn" : _options.Locale,
            ["vnp_OrderInfo"]  = orderInfo,
            ["vnp_OrderType"]  = "other",
            ["vnp_ReturnUrl"]  = _options.ReturnUrl,
            ["vnp_TxnRef"]     = txnRef,
        };

        // ── Chữ ký: ký trên raw (chưa encode) key=value, nối bằng & ──────────
        // Đây là chuẩn VNPay: https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html
        var rawSignString = BuildRawString(data);
        var signature     = HmacSha512(rawSignString, _options.HashSecret);

        _logger.LogWarning("[VNPay-DEBUG] raw sign: {Raw}", rawSignString);
        _logger.LogWarning("[VNPay-DEBUG] signature: {Sig}", signature);
        _logger.LogWarning("[VNPay-DEBUG] TmnCode: {Code} | HashSecret len: {Len}", _options.TmnCode, _options.HashSecret?.Length ?? 0);

        // ── URL: encode từng value ────────────────────────────────────────────
        var queryBuilder = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (queryBuilder.Length > 0) queryBuilder.Append('&');
            queryBuilder.Append(WebUtility.UrlEncode(key));
            queryBuilder.Append('=');
            queryBuilder.Append(WebUtility.UrlEncode(value));
        }
        queryBuilder.Append("&vnp_SecureHash=");
        queryBuilder.Append(signature);

        return $"{_options.BaseUrl}?{queryBuilder}";
    }

    public VnPayCallbackResult ParseCallback(IQueryCollection query)
    {
        // Thu thập params từ query, ASP.NET đã URL-decode sẵn
        var inputData   = new SortedList<string, string>(StringComparer.Ordinal);
        var secureHash  = query["vnp_SecureHash"].ToString();

        foreach (var pair in query)
        {
            if (!pair.Key.StartsWith("vnp_", StringComparison.Ordinal)) continue;
            if (pair.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) continue;
            if (pair.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(pair.Value)) continue;

            inputData[pair.Key] = pair.Value.ToString();
        }

        // Ký trên raw values (đã decode từ query) — giống lúc tạo URL
        var rawSignString = BuildRawString(inputData);
        var computedHash  = HmacSha512(rawSignString, _options.HashSecret ?? string.Empty);

        var txnRef            = inputData.TryGetValue("vnp_TxnRef",           out var tr) ? tr : string.Empty;
        var amountRaw         = inputData.TryGetValue("vnp_Amount",            out var av) ? av : "0";
        var responseCode      = inputData.TryGetValue("vnp_ResponseCode",      out var rc) ? rc : string.Empty;
        var transactionStatus = inputData.TryGetValue("vnp_TransactionStatus", out var ts) ? ts : string.Empty;
        var transactionNo     = inputData.TryGetValue("vnp_TransactionNo",     out var tn) ? tn : string.Empty;
        var bankCode          = inputData.TryGetValue("vnp_BankCode",          out var bc) ? bc : string.Empty;

        _ = long.TryParse(amountRaw, out var amount);
        _ = int.TryParse(txnRef, out var orderId);

        var isValid = !string.IsNullOrWhiteSpace(secureHash)
                      && string.Equals(computedHash, secureHash, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("VNPay callback — orderId={Id} code={Code} sigOk={Ok}",
            orderId, responseCode, isValid);

        return new VnPayCallbackResult
        {
            IsSignatureValid  = isValid,
            IsSuccess         = responseCode == "00"
                                && (string.IsNullOrWhiteSpace(transactionStatus) || transactionStatus == "00"),
            OrderId           = orderId,
            Amount            = amount,
            ResponseCode      = responseCode,
            TransactionStatus = transactionStatus,
            TransactionNo     = transactionNo,
            BankCode          = bankCode
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Build raw sign string theo chuẩn VNPay: raw key=raw value nối bằng &
    /// KHÔNG URL-encode (đây là điểm khác biệt so với query string gửi đi)
    /// </summary>
    private static string BuildRawString(SortedList<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
        }
        return sb.ToString();
    }

    /// <summary>HMAC-SHA512, lowercase hex — theo chuẩn VNPay</summary>
    private static string HmacSha512(string data, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
    }

    private static DateTime GetVietnamNow()
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")); }
        catch
        {
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); }
            catch { return DateTime.UtcNow.AddHours(7); }
        }
    }
}
