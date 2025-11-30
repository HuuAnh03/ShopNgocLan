using Microsoft.AspNetCore.Mvc.Rendering; // <-- Cần cho SelectList
using System.ComponentModel.DataAnnotations; // <-- Cần cho Validation Attributes
using Microsoft.AspNetCore.Http; // <-- Cần cho IFormFile

namespace ShopNgocLan.Models
{
    public class ProductCreateViewModel
    {
        // --- Thuộc tính của Sản phẩm chính ---
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Sản phẩm")] // <-- Thêm Display Name
        public string TenSanPham { get; set; } = string.Empty;

        [Display(Name = "Mô tả")] // <-- Thêm Display Name
        public string? MoTa { get; set; }

        [Display(Name = "Thương hiệu")] // <-- Thêm Display Name
        public string? ThuongHieu { get; set; }

        [Display(Name = "Chất liệu")] // <-- Thêm Display Name
        public string? ChatLieu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int DanhMucId { get; set; }

        [Display(Name = "Trạng thái (Đang bán?)")] // <-- Thêm Display Name
        public bool IsActive { get; set; } = true; // Mặc định là đang bán

        // --- Dữ liệu cho các Phiên bản (Variants) ---
        // Danh sách này sẽ được gửi từ Form (thông qua JavaScript)
        public List<VariantViewModel> Variants { get; set; } = new List<VariantViewModel>();

        // --- Dữ liệu cho Hình ảnh ---
        [Display(Name = "Hình ảnh sản phẩm")]
        // Không cần [Required] ở đây vì ta sẽ kiểm tra trong Controller
        public List<IFormFile>? ImageFiles { get; set; }

        // Index của ảnh đại diện (được set bởi JavaScript)
        [Display(Name = "Chọn ảnh đại diện")]
        public int? MainImageIndex { get; set; }

        // --- *** BẠN ĐANG THIẾU CÁC THUỘC TÍNH NÀY *** ---
        // --- Thuộc tính để chứa dữ liệu cho Dropdowns ---
        public SelectList? DanhMucList { get; set; }
        public SelectList? MauSacList { get; set; }
        public SelectList? SizeList { get; set; }
        // --------------------------------------------------
    }

    // Class này giữ nguyên như trước
    public class VariantViewModel
    {
        [Required(ErrorMessage = "Giá là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0.")]
        [Display(Name = "Giá")]
        public decimal Gia { get; set; }
        public decimal GiaNhap { get; set; }

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
    }
}