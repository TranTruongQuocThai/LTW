using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoryController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Nhập tên danh mục");
            return View(model);
        }

        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null)
            return NotFound();
        return View(c);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Nhập tên danh mục");
            return View(model);
        }

        var c = await _db.Categories.FindAsync(id);
        if (c == null)
            return NotFound();

        c.Name = model.Name.Trim();
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null)
            return NotFound();

        if (c.Products.Any())
        {
            TempData["Error"] = "Không xóa được: còn sản phẩm thuộc danh mục này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Index));
    }
}
