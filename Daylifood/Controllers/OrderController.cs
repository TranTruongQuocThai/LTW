using Daylifood.Data;
using Daylifood.Models;
using Daylifood.Services;
using Daylifood.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Controllers;

[Authorize(Roles = "User,Shipper")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var cart = await CartHelper.GetOrCreateCartAsync(_db, userId);
        await _db.Entry(cart).Collection(c => c.Items).Query().Include(i => i.Product).LoadAsync();
        if (!cart.Items.Any())
        {
            TempData["Message"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        var areas = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ToListAsync();
        ViewBag.DeliveryAreas = new SelectList(areas, "Id", "Name");
        ViewBag.CartTotal = cart.Items.Sum(i => i.Product.Price * i.Quantity);
        return View(new CheckoutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var areas = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ToListAsync();
        ViewBag.DeliveryAreas = new SelectList(areas, "Id", "Name", model.DeliveryAreaId);

        var cart = await CartHelper.GetOrCreateCartAsync(_db, userId);
        await _db.Entry(cart).Collection(c => c.Items).Query().Include(i => i.Product).ThenInclude(p => p.Store).LoadAsync();
        ViewBag.CartTotal = cart.Items.Sum(i => i.Product.Price * i.Quantity);

        if (!ModelState.IsValid)
            return View(model);

        if (!cart.Items.Any())
        {
            TempData["Message"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        foreach (var line in cart.Items)
        {
            if (line.Quantity > line.Product.Stock)
            {
                ModelState.AddModelError(string.Empty, $"Sản phẩm {line.Product.Name} không đủ tồn kho.");
                return View(model);
            }
        }

        var total = cart.Items.Sum(i => i.Product.Price * i.Quantity);
        var isOnlinePayment = model.PaymentMethod is PaymentMethod.VnPay or PaymentMethod.Momo;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                BuyerId = userId,
                DeliveryAreaId = model.DeliveryAreaId,
                DeliveryAddress = model.DeliveryAddress,
                TotalPrice = total,
                Status = isOnlinePayment ? OrderStatus.AwaitingPayment : OrderStatus.Pending,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = isOnlinePayment ? PaymentStatus.Pending : PaymentStatus.Unpaid
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            order.PaymentReference = order.Id.ToString();

            foreach (var line in cart.Items)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    Price = line.Product.Price
                });

                line.Product.Stock -= line.Quantity;
            }

            order.InventoryCommitted = true;
            _db.CartItems.RemoveRange(cart.Items);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (model.PaymentMethod == PaymentMethod.Momo)
                return RedirectToAction("CreateMomoPayment", "Payment", new { orderId = order.Id });

            if (model.PaymentMethod == PaymentMethod.VnPay)
                return RedirectToAction("CreateVnPayPayment", "Payment", new { orderId = order.Id });

            TempData["Message"] = $"Đặt hàng thành công. Mã đơn #{order.Id}";
            return RedirectToAction(nameof(History));
        }
        catch
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng lúc này. Vui lòng thử lại.");
            return View(model);
        }
    }

    public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.DeliveryArea)
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.DeliveryArea)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == userId);

        if (order == null)
            return NotFound();

        return View(order);
    }
}
