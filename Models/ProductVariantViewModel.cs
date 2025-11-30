using System.ComponentModel.DataAnnotations;

namespace ShopNgocLan.Models
{
    // Dùng cho action Edit, để phân biệt phiên bản mới và cũ
    public class ProductVariantViewModel
    {
        // Sẽ là null hoặc 0 cho phiên bản MỚI
        // Sẽ có giá trị cho phiên bản CŨ
        public int? Id { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc.")]
        
        [Display(Name = "Giá")]
        public decimal Gia { get; set; }

        public decimal? GiaNhap { get; set; }

        [Required(ErrorMessage = "Số lượng tồn là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không được âm.")]
        [Display(Name = "Số lượng tồn")]
        public int SoLuongTon { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu sắc.")]
        [Display(Name = "Màu sắc")]
        public int MauSacId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn size.")]
        [Display(Name = "Size")]
        public int SizeId { get; set; }

        // Thuộc tính tùy chọn để hiển thị tên trên View
        public string? MauSacName { get; set; }
        public string? MaMauHex { get; set; }
        public string? SizeName { get; set; }
    }
}