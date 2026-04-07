using Daylifood.Data;
using Daylifood.Models;
using Daylifood.Services;
using Daylifood.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Daylifood.Controllers;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVnPayService _vnPayService;
    private readonly IMomoService _momoService;

    public PaymentController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IVnPayService vnPayService,
        IMomoService momoService)
    {
        _db = db;
        _userManager = userManager;
        _vnPayService = vnPayService;
        _momoService = momoService;
    }

    // ─── VNPay ───────────────────────────────────────────────────────────────

    [Authorize(Roles = "User,Shipper")]
    public async Task<IActionResult> CreateVnPayPayment(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == userId);
        if (order == null)
            return NotFound();

        if (order.PaymentMethod != PaymentMethod.VnPay)
        {
            TempData["Error"] = "Đơn hàng này không dùng cổng VNPay.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["Message"] = "Đơn hàng đã thanh toán xong.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            TempData["Error"] = "Đơn hàng đã bị hủy, không thể thanh toán.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        try
        {
            var paymentUrl = _vnPayService.CreatePaymentUrl(order, GetClientIpAddress());
            return Redirect(paymentUrl);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VnPayReturn()
    {
        var callback = _vnPayService.ParseCallback(Request.Query);
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        Order? order = null;
        if (callback.OrderId > 0)
        {
            order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.DeliveryArea)
                .FirstOrDefaultAsync(o => o.Id == callback.OrderId && o.BuyerId == userId);
        }

        return View(new VnPayReturnViewModel
        {
            Order    = order,
            Callback = callback
        });
    }

    [AllowAnonymous]
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VnPayIpn()
    {
        var callback = _vnPayService.ParseCallback(Request.Query);
        if (!callback.IsSignatureValid)
            return IpnResponse("97", "Invalid signature");

        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == callback.OrderId);

        if (order == null)
            return IpnResponse("01", "Order not found");

        if (order.PaymentMethod != PaymentMethod.VnPay)
            return IpnResponse("01", "Order not found");

        var expectedAmount = decimal.ToInt64(decimal.Round(order.TotalPrice * 100m, 0, MidpointRounding.AwayFromZero));
        if (expectedAmount != callback.Amount)
            return IpnResponse("04", "Invalid amount");

        if (order.PaymentStatus == PaymentStatus.Paid)
            return IpnResponse("02", "Order already confirmed");

        order.PaymentGatewayTransactionNo     = callback.TransactionNo;
        order.PaymentGatewayResponseCode      = callback.ResponseCode;

        if (callback.IsSuccess)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status        = OrderStatus.Pending;
            order.PaidAt        = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return IpnResponse("00", "Confirm Success");
        }

        if (order.PaymentStatus != PaymentStatus.Failed)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.Status        = OrderStatus.Cancelled;

            if (order.InventoryCommitted)
            {
                foreach (var item in order.OrderItems)
                    item.Product.Stock += item.Quantity;

                order.InventoryCommitted = false;
            }

            await _db.SaveChangesAsync();
        }

        return IpnResponse("00", "Confirm Success");
    }

    // ─── Momo ────────────────────────────────────────────────────────────────

    [Authorize(Roles = "User,Shipper")]
    public async Task<IActionResult> CreateMomoPayment(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == userId);
        if (order == null)
            return NotFound();

        if (order.PaymentMethod != PaymentMethod.Momo)
        {
            TempData["Error"] = "Đơn hàng này không dùng cổng Momo.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["Message"] = "Đơn hàng đã thanh toán xong.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            TempData["Error"] = "Đơn hàng đã bị hủy, không thể thanh toán.";
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }

        try
        {
            var payUrl = await _momoService.CreatePaymentUrlAsync(order, HttpContext.RequestAborted);
            return Redirect(payUrl);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Detail", "Order", new { id = orderId });
        }
    }

    [Authorize(Roles = "User,Shipper")]
    [HttpGet]
    public async Task<IActionResult> MomoReturn()
    {
        var callback = _momoService.ParseCallback(Request.Query);
        var userId   = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        Order? order = null;
        if (callback.OrderId > 0)
        {
            order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.DeliveryArea)
                .FirstOrDefaultAsync(o => o.Id == callback.OrderId && o.BuyerId == userId);

            // Cập nhật trạng thái khi redirect về (backup nếu IPN chậm)
            if (order != null && callback.IsSignatureValid && callback.IsSuccess
                && order.PaymentStatus != PaymentStatus.Paid)
            {
                var tracked = await _db.Orders.FindAsync(order.Id);
                if (tracked != null && tracked.PaymentStatus != PaymentStatus.Paid)
                {
                    tracked.PaymentStatus               = PaymentStatus.Paid;
                    tracked.Status                      = OrderStatus.Pending;
                    tracked.PaidAt                      = DateTime.UtcNow;
                    tracked.PaymentGatewayTransactionNo = callback.TransactionId;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return View(new MomoReturnViewModel
        {
            Order    = order,
            Callback = callback
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MomoIpn()
    {
        // Momo gửi IPN dạng GET hoặc POST form-urlencoded tuỳ phiên bản
        var query    = Request.Query;
        var callback = _momoService.ParseCallback(query);

        if (!callback.IsSignatureValid)
            return IpnResponse("97", "Invalid signature");

        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == callback.OrderId);

        if (order == null || order.PaymentMethod != PaymentMethod.Momo)
            return IpnResponse("01", "Order not found");

        if (order.PaymentStatus == PaymentStatus.Paid)
            return IpnResponse("02", "Order already confirmed");

        order.PaymentGatewayTransactionNo = callback.TransactionId;
        order.PaymentGatewayResponseCode  = callback.ResultCode;

        if (callback.IsSuccess)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status        = OrderStatus.Pending;
            order.PaidAt        = DateTime.UtcNow;
        }
        else if (order.PaymentStatus != PaymentStatus.Failed)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.Status        = OrderStatus.Cancelled;

            if (order.InventoryCommitted)
            {
                foreach (var item in order.OrderItems)
                    item.Product.Stock += item.Quantity;

                order.InventoryCommitted = false;
            }
        }

        await _db.SaveChangesAsync();
        return IpnResponse("00", "Success");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private ContentResult IpnResponse(string code, string message)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["RspCode"] = code,
            ["Message"] = message
        });
        return Content(payload, "application/json");
    }

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstIp = forwardedFor.ToString().Split(',').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstIp))
                return firstIp.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
