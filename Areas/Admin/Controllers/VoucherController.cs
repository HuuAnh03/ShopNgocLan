using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization; // Cần thêm
using System.Threading.Tasks;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VoucherController : Controller
    {
        private readonly DBShopNLContext _context;

        public VoucherController(DBShopNLContext context)
        {
            _context = context;
        }

        // =======================================================================
        // 1. ACTION: Hiển thị Danh sách (INDEX - SSR)
        // =======================================================================
        public async Task<IActionResult> Index()
        {

            var vouchers = await _context.Vouchers.OrderByDescending(v => v.Id).ToListAsync();

            return View(vouchers); 
        }

        // =======================================================================
        // 2. ACTION: THÊM (CREATE - AJAX POST)
        // =======================================================================
        [HttpPost]
        public async Task<JsonResult> Create([FromBody] Voucher voucher)
        {

            if (await _context.Vouchers.AnyAsync(v => v.MaVoucher == voucher.MaVoucher))
            {
                return Json(new { success = false, message = "Mã Voucher đã tồn tại." });
            }

            if (ModelState.IsValid)
            {
                voucher.SoLuotDaSuDung = 0;
                voucher.NgayBatDau = DateTime.Now;

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm Voucher thành công!", data = voucher });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
        }

        // =======================================================================
        // 3. ACTION: SỬA (EDIT - AJAX POST)
        // =======================================================================
        [HttpPost]
        public async Task<JsonResult> Edit([FromBody] Voucher voucher)
        {
            var existingVoucher = await _context.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == voucher.Id);

            if (existingVoucher == null)
            {
                return Json(new { success = false, message = "Voucher không tồn tại." });
            }

            if (await _context.Vouchers.AnyAsync(v => v.MaVoucher == voucher.MaVoucher && v.Id != voucher.Id))
            {
                return Json(new { success = false, message = "Mã Voucher đã tồn tại." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Gán lại các trường do Server quản lý
                    voucher.NgayBatDau = existingVoucher.NgayBatDau;
                    voucher.SoLuotDaSuDung = existingVoucher.SoLuotDaSuDung;

                    _context.Vouchers.Update(voucher);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật Voucher thành công!", data = voucher });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "Lỗi xung đột dữ liệu." });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
        }

        // =======================================================================
        // 4. ACTION: XÓA (DELETE - AJAX POST)
        // =======================================================================
        [HttpPost]
        public async Task<JsonResult> Delete([FromBody] int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Voucher không tồn tại." });
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa Voucher thành công!" });
        }
    }
}