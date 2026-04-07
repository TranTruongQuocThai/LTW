using Daylifood.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StoresController : Controller
{
    private readonly ApplicationDbContext _db;

    public StoresController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var stores = await _db.Stores
            .AsNoTracking()
            .Include(s => s.Owner)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return View(stores);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null)
            return NotFound();

        store.IsActive = !store.IsActive;
        await _db.SaveChangesAsync();
        TempData["Message"] = store.IsActive ? "Đã bật cửa hàng." : "Đã tắt cửa hàng.";
        return RedirectToAction(nameof(Index));
    }
}
