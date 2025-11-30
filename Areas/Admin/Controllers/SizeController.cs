using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class SizeController : Controller
    {
        private readonly DBShopNLContext _context;

        public SizeController(DBShopNLContext context)
        {
            _context = context;
        }

        // ===================================================================
        // 1. INDEX: Hiển thị danh sách Size
        // ===================================================================
        public async Task<IActionResult> Index()
        {
            var sizes = await _context.Sizes
                                      .OrderBy(s => s.TenSize)
                                      .ToListAsync();
            return View(sizes);
        }

        // ===================================================================
        // 2. CREATE: Thêm Size (AJAX POST)
        // ===================================================================
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] Size size)
        {
            if (size == null || string.IsNullOrWhiteSpace(size.TenSize))
            {
                return Json(new { success = false, message = "Tên size không được để trống." });
            }

            // Check trùng tên size (không phân biệt hoa thường)
            var ten = size.TenSize.Trim();
            bool exists = await _context.Sizes
                .AnyAsync(s => s.TenSize.ToLower() == ten.ToLower());

            if (exists)
            {
                return Json(new { success = false, message = "Tên size đã tồn tại." });
            }

            if (ModelState.IsValid)
            {
                size.TenSize = ten;
                _context.Sizes.Add(size);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm size thành công!",
                    data = size
                });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // ===================================================================
        // 3. EDIT: Sửa Size (AJAX POST)
        // ===================================================================
        [HttpPost]
        public async Task<JsonResult> Edit([FromBody] Size size)
        {

            if (size == null || size.Id <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var existing = await _context.Sizes.FindAsync(size.Id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Size không tồn tại." });
            }

            if (string.IsNullOrWhiteSpace(size.TenSize))
            {
                return Json(new { success = false, message = "Tên size không được để trống." });
            }

            var ten = size.TenSize.Trim();

            // Check trùng tên cho size khác
            bool exists = await _context.Sizes
                .AnyAsync(s => s.TenSize.ToLower() == ten.ToLower()
                               && s.Id != size.Id);

            if (exists)
            {
                return Json(new { success = false, message = "Tên size đã tồn tại." });
            }

            if (ModelState.IsValid)
            {
                existing.TenSize = ten;

                _context.Sizes.Update(existing);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật size thành công!",
                    data = existing
                });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        // ===================================================================
        // 4. DELETE: Xóa Size (AJAX POST)
        // ===================================================================
        [HttpPost]
        public async Task<JsonResult> Delete([FromBody] int id)
        {
           
            var size = await _context.Sizes
                                     .Include(s => s.ChiTietSanPhams)
                                     .FirstOrDefaultAsync(s => s.Id == id);

            if (size == null)
            {
                return Json(new { success = false, message = "Size không tồn tại." });
            }

            // Nếu size đã được dùng trong ChiTietSanPham thì có thể không cho xóa (tùy bạn)
            if (size.ChiTietSanPhams != null && size.ChiTietSanPhams.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Size đã được sử dụng cho sản phẩm, không thể xóa."
                });
            }

            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa size thành công!" });
        }
    }
}
