using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ShopNgocLan.Models
{
    public class ProductDetailsViewModel
    {
        // ID của sản phẩm
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Sản phẩm")]
        public string TenSanPham { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Display(Name = "Thương hiệu")]
        public string? ThuongHieu { get; set; }

        [Display(Name = "Chất liệu")]
        public string? ChatLieu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int DanhMucId { get; set; }

        [Display(Name = "Trạng thái (Đang bán?)")]
        public bool IsActive { get; set; } = true;

        // --- Quản lý Phiên bản (biến thể) ---
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();

        // --- Quản lý Hình ảnh ---

        // 1. Danh sách ảnh ĐÃ CÓ (để hiển thị)
        [Display(Name = "Ảnh hiện tại")]
        public List<ProductImageViewModel> ExistingImages { get; set; } = new List<ProductImageViewModel>();

        // 2. Danh sách ảnh MỚI TẢI LÊN (nếu dùng lại VM này cho Edit)
        [Display(Name = "Tải thêm ảnh mới")]
        public List<IFormFile>? ImageFiles { get; set; }

        // 3. Danh sách ID của các ảnh CŨ cần XÓA
        public List<int>? ImagesToDelete { get; set; }

        // 4. Ảnh đại diện
        [Display(Name = "Chọn ảnh đại diện (từ ảnh cũ)")]
        public int? MainImageId { get; set; }

        [Display(Name = "Chọn ảnh đại diện (từ ảnh mới)")]
        public int? MainImageIndex { get; set; }

        public int ExistingImageCount { get; set; }

        // --- Danh sách danh mục (dùng cho breadcrumb / filter) ---
        public List<DanhMucSanPham> DanhMucList { get; set; } = new List<DanhMucSanPham>();

        // --- Thông tin đánh giá ---
        public int ReviewCount { get; set; }
        public double DiemDanhGia { get; set; }

        // --- Sản phẩm liên quan ---
        public List<RelatedProductViewModel> RelatedProducts { get; set; } = new List<RelatedProductViewModel>();
    }

    public class RelatedProductViewModel
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string AnhDaiDienUrl { get; set; } = "/images/placeholder.jpg";
        public decimal GiaMin { get; set; }
        public decimal GiaMax { get; set; }
    }
}
