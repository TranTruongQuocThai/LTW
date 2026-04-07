using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Roles = "Seller")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<Store?> GetMyStoreAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return null;
        return await _db.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
    }

    public async Task<IActionResult> Index()
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var productCount = await _db.Products.CountAsync(p => p.StoreId == store.Id);
        var revenue = await _db.OrderItems
            .Where(oi => oi.Product.StoreId == store.Id && oi.Order.Status == OrderStatus.Delivered)
            .SumAsync(oi => oi.Price * oi.Quantity);

        ViewBag.Store = store;
        ViewBag.ProductCount = productCount;
        ViewBag.Revenue = revenue;
        return View();
    }

    public async Task<IActionResult> Orders()
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var orderIds = await _db.OrderItems
            .Where(oi => oi.Product.StoreId == store.Id)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.DeliveryArea)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Where(o => orderIds.Contains(o.Id) && o.Status != OrderStatus.AwaitingPayment)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOrder(int id)
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var order = await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Chỉ xác nhận được đơn đang chờ xử lý.";
            return RedirectToAction(nameof(Orders));
        }

        var storeIds = order.OrderItems.Select(i => i.Product.StoreId).Distinct().ToList();
        if (storeIds.Count != 1 || storeIds[0] != store.Id)
        {
            TempData["Error"] = "Chỉ xác nhận được đơn chỉ gồm sản phẩm của quán bạn.";
            return RedirectToAction(nameof(Orders));
        }

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xác nhận đơn hàng.";
        return RedirectToAction(nameof(Orders));
    }

    public async Task<IActionResult> Revenue()
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var lines = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Product.StoreId == store.Id && oi.Order.Status == OrderStatus.Delivered)
            .OrderByDescending(oi => oi.Order.CreatedAt)
            .ToListAsync();

        var total = lines.Sum(oi => oi.Price * oi.Quantity);
        ViewBag.TotalRevenue = total;
        return View(lines);
    }
}
