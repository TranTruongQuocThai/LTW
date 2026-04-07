using System.ComponentModel.DataAnnotations;

namespace Daylifood.ViewModels;

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
}
