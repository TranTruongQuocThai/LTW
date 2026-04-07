using Daylifood.Data;
using Daylifood.Models;
using Daylifood.ViewModels.Seller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Roles = "Seller")]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
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

        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.StoreId == store.Id)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        await LoadCategoriesAsync();
        return View(new ProductEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel model)
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        await LoadCategoriesAsync();
        if (!ModelState.IsValid)
            return View(model);

        _db.Products.Add(new Product
        {
            StoreId = store.Id,
            CategoryId = model.CategoryId,
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            Stock = model.Stock,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/images/placeholder.svg" : model.ImageUrl,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);
        if (product == null)
            return NotFound();

        await LoadCategoriesAsync();
        return View(new ProductEditViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        await LoadCategoriesAsync();
        if (!ModelState.IsValid)
            return View(model);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);
        if (product == null)
            return NotFound();

        product.CategoryId = model.CategoryId;
        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Stock = model.Stock;
        product.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/images/placeholder.svg" : model.ImageUrl;
        product.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await GetMyStoreAsync();
        if (store == null)
            return NotFound();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);
        if (product == null)
            return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync()
    {
        var cats = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(cats, "Id", "Name");
    }
}
