using Daylifood.Data;
using Daylifood.Models;
using Daylifood.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Controllers;

[Authorize(Roles = "User,Shipper")]
public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var cart = await CartHelper.GetOrCreateCartAsync(_db, userId);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var product = await _db.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Store.IsActive);
        if (product == null)
            return NotFound();

        if (quantity < 1)
            quantity = 1;
        if (quantity > product.Stock)
            quantity = product.Stock;

        var cart = await CartHelper.GetOrCreateCartAsync(_db, userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            item = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity };
            _db.CartItems.Add(item);
        }
        else
        {
            item.Quantity = Math.Min(item.Quantity + quantity, product.Stock);
            _db.CartItems.Update(item);
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm vào giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var item = await _db.CartItems
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == userId);
        if (item == null)
            return NotFound();

        if (quantity < 1)
            quantity = 1;
        item.Quantity = Math.Min(quantity, item.Product.Stock);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var item = await _db.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == userId);
        if (item == null)
            return NotFound();

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
