using Daylifood.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShippersController : Controller
{
    private readonly ApplicationDbContext _db;

    public ShippersController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.ShipperProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.DeliveryArea)
            .OrderBy(p => p.User!.Email)
            .ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var profile = await _db.ShipperProfiles.FindAsync(id);
        if (profile == null)
            return NotFound();

        profile.IsActive = !profile.IsActive;
        await _db.SaveChangesAsync();
        TempData["Message"] = profile.IsActive ? "Đã bật shipper." : "Đã tắt shipper.";
        return RedirectToAction(nameof(Index));
    }
}
