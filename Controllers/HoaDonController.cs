using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using ShopNgocLan.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopNgocLan.Controllers
{
    [Authorize]
    public class HoaDonController : Controller // Giữ tên HoaDonController theo yêu cầu
    {
        private readonly DBShopNLContext _context;

        public HoaDonController(DBShopNLContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        //==================================================================
        // TRANG 1: DANH SÁCH HÓA ĐƠN (LỊCH SỬ ĐẶT HÀNG)
        //==================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            

            // Lấy hóa đơn VÀ INCLUDE BẢNG TRẠNG THÁI MỚI
            var orders = await _context.HoaDons
                .Where(h => h.UserId == userId.Value)
                .Include(h => h.TrangThai) // <-- THAY THẾ CHO CỘT TRANGTHAI CŨ
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            return View(orders);
        }

        //==================================================================
        // TRANG 2: CHI TIẾT CỦA MỘT HÓA ĐƠN
        //==================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id) // 'id' là Mã Hóa Đơn
        {
            var userId = GetCurrentUserId();
            

            // Rất nhiều Include để lấy tất cả thông tin cần hiển thị
            var order = await _context.HoaDons
                .Where(h => h.Id == id && h.UserId == userId.Value)
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.TrangThai) // <-- INCLUDE BẢNG TRẠNG THÁI MỚI
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.ChiTietSanPham)
                        .ThenInclude(ctsp => ctsp.SanPham)
                            .ThenInclude(sp => sp.HinhAnhSanPhams)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.ChiTietSanPham.MauSac)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.ChiTietSanPham.Size)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                // Không tìm thấy đơn hàng, hoặc user cố xem đơn của người khác
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }
    }
}