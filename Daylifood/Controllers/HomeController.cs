using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Daylifood.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Intro()
    {
        ViewData["Title"] = "Giới thiệu";
        return View();
    }

    public async Task<IActionResult> Index(int? categoryId, string? q)
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SearchQuery = q;

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Where(p => p.IsActive && p.Store.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)) ||
                p.Store.Name.Contains(term));
        }

        var products = await query.OrderByDescending(p => p.Id).ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> Stores(string? q)
    {
        ViewBag.SearchQuery = q;

        var query = _db.Stores
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                (s.Address != null && s.Address.Contains(term)) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.Description != null && s.Description.Contains(term)));
        }

        var stores = await query.OrderBy(s => s.Name).ToListAsync();
        return View(stores);
    }

    public async Task<IActionResult> Store(int id, int? categoryId, string? q)
    {
        var store = await _db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (store == null)
            return NotFound();

        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.Store = store;
        ViewBag.SearchQuery = q;

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Where(p => p.IsActive && p.StoreId == id && p.Store.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        var products = await query.OrderByDescending(p => p.Id).ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.Store.IsActive);

        if (product == null)
            return NotFound();

        return View(product);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
