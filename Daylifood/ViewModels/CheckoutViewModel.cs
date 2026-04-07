using Daylifood.Models;
using System.ComponentModel.DataAnnotations;

namespace Daylifood.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Chọn khu vực giao hàng")]
    [Display(Name = "Khu vực giao hàng")]
    public int DeliveryAreaId { get; set; }

    [Required(ErrorMessage = "Nhập địa chỉ giao hàng")]
    [Display(Name = "Địa chỉ chi tiết")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chọn phương thức thanh toán")]
    [Display(Name = "Phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;
}
