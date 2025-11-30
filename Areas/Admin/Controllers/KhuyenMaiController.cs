using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class KhuyenMaiController : Controller
    {
        private readonly DBShopNLContext _context;

        public KhuyenMaiController(DBShopNLContext context)
        {
            _context = context;
        }

        // =========================
        // HÀM CẬP NHẬT GIÁ SẢN PHẨM
        // =========================
        private async Task CapNhatGiaChoSanPhamAsync(int sanPhamId)
        {
            // Lấy tất cả biến thể của sản phẩm
            var variants = await _context.ChiTietSanPhams
                .Where(ct => ct.SanPhamId == sanPhamId)
                .ToListAsync();

            // Không có biến thể nào thì thôi
            if (variants == null || !variants.Any())
                return;

            // Đảm bảo đã có Giá Gốc (nếu đang = 0 thì lấy từ Giá hiện tại)
            foreach (var ct in variants)
            {
                if (ct.GiaGoc == 0)
                {
                    ct.GiaGoc = ct.Gia;
                }
            }

            var today = DateTime.Today;

            // Lấy tất cả khuyến mãi đang áp dụng cho sản phẩm này
            // Dùng Select sang KhuyenMai + lọc null để tránh NullReference
            var promos = await _context.SanPhamKhuyenMais
                .Where(x => x.SanPhamId == sanPhamId)
                .Select(x => x.KhuyenMai)
                .Where(km => km != null
                             && km.NgayBatDau <= today
                             && km.NgayKetThuc >= today)
                .ToListAsync();

            if (promos == null || !promos.Any())
            {
                // Không còn khuyến mãi nào -> trả về giá gốc và reset GiaGoc
                foreach (var ct in variants)
                {
                    if (ct.GiaGoc != 0)
                    {
                        ct.Gia = ct.GiaGoc;
                        ct.GiaGoc = 0;
                    }
                }
            }
            else
            {
                // Lấy khuyến mãi có phần trăm giảm cao nhất
                var bestPercent = promos.Max(km => km.PhanTramGiam);

                foreach (var ct in variants)
                {
                    ct.Gia = TinhGiaSauKhuyenMai(ct.GiaGoc, bestPercent);
                }
            }
        }

        // ============ DANH SÁCH ============
        public async Task<IActionResult> Index()
        {
            var list = await _context.KhuyenMais
                .OrderByDescending(km => km.NgayBatDau)
                .ToListAsync();

            return View(list);
        }

        // ============ CHI TIẾT + SẢN PHẨM ============
        public async Task<IActionResult> Details(int id)
        {
            var km = await _context.KhuyenMais
                .Include(k => k.SanPhamKhuyenMais)
                    .ThenInclude(spkm => spkm.SanPham)
                    .ThenInclude(sp => sp.HinhAnhSanPhams)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (km == null)
                return NotFound();

            var sanPhamTrongKm = km.SanPhamKhuyenMais
                .Select(x => x.SanPham)
                .ToList();

            var sanPhamIdsTrongKm = km.SanPhamKhuyenMais
                .Select(x => x.SanPhamId)
                .ToList();

            var sanPhamChuaKm = await _context.SanPhams
                .Include(sp => sp.HinhAnhSanPhams)
                .Where(sp => !sanPhamIdsTrongKm.Contains(sp.Id))
                .OrderBy(sp => sp.TenSanPham)
                .ToListAsync();

            ViewBag.SanPhamTrongKm = sanPhamTrongKm;
            ViewBag.SanPhamChuaKm = sanPhamChuaKm;

            return View(km);
        }

        // ============ HÀM TÍNH GIÁ SAU KM ============
        private decimal TinhGiaSauKhuyenMai(decimal giaGoc, decimal phanTramGiam)
        {
            var giaSauGiam = giaGoc * (1 - phanTramGiam / 100m);
            var giaLamTron = Math.Round(giaSauGiam / 1000m, MidpointRounding.AwayFromZero) * 1000m;
            return giaLamTron;
        }

        // ============ THÊM NHIỀU SẢN PHẨM VÀO KM ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProducts(int khuyenMaiId, int[] sanPhamIds)
        {
            if (sanPhamIds == null || sanPhamIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm.";
                return RedirectToAction(nameof(Details), new { id = khuyenMaiId });
            }

            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.Id == khuyenMaiId);

            if (khuyenMai == null)
                return NotFound();

            // Đảm bảo không trùng
            var distinctIds = sanPhamIds.Distinct().ToList();

            // 1️⃣ Thêm mapping KM–SP (nếu chưa có)
            foreach (var sanPhamId in distinctIds)
            {
                bool exists = await _context.SanPhamKhuyenMais
                    .AnyAsync(x => x.KhuyenMaiId == khuyenMaiId && x.SanPhamId == sanPhamId);

                if (!exists)
                {
                    var spkm = new SanPhamKhuyenMai
                    {
                        KhuyenMaiId = khuyenMaiId,
                        SanPhamId = sanPhamId
                    };
                    _context.SanPhamKhuyenMais.Add(spkm);
                }
            }

            // 2️⃣ Lưu mapping trước, để CapNhatGia thấy đủ dữ liệu trong DB
            await _context.SaveChangesAsync();

            // 3️⃣ Cập nhật giá cho từng sản phẩm
            foreach (var sanPhamId in distinctIds)
            {
                await CapNhatGiaChoSanPhamAsync(sanPhamId);
            }

            // 4️⃣ Lưu các thay đổi giá
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm {distinctIds.Count} sản phẩm vào khuyến mãi.";
            return RedirectToAction(nameof(Details), new { id = khuyenMaiId });
        }

        // ============ XÓA SẢN PHẨM KHỎI KM (AJAX) ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int khuyenMaiId, int sanPhamId)
        {
            try
            {
                var spkm = await _context.SanPhamKhuyenMais
                    .FirstOrDefaultAsync(x => x.KhuyenMaiId == khuyenMaiId && x.SanPhamId == sanPhamId);

                // 1️⃣ Xóa mapping nếu có
                if (spkm != null)
                {
                    _context.SanPhamKhuyenMais.Remove(spkm);
                }

                // 2️⃣ Lưu xóa mapping trước, để DB không còn KM này nữa
                await _context.SaveChangesAsync();

                // 3️⃣ Cập nhật lại giá theo các khuyến mãi còn lại
                await CapNhatGiaChoSanPhamAsync(sanPhamId);

                // 4️⃣ Lưu thay đổi giá
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã gỡ sản phẩm khỏi khuyến mãi và cập nhật lại giá."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        // =========================================
        // 🟢 CREATE = Thêm Khuyến Mãi (modal)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] KhuyenMai model)
        {
            // Validate đơn giản
            if (string.IsNullOrWhiteSpace(model.TenKhuyenMai))
                return Json(new { success = false, message = "Tên khuyến mãi không được bỏ trống." });

            if (model.PhanTramGiam <= 0 || model.PhanTramGiam > 100)
                return Json(new { success = false, message = "Phần trăm giảm phải từ 1 đến 100." });

            if (model.NgayKetThuc < model.NgayBatDau)
                return Json(new { success = false, message = "Ngày kết thúc phải >= ngày bắt đầu." });

            var kmEntity = new KhuyenMai
            {
                TenKhuyenMai = model.TenKhuyenMai,
                MoTa = model.MoTa,
                PhanTramGiam = model.PhanTramGiam,
                NgayBatDau = model.NgayBatDau,
                NgayKetThuc = model.NgayKetThuc
            };

            _context.KhuyenMais.Add(kmEntity);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm khuyến mãi thành công!", id = kmEntity.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] KhuyenMai model)
        {
            // Validate đơn giản
            if (string.IsNullOrWhiteSpace(model.TenKhuyenMai))
                return Json(new { success = false, message = "Tên khuyến mãi không được bỏ trống." });

            if (model.PhanTramGiam <= 0 || model.PhanTramGiam > 100)
                return Json(new { success = false, message = "Phần trăm giảm phải từ 1 đến 100." });

            if (model.NgayKetThuc < model.NgayBatDau)
                return Json(new { success = false, message = "Ngày kết thúc phải >= ngày bắt đầu." });

            var kmEntity = await _context.KhuyenMais
                .Include(k => k.SanPhamKhuyenMais)
                .FirstOrDefaultAsync(k => k.Id == model.Id);

            if (kmEntity == null)
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi cần sửa." });

            kmEntity.TenKhuyenMai = model.TenKhuyenMai;
            kmEntity.MoTa = model.MoTa;
            kmEntity.PhanTramGiam = model.PhanTramGiam;
            kmEntity.NgayBatDau = model.NgayBatDau;
            kmEntity.NgayKetThuc = model.NgayKetThuc;

            await _context.SaveChangesAsync();

            if (kmEntity.SanPhamKhuyenMais != null && kmEntity.SanPhamKhuyenMais.Any())
            {
                var sanPhamIds = kmEntity.SanPhamKhuyenMais
                    .Select(x => x.SanPhamId)
                    .Distinct()
                    .ToList();

                foreach (var spId in sanPhamIds)
                {
                    await CapNhatGiaChoSanPhamAsync(spId);
                }

                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Sửa khuyến mãi thành công!" });
        }


        // =========================================
        // 🔴 XÓA KHUYẾN MÃI (modal + AJAX)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var km = await _context.KhuyenMais
                    .Include(k => k.SanPhamKhuyenMais)
                    .FirstOrDefaultAsync(k => k.Id == id);

                if (km == null)
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi." });

                if (km.SanPhamKhuyenMais != null && km.SanPhamKhuyenMais.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng xóa sản phẩm khỏi khuyến mãi trước khi xóa khuyến mãi."
                    });
                }

                _context.KhuyenMais.Remove(km);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa khuyến mãi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
