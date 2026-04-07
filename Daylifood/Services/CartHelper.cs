using Daylifood.Data;
using Daylifood.Models;
using Microsoft.EntityFrameworkCore;

namespace Daylifood.Services;

public static class CartHelper
{
    public static async Task<Cart> GetOrCreateCartAsync(ApplicationDbContext db, string userId)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
            return cart;

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return cart;
    }
}
