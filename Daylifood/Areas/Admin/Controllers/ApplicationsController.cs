using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var sellerApps = await _db.SellerApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.Status == ApplicationStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var shipperApps = await _db.ShipperApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.DeliveryArea)
            .Where(a => a.Status == ApplicationStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewBag.SellerApplications = sellerApps;
        ViewBag.ShipperApplications = shipperApps;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSeller(int id)
    {
        var app = await _db.SellerApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.Status == ApplicationStatus.Pending);
        if (app == null)
            return NotFound();

        var user = app.User;
        app.Status = ApplicationStatus.Approved;

        if (await _db.Stores.AnyAsync(s => s.OwnerId == user.Id))
        {
            TempData["Error"] = "Người dùng đã có cửa hàng.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.RemoveFromRoleAsync(user, "User");
        await _userManager.AddToRoleAsync(user, "Seller");

        user.FullName = app.ContactFullName;
        user.PhoneNumber = app.ShopPhone;
        await _userManager.UpdateAsync(user);

        _db.Stores.Add(new Store
        {
            OwnerId = user.Id,
            Name = app.ShopName,
            Address = app.ShopAddress,
            Phone = app.ShopPhone,
            Description = app.Description,
            IsActive = true
        });

        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã duyệt đơn đăng ký seller.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectSeller(int id)
    {
        var app = await _db.SellerApplications.FirstOrDefaultAsync(a => a.Id == id && a.Status == ApplicationStatus.Pending);
        if (app == null)
            return NotFound();

        app.Status = ApplicationStatus.Rejected;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã từ chối đơn seller.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveShipper(int id)
    {
        var app = await _db.ShipperApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.Status == ApplicationStatus.Pending);
        if (app == null)
            return NotFound();

        var user = app.User;
        if (await _db.ShipperProfiles.AnyAsync(p => p.UserId == user.Id))
        {
            TempData["Error"] = "Người dùng đã có hồ sơ shipper.";
            return RedirectToAction(nameof(Index));
        }

        app.Status = ApplicationStatus.Approved;
        _db.ShipperProfiles.Add(new ShipperProfile
        {
            UserId = user.Id,
            VehicleType = app.VehicleType,
            LicensePlate = app.LicensePlate,
            DeliveryAreaId = app.DeliveryAreaId,
            IsActive = true
        });

        await _userManager.AddToRoleAsync(user, "Shipper");
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã duyệt đơn đăng ký shipper.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectShipper(int id)
    {
        var app = await _db.ShipperApplications.FirstOrDefaultAsync(a => a.Id == id && a.Status == ApplicationStatus.Pending);
        if (app == null)
            return NotFound();

        app.Status = ApplicationStatus.Rejected;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã từ chối đơn shipper.";
        return RedirectToAction(nameof(Index));
    }
}
