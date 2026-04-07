using System.ComponentModel.DataAnnotations;

namespace Daylifood.ViewModels;

public class ApplySellerViewModel
{
    [Required(ErrorMessage = "Nhập họ tên")]
    [Display(Name = "Họ tên")]
    public string ContactFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập tên cửa hàng")]
    [Display(Name = "Tên cửa hàng")]
    public string ShopName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập địa chỉ quán")]
    [Display(Name = "Địa chỉ quán")]
    public string ShopAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Số điện thoại từ 8–20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string ShopPhone { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
