using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using Microsoft.AspNetCore.Http; // Cần thiết cho GetInt32
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ShopNgocLan.Controllers
{
    public class ReviewController : Controller
    {
        private readonly DBShopNLContext _context;

        public ReviewController(DBShopNLContext context)
        {
            _context = context;
        }

        // Phương thức hỗ trợ lấy User ID từ Session
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // ----------------------------------------------------------------------
        // PHƯƠNG THỨC MỚI: Kiểm tra người dùng đã mua sản phẩm thành công chưa
        // ----------------------------------------------------------------------
        private async Task<bool> HasUserPurchasedProductSuccessfully(int userId, int sanPhamId)
        {
            // Truy vấn để kiểm tra:
            // 1. Có ChiTietHoaDon nào?
            // 2. ChiTietSanPham đó thuộc về SanPhamId đang xét?
            // 3. HoaDon đó của UserId hiện tại?
            // 4. Trạng thái HoaDon là "Delivered" (Đã giao thành công)?

            bool hasPurchased = await _context.ChiTietHoaDons
                .Include(cthd => cthd.HoaDon) // Cần include HoaDon để lấy UserId và TrangThai
                .Include(cthd => cthd.ChiTietSanPham) // Cần include ChiTietSanPham để lấy SanPhamId
                .AnyAsync(cthd =>
                    cthd.ChiTietSanPham.SanPhamId == sanPhamId &&
                    cthd.HoaDon.UserId == userId &&
                    cthd.HoaDon.TrangThai.MaTrangThai == "Delivered"
                );

            return hasPurchased;
        }

        // ----------------------------------------------------------------------
        // 1. ACTION: Xử lý việc thêm/cập nhật đánh giá (POST)
        // ----------------------------------------------------------------------
        public class JsonResultViewModel
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public object? Data { get; set; } // Để trả về dữ liệu nếu cần
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ProductReviewViewModel viewModel)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Json(new JsonResultViewModel { Success = false, Message = "Vui lòng đăng nhập để đánh giá sản phẩm." });
            }

            int currentUserId = userId.Value;

            // Kiểm tra mua hàng
            if (!await HasUserPurchasedProductSuccessfully(currentUserId, viewModel.SanPhamId))
            {
                return Json(new JsonResultViewModel { Success = false, Message = "Bạn chỉ có thể đánh giá sản phẩm đã mua và được giao thành công." });
            }

            if (!ModelState.IsValid)
            {
                // Trả về lỗi nếu dữ liệu không hợp lệ (ví dụ: DiemDanhGia không phải 1-5, Nội dung quá dài)
                // Lấy thông báo lỗi đầu tiên
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new JsonResultViewModel { Success = false, Message = string.Join(" ", errors) });
            }

            // --- LOGIC THÊM/CẬP NHẬT (Giữ nguyên) ---
            try
            {
                var existingReview = await _context.DanhGiaSanPhams
                    .FirstOrDefaultAsync(r => r.SanPhamId == viewModel.SanPhamId && r.UserId == currentUserId);

                if (existingReview != null)
                {
                    existingReview.DiemDanhGia = viewModel.DiemDanhGia;
                    existingReview.NoiDung = viewModel.NoiDung;
                    existingReview.NgayDanhGia = DateTime.Now;
                    _context.DanhGiaSanPhams.Update(existingReview);
                    await _context.SaveChangesAsync();
                    return Json(new JsonResultViewModel { Success = true, Message = "Cập nhật đánh giá thành công!" });
                }
                else
                {
                    var newReview = new DanhGiaSanPham
                    {
                        SanPhamId = viewModel.SanPhamId,
                        UserId = currentUserId,
                        DiemDanhGia = viewModel.DiemDanhGia,
                        NoiDung = viewModel.NoiDung,
                        NgayDanhGia = DateTime.Now
                    };
                    _context.DanhGiaSanPhams.Add(newReview);
                    await _context.SaveChangesAsync();
                    return Json(new JsonResultViewModel { Success = true, Message = "Gửi đánh giá thành công!" });
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (ex)
                return Json(new JsonResultViewModel { Success = false, Message = "Lỗi server: Không thể lưu đánh giá." });
            }
        }


        // ----------------------------------------------------------------------
        // 2. ACTION: Tải danh sách đánh giá (Partial View)
        // ----------------------------------------------------------------------
        public async Task<IActionResult> _GetReviewsPartial(int sanPhamId)
        {
            var reviews = await _context.DanhGiaSanPhams
                // 1. LỌC TRẠNG THÁI: Chỉ hiển thị đánh giá công khai
                .Where(r => r.SanPhamId == sanPhamId && r.IsPublished == true)

                .OrderByDescending(r => r.NgayDanhGia)

                // Cần Include AdminUser để lấy tên người phản hồi nếu có
                .Include(r => r.User)
                .Include(r => r.AdminUser)

                .Select(r => new ProductReviewViewModel
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    SanPhamId = r.SanPhamId,

                    // Thông tin người đánh giá (Khách hàng)
                    AvatarUrl = r.User.AvatarUrl,
                    UserName = r.User.Ho + " " + r.User.Ten,

                    DiemDanhGia = r.DiemDanhGia,
                    NoiDung = r.NoiDung,
                    NgayDanhGia = r.NgayDanhGia,

                    // 2. THÔNG TIN PHẢN HỒI ADMIN
                    PhanHoiAdmin = r.PhanHoiAdmin,
                    NgayPhanHoi = r.NgayPhanHoi,

                    // Tên Admin (dùng để hiển thị trong View)
                    AdminName = (r.AdminUser != null) ? (r.AdminUser.Ho + " " + r.AdminUser.Ten) : null,
                    AdminAvatarUrl = r.AdminUser.AvatarUrl ?? ""
                })
                .ToListAsync();

            return PartialView("_ProductReviewsPartial", reviews);
        }
    }
}