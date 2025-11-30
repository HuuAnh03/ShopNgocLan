using Microsoft.AspNetCore.Http; // Cần thiết cho GetInt32
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class ReviewController : Controller
    {
        private readonly DBShopNLContext _context;

        public ReviewController(DBShopNLContext context)
        {
            _context = context;
        }

        // Phương thức hỗ trợ: Lấy ID Staff/Admin đang đăng nhập
        private int? GetCurrentAdminOrStaffId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId!=null)
            {
                var user = _context.Users.AsNoTracking()
                                     .FirstOrDefault(u => u.Id == userId);

                // Giả định RoleId 1 (Admin) và 2 (Staff) có quyền phản hồi
                if (user != null && (user.RoleId == 1 || user.RoleId == 2))
                {
                    return user.Id;
                }
            }
            return null;
        }

        // =======================================================================
        // 1. ACTION: Hiển thị Danh sách Đánh giá (Index)
        // =======================================================================
        public async Task<IActionResult> Index()
        {
            // Lấy tất cả đánh giá, bao gồm thông tin sản phẩm và người đánh giá
            var reviews = await _context.DanhGiaSanPhams
                .Include(r => r.SanPham)
                    .ThenInclude(p => p.HinhAnhSanPhams)// Tên sản phẩm
                .Include(r => r.User)    // Người đánh giá (Khách hàng)
                .Include(r => r.AdminUser) // Người phản hồi (Admin/Staff)
                .OrderByDescending(r => r.NgayDanhGia)
                .ToListAsync();

            // Bạn có thể tạo AdminReviewViewModel nếu cần, nhưng tạm thời dùng List<DanhGiaSanPham>
            return View(reviews);
        }

        // =======================================================================
        // 2. ACTION: Xử lý Gửi Phản hồi (AJAX POST)
        // =======================================================================
        // Sử dụng JSON ViewModel để phản hồi client
        public class AdminJsonResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public object? Data { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> SubmitAdminReply(int reviewId, string replyContent)
        {
            var currentAdminId = GetCurrentAdminOrStaffId();

            if (!currentAdminId.HasValue)
            {
                return Json(new AdminJsonResult { Success = false, Message = "Bạn không có quyền hoặc chưa đăng nhập vào khu vực quản trị." });
            }

            if (string.IsNullOrWhiteSpace(replyContent))
            {
                return Json(new AdminJsonResult { Success = false, Message = "Nội dung phản hồi không được để trống." });
            }

            var reviewToUpdate = await _context.DanhGiaSanPhams.FindAsync(reviewId);

            if (reviewToUpdate == null)
            {
                return Json(new AdminJsonResult { Success = false, Message = "Không tìm thấy đánh giá cần phản hồi." });
            }

            try
            {
                // 1. Cập nhật bản ghi
                reviewToUpdate.PhanHoiAdmin = replyContent.Trim();
                reviewToUpdate.NgayPhanHoi = DateTime.Now;
                reviewToUpdate.AdminUserId = currentAdminId.Value; // Lưu ID người phản hồi

                // 2. Lưu thay đổi vào DB
                await _context.SaveChangesAsync();

                // 3. TRUY VẤN THÔNG TIN ADMIN/STAFF MỚI (AdminUser)
                // Lấy thông tin User đã phản hồi để trả về
                var adminUser = await _context.Users.AsNoTracking()
                                                 .FirstOrDefaultAsync(u => u.Id == currentAdminId.Value);

                // 4. Trả về JSON thành công
                return Json(new AdminJsonResult
                {
                    Success = true,
                    Message = "Phản hồi đã được gửi thành công!",
                    Data = new
                    {
                        phanhoi = reviewToUpdate.PhanHoiAdmin,
                        ngayphanhoi = reviewToUpdate.NgayPhanHoi?.ToString("dd/MM/yyyy HH:mm"),


                        adminname = (adminUser?.Ho ?? "") + " " + (adminUser?.Ten ?? "Admin"),
                        adminavatarurl = adminUser?.AvatarUrl
                    }
                });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (ex)
                return Json(new AdminJsonResult { Success = false, Message = "Lỗi server khi lưu dữ liệu." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> TogglePublishStatus(int id)
        {
            var currentAdminId = GetCurrentAdminOrStaffId();

            if (!currentAdminId.HasValue)
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            var reviewToUpdate = await _context.DanhGiaSanPhams.FindAsync(id);

            if (reviewToUpdate == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá." });
            }

            // Chuyển đổi trạng thái
            reviewToUpdate.IsPublished = !reviewToUpdate.IsPublished;
            await _context.SaveChangesAsync();

            // Trả về trạng thái mới cho JavaScript
            return Json(new
            {
                success = true,
                isPublished = reviewToUpdate.IsPublished,
                message = reviewToUpdate.IsPublished ? "Đã hiện đánh giá thành công." : "Đã ẩn đánh giá thành công."
            });
        }
    }
}