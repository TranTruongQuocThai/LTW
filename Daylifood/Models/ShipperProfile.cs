namespace Daylifood.Models;

public class ShipperProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string VehicleType { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public int DeliveryAreaId { get; set; }
    public DeliveryArea DeliveryArea { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
