using Microsoft.AspNetCore.Http;

namespace Daylifood.Services;

/// <summary>Kết quả từ chatbot: text trả lời + danh sách sản phẩm gợi ý (nếu có).</summary>
public sealed record ChatbotResponse(
    string Text,
    IReadOnlyList<ProductSuggestion> Products
);

/// <summary>Sản phẩm chatbot muốn gợi ý trong bubble.</summary>
public sealed record ProductSuggestion(
    int Id,
    string Name,
    decimal Price,
    string StoreName,
    string? ImageUrl
);

public interface IChatbotService
{
    Task<ChatbotResponse> AskAsync(string message, string websiteContext, CancellationToken cancellationToken = default);
}
