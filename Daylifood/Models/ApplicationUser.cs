using Microsoft.AspNetCore.Identity;

namespace Daylifood.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }

    public Store? Store { get; set; }
    public ShipperProfile? ShipperProfile { get; set; }
    public ICollection<SellerApplication> SellerApplications { get; set; } = new List<SellerApplication>();
    public ICollection<ShipperApplication> ShipperApplications { get; set; } = new List<ShipperApplication>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Cart? Cart { get; set; }
}
