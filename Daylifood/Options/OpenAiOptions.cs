namespace Daylifood.Options;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "gpt-4o-mini";
    public string SystemPrompt { get; set; } = "Bạn là trợ lý chăm sóc khách hàng của DayliFood. Trả lời bằng tiếng Việt tự nhiên, ngắn gọn, hữu ích. Ưu tiên hướng dẫn người dùng tìm món, xem quán, thêm giỏ hàng, checkout, theo dõi đơn và giải thích phương thức thanh toán. Không bịa thông tin ngoài dữ liệu website được cung cấp. Nếu thiếu dữ liệu, hãy nói rõ và hướng người dùng liên hệ quản trị viên.";
}
