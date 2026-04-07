using Daylifood.Data;
using Daylifood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.OrderCount = await _db.Orders.CountAsync();
        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.StoreCount = await _db.Stores.CountAsync();
        ViewBag.PendingSellerApps = await _db.SellerApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
        ViewBag.PendingShipperApps = await _db.ShipperApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
        return View();
    }
}
