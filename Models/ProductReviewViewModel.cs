using System.ComponentModel.DataAnnotations;

namespace ShopNgocLan.Models
{
    public class ProductReviewViewModel
    {
        public int Id { get; set; } // ID của đánh giá (MỚI)
        public int UserId { get; set; }
        // ID Sản phẩm cần được đánh giá
        [Required]
        public int SanPhamId { get; set; }

        // Điểm đánh giá (ví dụ: 1 đến 5 sao)
        [Required(ErrorMessage = "Vui lòng chọn điểm đánh giá.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5.")]
        public int DiemDanhGia { get; set; }

        // Nội dung đánh giá
        [StringLength(500, ErrorMessage = "Nội dung đánh giá không được vượt quá 500 ký tự.")]
        public string? NoiDung { get; set; }

        // (Tùy chọn) Thêm thông tin người dùng nếu bạn muốn hiển thị họ trên View Model
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? NgayDanhGia { get; set; }
        public string? PhanHoiAdmin { get; set; }
        public DateTime? NgayPhanHoi { get; set; }
        public string? AdminName { get; set; }
        public string? AdminAvatarUrl { get; set; }
    }
}
