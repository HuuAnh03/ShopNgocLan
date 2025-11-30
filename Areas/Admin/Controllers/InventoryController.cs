using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using ShopNgocLan.Models.Inventory;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class InventoryController : Controller
    {
        private readonly DBShopNLContext _context;

        public InventoryController(DBShopNLContext context)
        {
            _context = context;
        }

        // ================== INDEX TỒN KHO ==================
        // Có filter khoảng ngày để thống kê nhập/xuất theo giai đoạn
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            // Nếu không truyền ngày => mặc định 30 ngày gần nhất
            if (!toDate.HasValue)
                toDate = DateTime.Today.AddDays(1);          // đến hết ngày hôm nay

            if (!fromDate.HasValue)
                fromDate = toDate.Value.AddDays(-30);        // 30 ngày trước đó

            // Lưu ra ViewBag để dùng cho input type="date" trên View
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // 1. Lấy toàn bộ biến thể + SP + DM + Màu + Size
            var chiTietList = await _context.ChiTietSanPhams
    .Include(ct => ct.SanPham)
        .ThenInclude(sp => sp.DanhMuc)
    .Include(ct => ct.SanPham)
        .ThenInclude(sp => sp.HinhAnhSanPhams)
    .Include(ct => ct.MauSac)
    .Include(ct => ct.Size)
    .ToListAsync();


            // 2. Gom lịch sử nhập/xuất TOÀN THỜI GIAN
            var lichSuAll = await _context.LichSuTonKhos
                .GroupBy(ls => ls.ChiTietSanPhamId)
                .Select(g => new
                {
                    ChiTietSanPhamId = g.Key,

                    // CHỈ tính nhập kho (Nhập hàng từ NCC)
                    TongNhap = g.Sum(x =>
                        x.LoaiGiaoDich == "NhapHang" && x.SoLuongThayDoi > 0
                            ? x.SoLuongThayDoi
                            : 0),

                    // Xuất = Bán - Hoàn/Hủy (net)
                    TongXuat = g.Sum(x =>
                        x.LoaiGiaoDich == "BanHang"
                            ? (x.SoLuongThayDoi < 0 ? -x.SoLuongThayDoi : x.SoLuongThayDoi)     // cộng vào xuất
                            : (x.LoaiGiaoDich == "HoanHang" || x.LoaiGiaoDich == "HuyDon")
                                ? -(x.SoLuongThayDoi > 0 ? x.SoLuongThayDoi : -x.SoLuongThayDoi) // trừ bớt xuất
                                : 0)
                })
                .ToListAsync();

            // 3. Gom lịch sử nhập/xuất TRONG KHOẢNG NGÀY
            var lichSuTrongKhoang = await _context.LichSuTonKhos
                .Where(ls => ls.NgayTao >= fromDate && ls.NgayTao < toDate)
                .GroupBy(ls => ls.ChiTietSanPhamId)
                .Select(g => new
                {
                    ChiTietSanPhamId = g.Key,

                    TongNhap = g.Sum(x =>
                        x.LoaiGiaoDich == "NhapHang" && x.SoLuongThayDoi > 0
                            ? x.SoLuongThayDoi
                            : 0),

                    TongXuat = g.Sum(x =>
                        x.LoaiGiaoDich == "BanHang"
                            ? (x.SoLuongThayDoi < 0 ? -x.SoLuongThayDoi : x.SoLuongThayDoi)
                            : (x.LoaiGiaoDich == "HoanHang" || x.LoaiGiaoDich == "HuyDon")
                                ? -(x.SoLuongThayDoi > 0 ? x.SoLuongThayDoi : -x.SoLuongThayDoi)
                                : 0)
                })
                .ToListAsync();


            var label = $"{fromDate:dd/MM/yyyy} - {toDate.Value.AddDays(-1):dd/MM/yyyy}";

            // 4. Map sang ViewModel
            var vm = chiTietList.Select(ct =>
            {
                var all = lichSuAll.FirstOrDefault(x => x.ChiTietSanPhamId == ct.Id);
                var range = lichSuTrongKhoang.FirstOrDefault(x => x.ChiTietSanPhamId == ct.Id);

                return new InventoryItemViewModel
                {
                    ChiTietSanPhamId = ct.Id,
                    TenSanPham = ct.SanPham?.TenSanPham ?? string.Empty,
                    DanhMuc = ct.SanPham?.DanhMuc?.TenDanhMuc ?? string.Empty,
                    MauSac = ct.MauSac?.TenMau ?? string.Empty,
                    Size = ct.Size?.TenSize ?? string.Empty,
                    AnhSanPham = ct.SanPham?.HinhAnhSanPhams?.FirstOrDefault()?.UrlHinhAnh ?? "/images/no-image.jpg",

                    SoLuongTonHienTai = ct.SoLuongTon,

                    TongNhapAll = all?.TongNhap ?? 0,
                    TongXuatAll = all?.TongXuat ?? 0,

                    TongNhapTrongKhoang = range?.TongNhap ?? 0,
                    TongXuatTrongKhoang = range?.TongXuat ?? 0,

                    KhoangThoiGianLabel = label
                };
            }).ToList();

            return View(vm);
        }
        public async Task<IActionResult> History(
        DateTime? fromDate,
        DateTime? toDate,
        string? keyword,
        string? loaiGiaoDich)
        {
            // Mặc định 30 ngày gần nhất
            if (!toDate.HasValue)
                toDate = DateTime.Today.AddDays(1); // đến hết hôm nay

            if (!fromDate.HasValue)
                fromDate = toDate.Value.AddDays(-30);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.Keyword = keyword;
            ViewBag.LoaiGiaoDich = loaiGiaoDich;

            var label = $"{fromDate:dd/MM/yyyy} - {toDate.Value.AddDays(-1):dd/MM/yyyy}";

            // Query lịch sử
            var query = _context.LichSuTonKhos
     .Include(ls => ls.ChiTietSanPham)
         .ThenInclude(ct => ct.SanPham)
             .ThenInclude(sp => sp.DanhMuc)
     .Include(ls => ls.ChiTietSanPham)
         .ThenInclude(ct => ct.SanPham)
             .ThenInclude(sp => sp.HinhAnhSanPhams)
     .Include(ls => ls.ChiTietSanPham)
         .ThenInclude(ct => ct.MauSac)
     .Include(ls => ls.ChiTietSanPham)
         .ThenInclude(ct => ct.Size)
     .Include(ls => ls.NhanVien) // nếu navigation tên khác thì đổi lại cho đúng
     .Where(ls => ls.NgayTao >= fromDate && ls.NgayTao < toDate);


            // Lọc theo loại giao dịch
            if (!string.IsNullOrWhiteSpace(loaiGiaoDich))
            {
                query = query.Where(ls => ls.LoaiGiaoDich == loaiGiaoDich);
            }

            // Lọc theo từ khóa (tên sp, danh mục, ghi chú)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(ls =>
                    (ls.ChiTietSanPham.SanPham.TenSanPham.ToLower().Contains(keyword)) ||
                    (ls.ChiTietSanPham.SanPham.DanhMuc.TenDanhMuc.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(ls.GhiChu) && ls.GhiChu.ToLower().Contains(keyword))
                );
            }

            // Order mới nhất trước
            var list = await query
                .OrderByDescending(ls => ls.NgayTao)
                .ToListAsync();

            var vm = list.Select(ls =>
            {
                var ct = ls.ChiTietSanPham;
                var sp = ct?.SanPham;
                var anh = sp?.HinhAnhSanPhams?.FirstOrDefault()?.UrlHinhAnh ?? "/images/no-image.jpg";

                return new InventoryHistoryItemViewModel
                {
                    Id = ls.Id,
                    ChiTietSanPhamId = ls.ChiTietSanPhamId,
                    TenSanPham = sp?.TenSanPham ?? "",
                    DanhMuc = sp?.DanhMuc?.TenDanhMuc ?? "",
                    MauSac = ct?.MauSac?.TenMau ?? "",
                    Size = ct?.Size?.TenSize ?? "",
                    AnhSanPham = anh,
                    SoLuongThayDoi = ls.SoLuongThayDoi,
                    LoaiGiaoDich = ls.LoaiGiaoDich ?? "",
                    GhiChu = ls.GhiChu,
                    NgayTao = ls.NgayTao,
                    NhanVienId = ls.NhanVienId,
                    TenNhanVien = ls.NhanVien?.Ho+" "+ls.NhanVien?.Ten,
                    KhoangThoiGianLabel = label
                };
            }).ToList();

            return View(vm); // Areas/Admin/Views/Inventory/History.cshtml
        }

        // ================== NHẬP KHO (AJAX) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhapKhoAjax([FromBody] NhapKhoRequest request)
        {
            if (request == null || request.ChiTietSanPhamId <= 0)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            if (request.SoLuongNhap <= 0)
                return Json(new { success = false, message = "Số lượng nhập phải > 0." });

            var ct = await _context.ChiTietSanPhams
                .Include(x => x.SanPham)
                .FirstOrDefaultAsync(x => x.Id == request.ChiTietSanPhamId);

            if (ct == null)
                return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm." });

            // 1. Cập nhật tồn hiện tại
            ct.SoLuongTon += request.SoLuongNhap;
            _context.Update(ct);

            // 2. Ghi lịch sử để sau này thống kê
            int? nhanVienId = HttpContext.Session.GetInt32("UserId");

            var log = new LichSuTonKho
            {
                ChiTietSanPhamId = ct.Id,
                SoLuongThayDoi = request.SoLuongNhap, // >0 là nhập
                LoaiGiaoDich = "NhapHang",
                GhiChu = string.IsNullOrWhiteSpace(request.GhiChu)
                            ? $"Nhập kho cho {ct.SanPham?.TenSanPham}"
                            : request.GhiChu,
                NhanVienId = nhanVienId,
                NgayTao = DateTime.Now
            };

            _context.LichSuTonKhos.Add(log);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Nhập kho thành công.",
                newStock = ct.SoLuongTon
            });
        }
    }

    // Request model cho AJAX
    public class NhapKhoRequest
    {
        public int ChiTietSanPhamId { get; set; }
        public int SoLuongNhap { get; set; }
        public string? GhiChu { get; set; }
    }
}
