using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Shipper.Controllers;

[Area("Shipper")]
[Authorize(Roles = "Shipper")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<ShipperProfile?> GetMyProfileAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return null;
        return await _db.ShipperProfiles
            .Include(p => p.DeliveryArea)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<IActionResult> Index()
    {
        var profile = await GetMyProfileAsync();
        if (profile == null)
            return NotFound();

        var areaId = profile.DeliveryAreaId;
        var available = await _db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Confirmed && o.ShipperId == null && o.DeliveryAreaId == areaId);
        var delivering = await _db.Orders.CountAsync(o =>
            o.ShipperId == profile.UserId && o.Status == OrderStatus.Shipping);

        ViewBag.AvailableCount = available;
        ViewBag.DeliveringCount = delivering;
        ViewBag.Profile = profile;
        return View();
    }

    public async Task<IActionResult> AvailableOrders()
    {
        var profile = await GetMyProfileAsync();
        if (profile == null || !profile.IsActive)
            return NotFound();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.DeliveryArea)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Where(o => o.Status == OrderStatus.Confirmed && o.ShipperId == null && o.DeliveryAreaId == profile.DeliveryAreaId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOrder(int id)
    {
        var userId = _userManager.GetUserId(User);
        var profile = await GetMyProfileAsync();
        if (profile == null || !profile.IsActive || userId == null)
            return NotFound();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

        if (order.Status != OrderStatus.Confirmed || order.ShipperId != null || order.DeliveryAreaId != profile.DeliveryAreaId)
        {
            TempData["Error"] = "Không thể nhận đơn này.";
            return RedirectToAction(nameof(AvailableOrders));
        }

        order.ShipperId = userId;
        order.Status = OrderStatus.Shipping;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã nhận đơn giao hàng.";
        return RedirectToAction(nameof(MyDeliveries));
    }

    public async Task<IActionResult> MyDeliveries()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await GetMyProfileAsync();
        if (profile == null || userId == null)
            return NotFound();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.DeliveryArea)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Where(o => o.ShipperId == userId && o.Status == OrderStatus.Shipping)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var userId = _userManager.GetUserId(User);
        var profile = await GetMyProfileAsync();
        if (profile == null || userId == null)
            return NotFound();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.DeliveryArea)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        var canSee = order.ShipperId == null && order.Status == OrderStatus.Confirmed && order.DeliveryAreaId == profile.DeliveryAreaId
            || order.ShipperId == userId;
        if (!canSee)
            return Forbid();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivery(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return NotFound();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShipperId == userId);
        if (order == null)
            return NotFound();

        if (order.Status != OrderStatus.Shipping)
        {
            TempData["Error"] = "Trạng thái đơn không hợp lệ.";
            return RedirectToAction(nameof(MyDeliveries));
        }

        order.Status = OrderStatus.Delivered;
        if (order.PaymentMethod == PaymentMethod.Cod && order.PaymentStatus != PaymentStatus.Paid)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xác nhận giao hàng thành công.";
        return RedirectToAction(nameof(MyDeliveries));
    }

    [HttpGet]
    public async Task<IActionResult> SetServiceArea()
    {
        var profile = await GetMyProfileAsync();
        if (profile == null)
            return NotFound();

        var areas = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ToListAsync();
        ViewBag.DeliveryAreas = new SelectList(areas, "Id", "Name", profile.DeliveryAreaId);
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetServiceArea(int deliveryAreaId)
    {
        var profile = await GetMyProfileAsync();
        if (profile == null)
            return NotFound();

        var areaExists = await _db.DeliveryAreas.AnyAsync(a => a.Id == deliveryAreaId);
        if (!areaExists)
        {
            TempData["Error"] = "Khu vực không hợp lệ.";
            return RedirectToAction(nameof(SetServiceArea));
        }

        profile.DeliveryAreaId = deliveryAreaId;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật khu vực hoạt động.";
        return RedirectToAction(nameof(Index));
    }
}
