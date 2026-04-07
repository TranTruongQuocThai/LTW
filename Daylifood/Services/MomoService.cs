using Daylifood.Models;
using Daylifood.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Daylifood.Services;

public sealed class MomoService : IMomoService
{
    private readonly MomoOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MomoService> _logger;

    public MomoService(
        IOptions<MomoOptions> options,
        HttpClient httpClient,
        ILogger<MomoService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> CreatePaymentUrlAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKey) ||
            string.IsNullOrWhiteSpace(_options.SecretKey) ||
            string.IsNullOrWhiteSpace(_options.ReturnUrl))
        {
            throw new InvalidOperationException("Thiếu cấu hình Momo: AccessKey, SecretKey hoặc ReturnUrl.");
        }

        var partnerCode = _options.PartnerCode;
        var requestId   = Guid.NewGuid().ToString();
        var orderId     = $"DL{order.Id}_{DateTime.UtcNow.Ticks}"; // unique per request
        var amount      = (long)Math.Round(order.TotalPrice, 0);
        var orderInfo   = $"Thanh toan don hang #{order.Id} - DayliFood";
        var redirectUrl = _options.ReturnUrl;
        // ipnUrl phải là URL public. Khi test local để trống hoặc dùng ngrok.
        // Momo yêu cầu ipnUrl không được trống với một số requestType.
        var ipnUrl      = string.IsNullOrWhiteSpace(_options.IpnUrl)
                          ? redirectUrl  // fallback khi test local
                          : _options.IpnUrl;
        var requestType = _options.RequestType; // captureMoMoWallet hoặc payWithATM
        var extraData   = string.Empty;
        var lang        = "vi";

        // ── Signature đúng theo Momo v2 spec ────────────────────────────────
        // Chỉ ký các field sau, đúng thứ tự alphabetical
        var rawSignature =
            $"accessKey={_options.AccessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={partnerCode}" +
            $"&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";

        var signature = ComputeHmacSha256(rawSignature, _options.SecretKey);

        _logger.LogDebug("Momo raw signature: {Raw}", rawSignature);

        // ── Payload gửi đến Momo ────────────────────────────────────────────
        var payloadObj = new Dictionary<string, object>
        {
            ["partnerCode"] = partnerCode,
            ["partnerName"] = "DayliFood",
            ["storeId"]     = "DayliFood",
            ["requestId"]   = requestId,
            ["amount"]      = amount,
            ["orderId"]     = orderId,
            ["orderInfo"]   = orderInfo,
            ["redirectUrl"] = redirectUrl,
            ["ipnUrl"]      = ipnUrl,
            ["lang"]        = lang,
            ["extraData"]   = extraData,
            ["requestType"] = requestType,
            ["signature"]   = signature
        };

        var json = JsonSerializer.Serialize(payloadObj);
        _logger.LogDebug("Momo request payload: {Json}", json);

        using var content  = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(_options.PaymentUrl, content, cancellationToken);
        var responseBody   = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Momo response: {Status} — {Body}", response.StatusCode, responseBody);

        // Momo trả về 200 ngay cả khi lỗi logic, parse resultCode
        using var doc  = JsonDocument.Parse(responseBody);
        var root       = doc.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;

        if (resultCode != 0)
        {
            var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Không rõ lỗi";
            _logger.LogError("Momo resultCode={Code}: {Message} | Raw: {Body}", resultCode, message, responseBody);
            throw new InvalidOperationException($"Momo từ chối thanh toán (mã {resultCode}): {message}");
        }

        if (!root.TryGetProperty("payUrl", out var payUrlEl) ||
            string.IsNullOrWhiteSpace(payUrlEl.GetString()))
        {
            throw new InvalidOperationException("Momo không trả về payUrl.");
        }

        return payUrlEl.GetString()!;
    }

    public MomoCallbackResult ParseCallback(IQueryCollection query)
    {
        var partnerCode  = query["partnerCode"].ToString();
        var orderId      = query["orderId"].ToString();
        var requestId    = query["requestId"].ToString();
        var amount       = query["amount"].ToString();
        var orderInfo    = query["orderInfo"].ToString();
        var orderType    = query["orderType"].ToString();
        var transId      = query["transId"].ToString();
        var resultCode   = query["resultCode"].ToString();
        var message      = query["message"].ToString();
        var payType      = query["payType"].ToString();
        var responseTime = query["responseTime"].ToString();
        var extraData    = query["extraData"].ToString();
        var receivedSig  = query["signature"].ToString();

        // Momo v2 return/IPN signature verification
        var rawToVerify =
            $"accessKey={_options.AccessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&message={message}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&orderType={orderType}" +
            $"&partnerCode={partnerCode}" +
            $"&payType={payType}" +
            $"&requestId={requestId}" +
            $"&responseTime={responseTime}" +
            $"&resultCode={resultCode}" +
            $"&transId={transId}";

        var computedSig = ComputeHmacSha256(rawToVerify, _options.SecretKey);

        // orderId mình tạo dạng "DL{dbId}_{ticks}" — parse lấy dbId
        var dbOrderId = ParseDbOrderId(orderId);

        _ = long.TryParse(amount, out var parsedAmount);
        _ = int.TryParse(resultCode, out var parsedResultCode);

        return new MomoCallbackResult(
            IsSignatureValid: string.Equals(computedSig, receivedSig, StringComparison.OrdinalIgnoreCase),
            IsSuccess:        parsedResultCode == 0,
            OrderId:          dbOrderId,
            Amount:           parsedAmount,
            ResultCode:       resultCode,
            Message:          message,
            TransactionId:    transId,
            OrderInfo:        orderInfo
        );
    }

    /// <summary>Parse DB order Id từ orderId Momo dạng "DL{id}_{ticks}".</summary>
    private static int ParseDbOrderId(string momoOrderId)
    {
        if (string.IsNullOrWhiteSpace(momoOrderId))
            return 0;

        // Format: DL3_638XXXXXX
        if (momoOrderId.StartsWith("DL", StringComparison.Ordinal))
        {
            var withoutPrefix = momoOrderId[2..]; // "3_638..."
            var underscoreIdx = withoutPrefix.IndexOf('_');
            var idPart = underscoreIdx > 0
                ? withoutPrefix[..underscoreIdx]
                : withoutPrefix;
            _ = int.TryParse(idPart, out var id);
            return id;
        }

        _ = int.TryParse(momoOrderId, out var fallback);
        return fallback;
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
    }
}
