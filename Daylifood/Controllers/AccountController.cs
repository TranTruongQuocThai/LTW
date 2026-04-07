using Daylifood.Data;
using Daylifood.Models;
using Daylifood.ViewModels;
using Daylifood.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "User");
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Intro", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Intro", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Intro", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var vm = new ProfileViewModel
        {
            Email = user.Email ?? "",
            FullName = user.FullName ?? "",
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        };
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        await _userManager.UpdateAsync(user);
        TempData["Message"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "User")]
    [HttpGet]
    public IActionResult ApplyAsSeller()
    {
        return View(new ApplySellerViewModel());
    }

    [Authorize(Roles = "User")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyAsSeller(ApplySellerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Seller") || await _userManager.IsInRoleAsync(user, "Admin"))
            return Forbid();

        var pending = await _db.SellerApplications.AnyAsync(a =>
            a.UserId == user.Id && a.Status == ApplicationStatus.Pending);
        if (pending)
        {
            ModelState.AddModelError(string.Empty, "Bạn đã có đơn đăng ký seller đang chờ duyệt.");
            return View(model);
        }

        _db.SellerApplications.Add(new SellerApplication
        {
            UserId = user.Id,
            ContactFullName = model.ContactFullName.Trim(),
            ShopName = model.ShopName.Trim(),
            ShopAddress = model.ShopAddress.Trim(),
            ShopPhone = model.ShopPhone.Trim(),
            Description = model.Description,
            Status = ApplicationStatus.Pending
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã gửi đơn đăng ký làm người bán. Vui lòng chờ admin duyệt.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "User")]
    [HttpGet]
    public async Task<IActionResult> ApplyAsShipper()
    {
        var areas = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync();
        ViewBag.DeliveryAreas = new SelectList(areas, "Id", "Name");
        return View(new ApplyShipperViewModel());
    }

    [Authorize(Roles = "User")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyAsShipper(ApplyShipperViewModel model)
    {
        var areas = await _db.DeliveryAreas.OrderBy(a => a.SortOrder).ToListAsync();
        ViewBag.DeliveryAreas = new SelectList(areas, "Id", "Name", model.DeliveryAreaId);

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Shipper"))
        {
            ModelState.AddModelError(string.Empty, "Bạn đã là shipper.");
            return View(model);
        }

        var pending = await _db.ShipperApplications.AnyAsync(a =>
            a.UserId == user.Id && a.Status == ApplicationStatus.Pending);
        if (pending)
        {
            ModelState.AddModelError(string.Empty, "Bạn đã có đơn đăng ký shipper đang chờ duyệt.");
            return View(model);
        }

        _db.ShipperApplications.Add(new ShipperApplication
        {
            UserId = user.Id,
            DeliveryAreaId = model.DeliveryAreaId,
            VehicleType = model.VehicleType,
            LicensePlate = model.LicensePlate,
            Status = ApplicationStatus.Pending
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = "Đã gửi đơn đăng ký shipper. Vui lòng chờ admin duyệt.";
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
