using System.ComponentModel.DataAnnotations;

namespace Daylifood.ViewModels;

public class ApplyShipperViewModel
{
    [Required(ErrorMessage = "Chọn khu vực giao hàng")]
    [Display(Name = "Khu vực hoạt động")]
    public int DeliveryAreaId { get; set; }

    [Required(ErrorMessage = "Nhập loại phương tiện")]
    [Display(Name = "Loại xe")]
    public string VehicleType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập biển số")]
    [Display(Name = "Biển số")]
    public string LicensePlate { get; set; } = string.Empty;
}
