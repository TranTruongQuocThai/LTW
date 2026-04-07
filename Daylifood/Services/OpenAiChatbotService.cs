using Daylifood.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Daylifood.Services;

public sealed class OpenAiChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatbotService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenAiChatbotService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatbotService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatbotResponse> AskAsync(
        string message,
        string websiteContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Chưa cấu hình OpenAI:ApiKey trong appsettings.");

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model;

        var systemPrompt = string.IsNullOrWhiteSpace(_options.SystemPrompt)
            ? "Bạn là trợ lý chăm sóc khách hàng của DayliFood. Trả lời tiếng Việt, ngắn gọn, hữu ích."
            : _options.SystemPrompt;

        // Gọi chat/completions API — chuẩn OpenAI, tương thích mọi key
        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt + "\n\n" + websiteContext },
                new { role = "user",   content = message }
            },
            temperature = 0.7,
            max_tokens  = 600
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API lỗi: {StatusCode} — {Body}", response.StatusCode, json);
            throw new InvalidOperationException("OpenAI API từ chối yêu cầu. Kiểm tra API key và billing.");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var text = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        text = text.Trim();

        // Trích product IDs từ text nếu AI mention theo pattern [PRODUCT:id]
        var products = ExtractProductSuggestions(text, websiteContext);

        return new ChatbotResponse(text, products);
    }

    /// <summary>
    /// Tìm các product được AI đề cập theo pattern [PRODUCT:123] trong text.
    /// Format này được nhúng vào websiteContext khi build prompt.
    /// </summary>
    private static IReadOnlyList<ProductSuggestion> ExtractProductSuggestions(
        string text,
        string websiteContext)
    {
        // AI được prompt trả lời dạng: "Bạn nên thử **Bò lúc lắc** [PRODUCT:12]"
        var ids = Regex.Matches(text, @"\[PRODUCT:(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return Array.Empty<ProductSuggestion>();

        // Parse lại từ websiteContext để lấy thông tin sản phẩm
        // Format trong context: "- {name} | {storeName} | {price} đ [PRODUCT:{id}]"
        var suggestions = new List<ProductSuggestion>();
        foreach (var id in ids)
        {
            var pattern = new Regex($@"- (.+?) \| (.+?) \| ([\d,]+) đ \[PRODUCT:{id}\]");
            var match   = pattern.Match(websiteContext);
            if (!match.Success)
                continue;

            _ = decimal.TryParse(
                match.Groups[3].Value.Replace(",", string.Empty),
                out var price);

            suggestions.Add(new ProductSuggestion(
                Id:        id,
                Name:      match.Groups[1].Value.Trim(),
                Price:     price,
                StoreName: match.Groups[2].Value.Trim(),
                ImageUrl:  null
            ));
        }

        return suggestions;
    }
}
