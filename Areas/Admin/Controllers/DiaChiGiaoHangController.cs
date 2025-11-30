using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models; // Đảm bảo đúng namespace
using System.Linq;
using System.Threading.Tasks;

[Area("Admin")] // Đặt đúng Area
[Authorize(Roles = "Admin")]
public class DiaChiGiaoHangController : Controller
{
    private readonly DBShopNLContext _context;

    public DiaChiGiaoHangController(DBShopNLContext context)
    {
        _context = context;
    }

    // ========== HÀM XỬ LÝ "CHỌN MẶC ĐỊNH" (PHIÊN BẢN NÂNG CẤP) ==========
    private async Task SetDefaultAddressAndUpdate(DiaChiGiaoHang diaChiFromForm)
    {
        // 1. Lấy TẤT CẢ địa chỉ của user này
        // Dùng AsNoTracking() để tránh lỗi tracking
        var allAddresses = await _context.DiaChiGiaoHangs
            .Where(d => d.UserId == diaChiFromForm.UserId)
            .AsNoTracking()
            .ToListAsync();

        // 2. Dùng vòng lặp để cập nhật
        foreach (var addr in allAddresses)
        {
            if (addr.Id == diaChiFromForm.Id)
            {
                // Đây là địa chỉ đang được sửa -> Cập nhật TẤT CẢ thông tin từ form
                addr.DiaChiChiTiet = diaChiFromForm.DiaChiChiTiet;
                addr.PhuongXa = diaChiFromForm.PhuongXa;
                addr.QuanHuyen = diaChiFromForm.QuanHuyen;
                addr.TinhThanhPho = diaChiFromForm.TinhThanhPho;
                addr.LaMacDinh = true; // Bắt buộc là true vì ta đang ở trong hàm "Set Default"
            }
            else
            {
                // Đây là các địa chỉ CŨ -> Set LaMacDinh = false
                addr.LaMacDinh = false;
            }
        }

        // 3. Cập nhật TẤT CẢ vào CSDL
        _context.UpdateRange(allAddresses);
    }

    // GET: /Admin/DiaChiGiaoHang/Create?userId=5
    public IActionResult Create(int userId)
    {
        var diaChi = new DiaChiGiaoHang { UserId = userId };
        return View(diaChi);
    }

    // POST: /Admin/DiaChiGiaoHang/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiaChiGiaoHang diaChi)
    {
        ModelState.Remove("User"); // Bỏ qua validate User object
        if (ModelState.IsValid)
        {
            // Xử lý logic Mặc định (nếu được tick)
            if (diaChi.LaMacDinh == true)
            {
                // Bỏ mặc định tất cả địa chỉ cũ (chưa lưu)
                var otherAddresses = await _context.DiaChiGiaoHangs
                    .Where(d => d.UserId == diaChi.UserId && d.LaMacDinh == true)
                    .ToListAsync();
                otherAddresses.ForEach(a => a.LaMacDinh = false);
                _context.UpdateRange(otherAddresses);
            }

            _context.Add(diaChi);
            await _context.SaveChangesAsync();

            // Quay lại trang Edit của User cha
            return RedirectToAction("Edit", "Users", new { id = diaChi.UserId });
        }
        return View(diaChi);
    }

    // GET: /Admin/DiaChiGiaoHang/Edit/123 (với 123 là Id của địa chỉ)
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var diaChi = await _context.DiaChiGiaoHangs.FindAsync(id);
        if (diaChi == null) return NotFound();
        return View(diaChi);
    }

    // POST: /Admin/DiaChiGiaoHang/Edit/123
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DiaChiGiaoHang diaChi)
    {
        if (id != diaChi.Id) return NotFound();
        ModelState.Remove("User");

        if (ModelState.IsValid)
        {
            try
            {
                // Tách 2 trường hợp:
                if (diaChi.LaMacDinh == true)
                {
                    // TRƯỜNG HỢP 1: User tick "Mặc định"
                    // Gọi hàm nâng cấp (hàm này tự SaveChanges)
                    await SetDefaultAddressAndUpdate(diaChi);
                }
                else
                {
                    // TRƯỜNG HỢP 2: User KHÔNG tick "Mặc định"
                    // Chỉ cần cập nhật 1 mình địa chỉ này
                    _context.Update(diaChi);
                }

                // Lưu thay đổi (SaveChangesAsync)
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                //... (xử lý lỗi)
                ModelState.AddModelError("", "Không thể lưu thay đổi.");
                return View(diaChi); // Trả về view nếu lỗi
            }

            // Quay lại trang Edit của User cha
            return RedirectToAction("Edit", "Users", new { id = diaChi.UserId });
        }

        // Nếu ModelState không hợp lệ
        return View(diaChi);
    }

    // GET: /Admin/DiaChiGiaoHang/Delete/123
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var diaChi = await _context.DiaChiGiaoHangs
            .Include(d => d.User) // Lấy thông tin User để biết tên
            .FirstOrDefaultAsync(d => d.Id == id);
        if (diaChi == null) return NotFound();

        return View(diaChi);
    }

    // POST: /Admin/DiaChiGiaoHang/Delete/123
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var diaChi = await _context.DiaChiGiaoHangs.FindAsync(id);
        if (diaChi != null)
        {
            _context.DiaChiGiaoHangs.Remove(diaChi);
            await _context.SaveChangesAsync();
        }
        // Quay lại trang Edit của User cha
        return RedirectToAction("Edit", "Users", new { id = diaChi.UserId });
    }
}