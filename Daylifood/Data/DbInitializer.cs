using Daylifood.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Data;

public static class DbInitializer
{
    private static readonly Random Rng = new(2026);

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roles = ["Admin", "User", "Seller", "Shipper"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Rau củ" },
                new Category { Name = "Trái cây" },
                new Category { Name = "Thịt & Hải sản" },
                new Category { Name = "Đồ uống" },
                new Category { Name = "Bún & Phở" },
                new Category { Name = "Cơm & Món chính" },
                new Category { Name = "Đồ ăn vặt" },
                new Category { Name = "Bánh mì & Bánh" });
            await context.SaveChangesAsync();
        }
        else
        {
            await EnsureExtraCategoriesAsync(context);
        }

        if (!await context.DeliveryAreas.AnyAsync())
        {
            context.DeliveryAreas.AddRange(
                new DeliveryArea { Name = "Quận 1", SortOrder = 1 },
                new DeliveryArea { Name = "Quận 3", SortOrder = 2 },
                new DeliveryArea { Name = "Quận 7", SortOrder = 3 },
                new DeliveryArea { Name = "Thủ Đức", SortOrder = 4 });
            await context.SaveChangesAsync();
        }

        const string adminEmail = "admin@daylifood.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Quản trị viên"
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await context.Stores.AnyAsync())
            await SeedTenSellersWithStoresAsync(context, userManager);

        if (!await context.ShipperProfiles.AnyAsync())
            await SeedTenShippersAsync(context, userManager);
    }

    private static async Task EnsureExtraCategoriesAsync(ApplicationDbContext context)
    {
        var existing = await context.Categories.Select(c => c.Name).ToListAsync();
        foreach (var name in new[]
                 {
                     "Bún & Phở", "Cơm & Món chính", "Đồ ăn vặt", "Bánh mì & Bánh"
                 })
        {
            if (!existing.Contains(name))
                context.Categories.Add(new Category { Name = name });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>50 món khác nhau — trộn ngẫu nhiên rồi chia 5 món/quán (10 quán).</summary>
    private static List<(string Name, string Description, string CategoryName)> BuildDishPool()
    {
        var list = new List<(string, string, string)>
        {
            ("Phở bò tái", "Nước dùng xương ống", "Bún & Phở"),
            ("Phở gà", "Gà ta thả vườn", "Bún & Phở"),
            ("Bún bò Huế", "Mọc huyết, chả cua", "Bún & Phở"),
            ("Bún chả Hà Nội", "Nướng than, nem", "Bún & Phở"),
            ("Bún riêu cua", "Đậu hũ, riêu cua", "Bún & Phở"),
            ("Hủ tiếu Nam Vang", "Xương hầm", "Bún & Phở"),
            ("Mì Quảng", "Tôm thịt, đậu phộng", "Bún & Phở"),
            ("Bún thịt nướng", "Chả giò", "Bún & Phở"),
            ("Cơm tấm sườn bì", "Trứng ốp la", "Cơm & Món chính"),
            ("Cơm gà xối mỡ", "Gà luộc, gỏi", "Cơm & Món chính"),
            ("Cơm chiên Dương Châu", "Tôm, lạp xưởng", "Cơm & Món chính"),
            ("Cơm gà Hải Nam", "Gà xé, mỡ hành", "Cơm & Món chính"),
            ("Cơm sườn ram", "Trứng kho", "Cơm & Món chính"),
            ("Cơm cá kho tộ", "Cá basa", "Cơm & Món chính"),
            ("Cơm chiên hải sản", "Mực, tôm", "Cơm & Món chính"),
            ("Bánh mì thịt nướng", "Pate, dưa chua", "Bánh mì & Bánh"),
            ("Bánh mì chả cá", "Rau răm", "Bánh mì & Bánh"),
            ("Bánh cuốn nóng", "Chả lụa, hành phi", "Bánh mì & Bánh"),
            ("Bánh xèo miền Tây", "Giá, tôm", "Bánh mì & Bánh"),
            ("Bánh tráng trộn", "Khô bò, trứng cút", "Đồ ăn vặt"),
            ("Gỏi cuốn tôm thịt", "Nước mắm pha", "Đồ ăn vặt"),
            ("Chả giò chiên", "Khoai môn", "Đồ ăn vặt"),
            ("Nem nướng Nha Trang", "Bánh hỏi", "Đồ ăn vặt"),
            ("Chân gà sả ớt", "Cay vừa", "Đồ ăn vặt"),
            ("Bò lúc lắc", "Bơ tỏi", "Thịt & Hải sản"),
            ("Tôm rang me", "Tôm sú", "Thịt & Hải sản"),
            ("Cá hồi áp chảo", "Sốt chanh dây", "Thịt & Hải sản"),
            ("Mực nướng muối ớt", "Mực ống", "Thịt & Hải sản"),
            ("Sườn nướng BBQ", "Khoai tây chiên", "Thịt & Hải sản"),
            ("Salad rau trộn", "Sốt mè rang", "Rau củ"),
            ("Rau muống xào tỏi", "Giòn xanh", "Rau củ"),
            ("Canh chua cá", "Đậu bắp", "Rau củ"),
            ("Gỏi ngó sen tôm", "Đậu phộng", "Rau củ"),
            ("Đậu hũ sốt cà", "Hành lá", "Rau củ"),
            ("Xoài cát Hòa Lộc", "Trái lớn", "Trái cây"),
            ("Dưa hấu đỏ", "Cắt miếng", "Trái cây"),
            ("Thanh long ruột đỏ", "Bình Thuận", "Trái cây"),
            ("Nho xanh", "Không hạt", "Trái cây"),
            ("Cam sành", "Mọng nước", "Trái cây"),
            ("Trà đào cam sả", "Ít đá", "Đồ uống"),
            ("Cà phê sữa đá", "Robusta", "Đồ uống"),
            ("Nước mía", "Ép lạnh", "Đồ uống"),
            ("Sinh tố bơ", "Bơ sáp", "Đồ uống"),
            ("Trà sữa trân châu", "Kem cheese", "Đồ uống"),
            ("Chè khúc bạch", "Hạnh nhân", "Đồ uống"),
            ("Sữa chua nếp cẩm", "Mít sấy", "Đồ uống"),
            ("Bánh flan caramel", "Mềm mịn", "Đồ ăn vặt"),
            ("Kem dừa", "Dừa xiêm", "Đồ ăn vặt"),
            ("Xôi gà xé", "Hành phi", "Cơm & Món chính"),
            ("Cháo lòng", "Quẩy giòn", "Bún & Phở")
        };

        Shuffle(list, Rng);
        return list;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static async Task SeedTenSellersWithStoresAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        var categories = await context.Categories.AsNoTracking().ToListAsync();
        var catByName = categories.ToDictionary(c => c.Name, c => c.Id);
        var defaultCatId = categories.OrderBy(c => c.Id).First().Id;

        int CatId(string name) =>
            catByName.TryGetValue(name, out var id) ? id : defaultCatId;

        var dishPool = BuildDishPool();
        if (dishPool.Count < 50)
            throw new InvalidOperationException("Cần ít nhất 50 món trong pool.");

        var storeNameTemplates = new[]
        {
            "Bếp nhà {0}", "Quán ăn Phố {0}", "Nhà hàng Dayli {0}", "Cơm tấm Sài Gòn {0}",
            "Tiệm ăn Vườn {0}", "Quán Ngon {0}", "Ẩm thực {0}", "Món Huế {0}",
            "Bếp Xanh {0}", "Góc phố {0}"
        };

        var districts = new[]
        {
            "Quận 1", "Quận 3", "Quận 5", "Quận 7", "Quận 10", "Bình Thạnh", "Tân Bình", "Gò Vấp", "Thủ Đức", "Phú Nhuận"
        };

        for (var i = 0; i < 10; i++)
        {
            var n = i + 1;
            var email = $"seller{n}@daylifood.com";
            var sellerUser = await userManager.FindByEmailAsync(email);
            if (sellerUser == null)
            {
                sellerUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = $"Người bán Demo {n}"
                };
                var create = await userManager.CreateAsync(sellerUser, "Seller123!");
                if (!create.Succeeded)
                    continue;
                await userManager.AddToRoleAsync(sellerUser, "Seller");
            }

            var store = new Store
            {
                OwnerId = sellerUser!.Id,
                Name = string.Format(storeNameTemplates[i], n),
                Address = $"{Rng.Next(10, 999)} Đường Demo, {districts[i]}, TP.HCM",
                Phone = $"09{Rng.Next(10000000, 99999999)}",
                Description = $"Quán ăn gia đình — {districts[i]}. Giao nhanh trong ngày.",
                IsActive = true
            };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            var products = new List<Product>();
            for (var p = 0; p < 5; p++)
            {
                var idx = i * 5 + p;
                var (dishName, desc, catName) = dishPool[idx];
                var price = Rng.Next(15, 180) * 1000m;
                var stock = Rng.Next(5, 120);
                products.Add(new Product
                {
                    StoreId = store.Id,
                    CategoryId = CatId(catName),
                    Name = dishName,
                    Description = desc,
                    Price = price,
                    Stock = stock,
                    ImageUrl = "/images/placeholder.svg",
                    IsActive = true
                });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedTenShippersAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        var areas = await context.DeliveryAreas.AsNoTracking().OrderBy(a => a.SortOrder).ToListAsync();
        if (areas.Count == 0)
            return;

        var vehicles = new[] { "Xe máy", "Xe đạp điện", "Xe máy tay ga", "Ô tô nhỏ" };
        var firstNames = new[]
        {
            "An", "Bình", "Cường", "Dũng", "Em", "Phúc", "Giang", "Hải", "Khoa", "Linh"
        };

        for (var i = 0; i < 10; i++)
        {
            var n = i + 1;
            var email = $"shipper{n}@daylifood.com";
            if (await userManager.FindByEmailAsync(email) != null)
                continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = $"Shipper {firstNames[i]} {n}"
            };
            var create = await userManager.CreateAsync(user, "Shipper123!");
            if (!create.Succeeded)
                continue;

            await userManager.AddToRoleAsync(user, "Shipper");

            var area = areas[Rng.Next(areas.Count)];
            var plate = $"{Rng.Next(29, 52):D2}{Rng.Next(1, 9)}-{Rng.Next(10000, 99999)}";
            context.ShipperProfiles.Add(new ShipperProfile
            {
                UserId = user.Id,
                VehicleType = vehicles[Rng.Next(vehicles.Length)],
                LicensePlate = plate,
                DeliveryAreaId = area.Id,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
    }
}
