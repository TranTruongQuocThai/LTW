using Daylifood.Data;
using Daylifood.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace Daylifood.Controllers;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        ApplicationDbContext db,
        IChatbotService chatbotService,
        ILogger<ChatbotController> logger)
    {
        _db = db;
        _chatbotService = chatbotService;
        _logger = logger;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatbotRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Bạn chưa nhập câu hỏi." });

        if (request.Message.Length > 1000)
            return BadRequest(new { message = "Câu hỏi quá dài, vui lòng rút gọn dưới 1000 ký tự." });

        try
        {
            var (websiteContext, productMap) = await BuildWebsiteContextAsync();
            var result = await _chatbotService.AskAsync(
                request.Message.Trim(), websiteContext, HttpContext.RequestAborted);

            // Xóa tag [PRODUCT:id] khỏi text hiển thị cho người dùng
            var cleanText = Regex.Replace(result.Text, @"\s*\[PRODUCT:\d+\]", string.Empty).Trim();

            // Enrich product suggestions với imageUrl từ productMap
            var enriched = result.Products
                .Select(p => new
                {
                    id        = p.Id,
                    name      = p.Name,
                    price     = p.Price,
                    storeName = p.StoreName,
                    imageUrl  = productMap.TryGetValue(p.Id, out var img) ? img : null,
                    url       = $"/Home/Index?q={Uri.EscapeDataString(p.Name)}"
                })
                .ToList();

            return Ok(new { answer = cleanText, products = enriched });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Chatbot config issue.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Chatbot đang bận, vui lòng thử lại sau." });
        }
    }

    /// <returns>Context text + dict[productId → imageUrl] để enrich response.</returns>
    private async Task<(string context, Dictionary<int, string?> productMap)> BuildWebsiteContextAsync()
    {
        var stores = await _db.Stores
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Take(8)
            .Select(s => new { s.Name, s.Address })
            .ToListAsync();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Store.IsActive)
            .OrderByDescending(p => p.Id)
            .Take(30)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.ImageUrl,
                StoreName = p.Store.Name
            })
            .ToListAsync();

        var productMap = products.ToDictionary(p => p.Id, p => p.ImageUrl);

        var sb = new StringBuilder();
        sb.AppendLine("Ngữ cảnh website DayliFood:");
        sb.AppendLine("Các trang chính: /Home/Index (thực đơn), /Home/Stores (quán ngon), /Cart/Index (giỏ hàng), /Order/History (đơn hàng), /Account/Login (đăng nhập).");
        sb.AppendLine("Hãy tư vấn món ăn trong danh sách bên dưới. Khi đề xuất sản phẩm, dùng tag [PRODUCT:id] ngay sau tên sản phẩm (ví dụ: Bò lúc lắc [PRODUCT:12]).");
        sb.AppendLine("Hỗ trợ thanh toán: COD (tiền mặt khi nhận), VNPay, Momo.");
        sb.AppendLine();

        sb.AppendLine("Quán đang hoạt động:");
        foreach (var s in stores)
            sb.AppendLine($"- {s.Name}{(string.IsNullOrWhiteSpace(s.Address) ? string.Empty : $" — {s.Address}")}");

        sb.AppendLine();
        sb.AppendLine("Một số món đang bán:");
        foreach (var p in products)
            sb.AppendLine($"- {p.Name} | {p.StoreName} | {p.Price:N0} đ [PRODUCT:{p.Id}]");

        return (sb.ToString(), productMap);
    }

    public sealed class ChatbotRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
