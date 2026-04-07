namespace Daylifood.Models;

public class DeliveryArea
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ShipperProfile> ShipperProfiles { get; set; } = new List<ShipperProfile>();
    public ICollection<ShipperApplication> ShipperApplications { get; set; } = new List<ShipperApplication>();
}
