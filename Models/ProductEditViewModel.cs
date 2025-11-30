using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ShopNgocLan.Models
{
    public class ProductEditViewModel
    {
        // ID của sản phẩm đang sửa
        public int Id { get; set; }

        // --- Các thuộc tính cơ bản (giống hệt ProductCreateViewModel) ---
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

        // --- Quản lý Phiên bản (Dùng ProductVariantViewModel) ---
        // Chứa danh sách TẤT CẢ phiên bản (cũ và mới)
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();

        // --- Quản lý Hình ảnh ---

        // 1. Danh sách ảnh ĐÃ CÓ (để hiển thị)
        [Display(Name = "Ảnh hiện tại")]
        public List<ProductImageViewModel> ExistingImages { get; set; } = new List<ProductImageViewModel>();

        // 2. Danh sách ảnh MỚI TẢI LÊN (giống Create)
        [Display(Name = "Tải thêm ảnh mới")]
        public List<IFormFile>? ImageFiles { get; set; }

        // 3. Danh sách ID của các ảnh CŨ cần XÓA
        // (JavaScript sẽ thêm ID vào đây khi người dùng bấm nút xóa ảnh)
        public List<int>? ImagesToDelete { get; set; }

        // 4. Quản lý ảnh đại diện
        // Dùng để chọn 1 ảnh CŨ làm ảnh đại diện
        [Display(Name = "Chọn ảnh đại diện (từ ảnh cũ)")]
        public int? MainImageId { get; set; }

        // Dùng để chọn 1 ảnh MỚI TẢI LÊN làm ảnh đại diện (giống Create)
        [Display(Name = "Chọn ảnh đại diện (từ ảnh mới)")]
        public int? MainImageIndex { get; set; }

        public int ExistingImageCount { get; set; }
        // --- Dữ liệu cho Dropdowns (giống hệt ProductCreateViewModel) ---
        public SelectList? DanhMucList { get; set; }
        public SelectList? MauSacList { get; set; }
        public SelectList? SizeList { get; set; }
    }
}