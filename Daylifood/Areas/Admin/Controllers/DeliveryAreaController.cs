using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DeliveryAreaController : Controller
{
    private readonly ApplicationDbContext _db;

    public DeliveryAreaController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View(new DeliveryArea());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeliveryArea model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Nhập tên khu vực");
            return View(model);
        }

        _db.DeliveryAreas.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm khu vực.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.DeliveryAreas.FindAsync(id);
        if (a == null)
            return NotFound();
        return View(a);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeliveryArea model)
    {
        if (id != model.Id)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Nhập tên khu vực");
            return View(model);
        }

        var a = await _db.DeliveryAreas.FindAsync(id);
        if (a == null)
            return NotFound();

        a.Name = model.Name.Trim();
        a.SortOrder = model.SortOrder;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật khu vực.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.DeliveryAreas.FindAsync(id);
        if (a == null)
            return NotFound();

        var inUse = await _db.Orders.AnyAsync(o => o.DeliveryAreaId == id)
            || await _db.ShipperProfiles.AnyAsync(p => p.DeliveryAreaId == id)
            || await _db.ShipperApplications.AnyAsync(x => x.DeliveryAreaId == id);
        if (inUse)
        {
            TempData["Error"] = "Không xóa được: khu vực đang được sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        _db.DeliveryAreas.Remove(a);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã xóa khu vực.";
        return RedirectToAction(nameof(Index));
    }
}
