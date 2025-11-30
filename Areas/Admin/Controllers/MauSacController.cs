using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class MauSacController : Controller
    {
        private readonly DBShopNLContext _context;

        public MauSacController(DBShopNLContext context)
        {
            _context = context;
        }

        

        // ============================================================
        // 1. INDEX: Danh sách màu
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var mauSacs = await _context.MauSacs
                .OrderBy(m => m.TenMau)
                .ToListAsync();

            
            return View(mauSacs);
        }

        // ============================================================
        // 2. CREATE: Thêm màu (AJAX POST)
        // ============================================================
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] MauSac mau)
        {
            if (mau == null || string.IsNullOrWhiteSpace(mau.TenMau))
            {
                return Json(new { success = false, message = "Tên màu không được để trống." });
            }

            var ten = mau.TenMau.Trim();

            // Check trùng tên
            bool exists = await _context.MauSacs
                .AnyAsync(m => m.TenMau.ToLower() == ten.ToLower());

            if (exists)
            {
                return Json(new { success = false, message = "Tên màu đã tồn tại." });
            }

            // Chuẩn hóa & validate mã HEX (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(mau.MaMauHex))
            {
                var hex = mau.MaMauHex.Trim();

                if (!hex.StartsWith("#"))
                    hex = "#" + hex;

                // #RGB hoặc #RRGGBB
                if (!Regex.IsMatch(hex, "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$"))
                {
                    return Json(new { success = false, message = "Mã màu HEX không hợp lệ. Ví dụ: #ff0000 hoặc #f00" });
                }

                mau.MaMauHex = hex;
            }
            else
            {
                mau.MaMauHex = null;
            }

            if (ModelState.IsValid)
            {
                mau.TenMau = ten;
                _context.MauSacs.Add(mau);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm màu thành công!",
                    data = mau
                });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // ============================================================
        // 3. EDIT: Sửa màu (AJAX POST)
        // ============================================================
        [HttpPost]
        public async Task<JsonResult> Edit([FromBody] MauSac mau)
        {
            if (mau == null || mau.Id <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var existing = await _context.MauSacs.FindAsync(mau.Id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Màu không tồn tại." });
            }

            if (string.IsNullOrWhiteSpace(mau.TenMau))
            {
                return Json(new { success = false, message = "Tên màu không được để trống." });
            }

            var ten = mau.TenMau.Trim();

            // Check trùng tên với màu khác
            bool exists = await _context.MauSacs
                .AnyAsync(m => m.TenMau.ToLower() == ten.ToLower()
                               && m.Id != mau.Id);

            if (exists)
            {
                return Json(new { success = false, message = "Tên màu đã tồn tại." });
            }

            // Validate mã HEX
            string? hexResult = null;
            if (!string.IsNullOrWhiteSpace(mau.MaMauHex))
            {
                var hex = mau.MaMauHex.Trim();
                if (!hex.StartsWith("#"))
                    hex = "#" + hex;

                if (!Regex.IsMatch(hex, "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$"))
                {
                    return Json(new { success = false, message = "Mã màu HEX không hợp lệ. Ví dụ: #ff0000 hoặc #f00" });
                }

                hexResult = hex;
            }

            if (ModelState.IsValid)
            {
                existing.TenMau = ten;
                existing.MaMauHex = hexResult;

                _context.MauSacs.Update(existing);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật màu thành công!",
                    data = existing
                });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // ============================================================
        // 4. DELETE: Xóa màu (AJAX POST)
        // ============================================================
        [HttpPost]
        public async Task<JsonResult> Delete([FromBody] int id)
        {
            var mau = await _context.MauSacs
                .Include(m => m.ChiTietSanPhams)
                .Include(m => m.HinhAnhSanPhams)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mau == null)
            {
                return Json(new { success = false, message = "Màu không tồn tại." });
            }

            // Nếu màu đã dùng trong sản phẩm / hình ảnh thì không cho xóa (tùy quy tắc bạn)
            if ((mau.ChiTietSanPhams != null && mau.ChiTietSanPhams.Any()) ||
                (mau.HinhAnhSanPhams != null && mau.HinhAnhSanPhams.Any()))
            {
                return Json(new
                {
                    success = false,
                    message = "Màu đang được sử dụng cho sản phẩm/hình ảnh, không thể xóa."
                });
            }

            _context.MauSacs.Remove(mau);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa màu thành công!" });
        }
    }
}
