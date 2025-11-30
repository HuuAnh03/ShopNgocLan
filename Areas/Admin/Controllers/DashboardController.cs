using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShopNgocLan.Helpers;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class DashboardController : Controller
    {
        private readonly DBShopNLContext _context;

        public DashboardController(DBShopNLContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            // ======= CARD 1: TOTAL ORDERS (tất cả hóa đơn, kể cả hủy/thất bại) =======
            vm.TotalOrders = await _context.HoaDons.CountAsync();

            // ======= CARD 2: NEW LEADS (user mới trong 30 ngày) =======
            var fromDate = DateTime.Today.AddDays(-30);
            vm.NewLeads = await _context.Users
                .Where(u => u.NgayTao != null && u.NgayTao >= fromDate)
                .CountAsync();

            // ======= HÓA ĐƠN CHỜ XÁC NHẬN (PENDING) =======
            vm.PendingOrders = await _context.HoaDons
                .CountAsync(hd => hd.TrangThaiId == 2
                               || hd.TrangThai.TenTrangThai == "Pending");

            // ======= LỢI NHUẬN (CHỈ ĐƠN HOÀN THÀNH) =======
            // Lợi nhuận = (Đơn giá bán thực tế - Giá nhập) * Số lượng
            vm.Profit = await _context.ChiTietHoaDons
                .Where(ct => ct.HoaDon.TrangThaiId == 6
                          || ct.HoaDon.TrangThai.TenTrangThai == "Hoàn thành"
                          || ct.HoaDon.TrangThai.TenTrangThai == "Delivered")
                .SumAsync(ct => (decimal?)(
                    (ct.DonGia - (ct.ChiTietSanPham.GiaNhap ?? 0m)) * ct.SoLuong
                )) ?? 0m;

            // ======= CARD 3: TOTAL DEALS (đơn HOÀN THÀNH) =======
            vm.TotalDeals = await _context.HoaDons
                .Where(hd => hd.TrangThaiId == 6
                          || hd.TrangThai.TenTrangThai == "Delivered"
                          || hd.TrangThai.TenTrangThai == "Hoàn thành")
                .CountAsync();

            // ======= CARD 4: REVENUE (tổng doanh thu từ đơn HOÀN THÀNH) =======
            vm.Revenue = await _context.HoaDons
                .Where(h => h.TrangThaiId == 6)
                .Select(h => (decimal?)h.ThanhTien)
                .SumAsync() ?? 0m;

            // ======= PERFORMANCE (dummy) =======
            vm.PerformanceData = new List<int> { 50, 65, 70, 80, 95, 130, 150, 145, 160, 170, 190, 210 };

            // ======= TỶ LỆ ĐƠN HOÀN THÀNH TUẦN NÀY / TUẦN TRƯỚC =======
            var startOfThisWeek = DateTime.Now.StartOfWeek(DayOfWeek.Monday);
            var startOfNextWeek = startOfThisWeek.AddDays(7);
            var startOfLastWeek = startOfThisWeek.AddDays(-7);

            var ordersThisWeekQuery = _context.HoaDons
                .Where(h => h.NgayTao != null
                         && h.NgayTao >= startOfThisWeek
                         && h.NgayTao < startOfNextWeek
                         && h.TrangThaiId != 7   // PaymentFailed
                         && h.TrangThaiId != 8); // Cancelled

            var ordersLastWeekQuery = _context.HoaDons
                .Where(h => h.NgayTao != null
                         && h.NgayTao >= startOfLastWeek
                         && h.NgayTao < startOfThisWeek
                         && h.TrangThaiId != 7
                         && h.TrangThaiId != 8);

            var totalThisWeek = await ordersThisWeekQuery.CountAsync();
            var totalLastWeek = await ordersLastWeekQuery.CountAsync();

            var successThisWeek = await ordersThisWeekQuery
                .Where(h => h.TrangThaiId == 6)
                .CountAsync();

            var successLastWeek = await ordersLastWeekQuery
                .Where(h => h.TrangThaiId == 6)
                .CountAsync();

            vm.SessionThisWeek = totalThisWeek == 0
                ? 0
                : (int)Math.Round(100m * successThisWeek / totalThisWeek);

            vm.SessionLastWeek = totalLastWeek == 0
                ? 0
                : (int)Math.Round(100m * successLastWeek / totalLastWeek);

            // ======= CONVERSION DATA CHO RADIAL CHART =======
            vm.ConversionData = new List<int> { vm.SessionThisWeek };

            // ======= TOP PAGES (static demo – nếu không dùng nữa có thể bỏ) =======
            vm.TopPages = new List<TopPageItem>
    {
        new TopPageItem{ PagePath="larkon/ecommerce.html", PageViews=465, ExitRate="4.4%" },
        new TopPageItem{ PagePath="larkon/dashboard.html", PageViews=426, ExitRate="20.4%" },
        new TopPageItem{ PagePath="larkon/chat.html", PageViews=254, ExitRate="12.25%" },
    };

            // ======= RECENT ORDERS =======
            vm.RecentOrders = await _context.HoaDons
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.TrangThai)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.ChiTietSanPham)
                        .ThenInclude(ctsp => ctsp.SanPham)
                            .ThenInclude(sp => sp.HinhAnhSanPhams)
                .OrderByDescending(h => h.NgayTao)
                .Take(5)
                .Select(h => new RecentOrderViewModel
                {
                    OrderId = h.Id,
                    NgayTao = h.NgayTao,
                    KhachHang = h.TenKhachHang ?? (h.User != null ? (h.User.Ho + " " + h.User.Ten) : "Khách lẻ"),
                    Email = h.EmailKhachHang ?? (h.User != null ? h.User.Email : ""),
                    Phone = h.SoDienThoaiKhachHang ?? (h.User != null ? h.User.SoDienThoai : ""),
                    DiaChi = h.DiaChiGiaoHang,
                    Payment = h.PhuongThucThanhToan.TenPhuongThuc,
                    TrangThai = h.TrangThai.TenTrangThai,
                    Image = h.ChiTietHoaDons
                        .Select(ct => ct.ChiTietSanPham.SanPham.HinhAnhSanPhams
                            .Where(a => a.LaAnhDaiDien == true)
                            .Select(a => a.UrlHinhAnh)
                            .FirstOrDefault())
                        .FirstOrDefault()
                })
                .ToListAsync();

            // ======= TOP SẢN PHẨM BÁN CHẠY (đơn hoàn thành) =======
            var topSellingQuery = _context.ChiTietHoaDons
                .Where(ct => ct.HoaDon.TrangThaiId == 6)
                .GroupBy(ct => ct.ChiTietSanPham.SanPhamId)
                .Select(g => new
                {
                    SanPhamId = g.Key,
                    SoLuong = g.Sum(x => x.SoLuong)
                });

            vm.TopSellingProducts = await topSellingQuery
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .Join(
                    _context.SanPhams,
                    g => g.SanPhamId,
                    sp => sp.Id,
                    (g, sp) => new ProductStat
                    {
                        SanPhamId = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        SoLuong = g.SoLuong,
                        Image = sp.HinhAnhSanPhams
                            .Where(a => a.LaAnhDaiDien == true)
                            .Select(a => a.UrlHinhAnh)
                            .FirstOrDefault()
                    }
                )
                .ToListAsync();

            // ======= TOP SẢN PHẨM BÁN CHẬM =======
            var slowSellingQuery = _context.ChiTietHoaDons
                .Where(ct => ct.HoaDon.TrangThaiId == 6)
                .GroupBy(ct => ct.ChiTietSanPham.SanPhamId)
                .Select(g => new
                {
                    SanPhamId = g.Key,
                    SoLuong = g.Sum(x => x.SoLuong)
                });

            vm.SlowSellingProducts = await slowSellingQuery
                .OrderBy(x => x.SoLuong)
                .Take(5)
                .Join(
                    _context.SanPhams,
                    g => g.SanPhamId,
                    sp => sp.Id,
                    (g, sp) => new ProductStat
                    {
                        SanPhamId = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        SoLuong = g.SoLuong,
                        Image = sp.HinhAnhSanPhams
                            .Where(a => a.LaAnhDaiDien == true)
                            .Select(a => a.UrlHinhAnh)
                            .FirstOrDefault()
                    }
                )
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(string type, DateTime? start, DateTime? end)
        {
            // type: "day" | "week" | "month" | "year"

            // Mặc định: 30 ngày gần nhất
            var today = DateTime.Today;
            var from = start?.Date ?? today.AddDays(-30);
            var to = end?.Date.AddDays(1).AddTicks(-1) ?? today.AddDays(1).AddTicks(-1); // end-of-day

            if (from > to)
            {
                var tmp = from;
                from = to;
                to = tmp;
            }

            // Chỉ tính hóa đơn HOÀN THÀNH (Delivered = 6)
            var query = _context.HoaDons
                .Where(h => h.NgayTao != null
                         && h.NgayTao >= from
                         && h.NgayTao <= to
                         && h.TrangThaiId == 6);

            var list = await query.ToListAsync();

            var labels = new List<string>();
            var values = new List<decimal>();

            type = (type ?? "day").ToLower();

            if (type == "day")
            {
                // Nhóm theo ngày
                var grouped = list
                    .GroupBy(h => h.NgayTao!.Value.Date)
                    .OrderBy(g => g.Key);

                foreach (var g in grouped)
                {
                    labels.Add(g.Key.ToString("dd/MM"));
                    values.Add(g.Sum(x => x.ThanhTien));
                }
            }
            else if (type == "week")
            {
                // Nhóm theo tuần (ISO week)
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var grouped = list
                    .GroupBy(h =>
                    {
                        var d = h.NgayTao!.Value;
                        int week = culture.Calendar.GetWeekOfYear(
                            d,
                            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                        return new { d.Year, Week = week };
                    })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week);

                foreach (var g in grouped)
                {
                    labels.Add($"Tuần {g.Key.Week}/{g.Key.Year}");
                    values.Add(g.Sum(x => x.ThanhTien));
                }
            }
            else if (type == "month")
            {
                // Nhóm theo tháng
                var grouped = list
                    .GroupBy(h => new { h.NgayTao!.Value.Year, h.NgayTao!.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

                foreach (var g in grouped)
                {
                    labels.Add($"{g.Key.Month:00}/{g.Key.Year}");
                    values.Add(g.Sum(x => x.ThanhTien));
                }
            }
            else if (type == "year")
            {
                // Nhóm theo năm
                var grouped = list
                    .GroupBy(h => h.NgayTao!.Value.Year)
                    .OrderBy(g => g.Key);

                foreach (var g in grouped)
                {
                    labels.Add(g.Key.ToString());
                    values.Add(g.Sum(x => x.ThanhTien));
                }
            }

            return Json(new
            {
                labels,
                values
            });
        }

    }
}
