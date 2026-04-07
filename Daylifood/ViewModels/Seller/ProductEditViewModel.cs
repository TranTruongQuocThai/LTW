using System.ComponentModel.DataAnnotations;

namespace Daylifood.ViewModels.Seller;

public class ProductEditViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Tồn kho")]
    public int Stock { get; set; }

    [Required]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "URL hình ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;
}
