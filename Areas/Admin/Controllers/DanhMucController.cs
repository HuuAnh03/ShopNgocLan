using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class DanhMucController : Controller
    {
        
        private readonly DBShopNLContext _context;

        public DanhMucController(DBShopNLContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            // BƯỚC 1: Tải TẤT CẢ danh mục.
            // Việc này cho phép EF "khớp nối" (fix-up) các mối quan hệ
            // Model.InverseParent sẽ tự động được điền vào.
            var allCategories = await _context.DanhMucSanPhams.ToListAsync();

            // BƯỚC 2: Lọc ra danh sách chỉ chứa các danh mục CẤP CAO NHẤT (gốc)
            // Dùng .Where() của LINQ to Objects (trên danh sách allCategories đã tải)
            var topLevelCategories = allCategories
                                        .Where(c => c.ParentId == null)
                                        .OrderBy(c => c.TenDanhMuc)
                                        .ToList();

            // BƯỚC 3: Trả danh sách cấp cao nhất này về View
            return View(topLevelCategories);
        }
        private async Task<List<int>> GetDescendantCategoryIdsAsync(int categoryId)
        {
            // Lấy các ID con trực tiếp
            var childrenIds = await _context.DanhMucSanPhams
                                            .Where(c => c.ParentId == categoryId)
                                            .Select(c => c.Id)
                                            .ToListAsync();

            var allDescendantIds = new List<int>(childrenIds);

            // Với mỗi con, lại gọi đệ quy để lấy cháu
            foreach (var childId in childrenIds)
            {
                allDescendantIds.AddRange(await GetDescendantCategoryIdsAsync(childId));
            }

            return allDescendantIds;
        }


        // --- SỬA LẠI ACTION DETAILS ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // BƯỚC 1: Lấy danh mục hiện tại và các con TRỰC TIẾP (để hiển thị)
            var danhMucHienTai = await _context.DanhMucSanPhams
                .Include(d => d.Parent)         // Lấy cha
                .Include(d => d.InverseParent)  // Lấy các con
                .FirstOrDefaultAsync(m => m.Id == id);

            if (danhMucHienTai == null)
            {
                return NotFound();
            }

            // BƯỚC 2: (MỚI) Lấy TẤT CẢ ID của con, cháu, chắt...
            List<int> allCategoryIds = await GetDescendantCategoryIdsAsync(id.Value);

            // Thêm cả ID của chính danh mục cha vào danh sách
            allCategoryIds.Add(id.Value);

            // --> allCategoryIds bây giờ là: [1, 5, 6, 7, 8...] (ví dụ)

            // BƯỚC 3: (SỬA) Truy vấn TẤT CẢ sản phẩm có DanhMucId nằm trong danh sách ID ở trên
            var sanPhamsQuery = _context.SanPhams
                .Where(s => allCategoryIds.Contains(s.DanhMucId)) // Logic then chốt là ở đây!
                .Include(p => p.HinhAnhSanPhams)
                .Include(p => p.ChiTietSanPhams)
                    .ThenInclude(ct => ct.Size);

            // BƯỚC 4: (SỬA) Chuyển đổi danh sách sản phẩm vừa truy vấn sang ViewModel
            var sanPhamViewModels = await sanPhamsQuery
                .OrderBy(s => s.TenSanPham)
                .Select(s => new ProductListViewModel
                {
                    Id = s.Id,
                    TenSanPham = s.TenSanPham,
                    IsActive = s.IsActive ?? false,
                    NgayTaoFormatted = s.NgayTao.HasValue ? s.NgayTao.Value.ToString("dd/MM/yyyy") : null,

                    AnhDaiDienUrl = s.HinhAnhSanPhams.FirstOrDefault(h => h.LaAnhDaiDien==true).UrlHinhAnh ??
                                    s.HinhAnhSanPhams.FirstOrDefault().UrlHinhAnh,

                    PriceString = (s.ChiTietSanPhams != null && s.ChiTietSanPhams.Any())
                        ? (s.ChiTietSanPhams.Min(v => v.Gia) == s.ChiTietSanPhams.Max(v => v.Gia)
                            ? s.ChiTietSanPhams.Min(v => v.Gia).ToString("N0") + " đ"
                            : $"{s.ChiTietSanPhams.Min(v => v.Gia):N0} - {s.ChiTietSanPhams.Max(v => v.Gia):N0} đ")
                        : "N/A",

                    SizeString = (s.ChiTietSanPhams != null && s.ChiTietSanPhams.Any())
                        ? string.Join(", ", s.ChiTietSanPhams
                                                .Select(v => v.Size.TenSize)
                                                .Distinct())
                        : "N/A",

                    TotalStock = (s.ChiTietSanPhams != null && s.ChiTietSanPhams.Any())
                        ? s.ChiTietSanPhams.Sum(v => v.SoLuongTon)
                        : 0,

                    // Lấy tên danh mục (của chính sản phẩm đó)
                    // Chúng ta cần Include(p => p.DanhMuc) trong truy vấn ở BƯỚC 3 nếu muốn tên chính xác
                    // Hoặc đơn giản là hiển thị tên danh mục cha (đang xem)
                    TenDanhMuc = danhMucHienTai.TenDanhMuc
                })
                .ToListAsync(); // Thực thi truy vấn

            // BƯỚC 5: Gán vào ViewModel (như cũ)
            var viewModel = new DanhMucViewModel
            {
                DanhMucHienTai = danhMucHienTai,
                DanhSachCon = danhMucHienTai.InverseParent.OrderBy(c => c.TenDanhMuc).ToList(), // Vẫn chỉ là con trực tiếp
                DanhSachSanPham = sanPhamViewModels // (MỚI) Danh sách này giờ đã bao gồm tất cả SP
            };

            return View(viewModel);
        }

        // GET: DanhMucSanPhams/Create
        public async Task<IActionResult> Create()
        {
            // BƯỚC 1: Lấy tất cả danh mục hiện có
            var danhMucList = await _context.DanhMucSanPhams
                                            .OrderBy(d => d.TenDanhMuc)
                                            .ToListAsync();

            // BƯỚC 2: Tạo một SelectList (danh sách <option> cho thẻ <select>)
            //        - "Id" sẽ là giá trị (value) của <option>
            //        - "TenDanhMuc" sẽ là văn bản (text) hiển thị cho <option>
            // Gửi danh sách này sang View để người dùng chọn Danh Mục Cha
            ViewData["ParentId"] = new SelectList(danhMucList, "Id", "TenDanhMuc");

            // BƯỚC 3: Hiển thị View "Create.cshtml"
            return View();
        }

        // POST: DanhMucSanPhams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDanhMuc,ParentId")] DanhMucSanPham danhMucSanPham)
        {
            if (ModelState.IsValid)
            {
                if (danhMucSanPham.ParentId == 0)
                {
                    danhMucSanPham.ParentId = null;
                }

                // --- SỬA ĐỔI BẮT ĐẦU ---

                // B1: Thêm vào context và Lưu lần 1 để lấy ID
                _context.Add(danhMucSanPham);
                await _context.SaveChangesAsync(); // <-- Tại thời điểm này, danhMucSanPham.Id đã có giá trị

                // B2: Tính toán Path
                if (danhMucSanPham.ParentId == null)
                {
                    // Nếu là danh mục gốc, Path là "ID/"
                    danhMucSanPham.Path = danhMucSanPham.Id.ToString() + "/";
                }
                else
                {
                    // Nếu là danh mục con, lấy Path của cha và nối ID của mình vào
                    var parent = await _context.DanhMucSanPhams.FindAsync(danhMucSanPham.ParentId.Value);
                    danhMucSanPham.Path = parent.Path + danhMucSanPham.Id.ToString() + "/";
                }

                // B3: Cập nhật lại entity với Path mới và Lưu lần 2
                _context.Update(danhMucSanPham);
                await _context.SaveChangesAsync();

                // --- KẾT THÚC SỬA ĐỔI ---

                return RedirectToAction(nameof(Index));
            }

            // Tải lại SelectList (phần này giữ nguyên)
            var danhMucList = await _context.DanhMucSanPhams
                                            .OrderBy(d => d.TenDanhMuc)
                                            .ToListAsync();
            ViewData["ParentId"] = new SelectList(danhMucList, "Id", "TenDanhMuc", danhMucSanPham.ParentId);

            return View(danhMucSanPham);
        }
        // GET: DanhMucSanPhams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMucSanPham = await _context.DanhMucSanPhams.FindAsync(id);
            if (danhMucSanPham == null || string.IsNullOrEmpty(danhMucSanPham.Path))
            {
                // Thêm kiểm tra Path để đảm bảo dữ liệu
                return NotFound();
            }

            // --- SỬA ĐỔI LOGIC TÌM CON CHÁU ---

            // B1: Lấy Path của danh mục hiện tại
            string currentPath = danhMucSanPham.Path;

            // B2: Lấy ID của tất cả con cháu (và chính nó) bằng Path.StartsWith()
            var forbiddenIds = await _context.DanhMucSanPhams
                .Where(c => c.Path != null && c.Path.StartsWith(currentPath))
                .Select(c => c.Id)
                .ToListAsync();

            // B3: Lấy danh sách cha hợp lệ (TRỪ các ID cấm)
            var possibleParents = await _context.DanhMucSanPhams
                .Where(c => !forbiddenIds.Contains(c.Id))
                .OrderBy(c => c.TenDanhMuc)
                .ToListAsync();

            // --- KẾT THÚC SỬA ĐỔI ---

            ViewData["ParentId"] = new SelectList(possibleParents, "Id", "TenDanhMuc", danhMucSanPham.ParentId);
            return View(danhMucSanPham);
        }

        // POST: DanhMucSanPhams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TenDanhMuc,ParentId")] DanhMucSanPham danhMucSanPham)
        {
            if (id != danhMucSanPham.Id)
            {
                return NotFound();
            }

            // Lấy trạng thái GỐC của danh mục từ CSDL
            var categoryToUpdate = await _context.DanhMucSanPhams.AsNoTracking()
                                               .FirstOrDefaultAsync(c => c.Id == id);
            if (categoryToUpdate == null)
            {
                return NotFound();
            }

            // Chuẩn hóa ParentId (nếu là 0 thì set về null)
            int? newParentId = (danhMucSanPham.ParentId == 0) ? (int?)null : danhMucSanPham.ParentId;
            danhMucSanPham.ParentId = newParentId;

            // --- SỬA ĐỔI LOGIC VALIDATION (dùng Path) ---
            if (newParentId.HasValue)
            {
                if (newParentId.Value == danhMucSanPham.Id)
                {
                    ModelState.AddModelError("ParentId", "Danh mục không thể tự làm danh mục cha của chính nó.");
                }
                else
                {
                    // Kiểm tra xem "cha mới" có phải là "con" của mình không
                    var newParent = await _context.DanhMucSanPhams.AsNoTracking()
                                                .FirstOrDefaultAsync(c => c.Id == newParentId.Value);

                    if (newParent != null && newParent.Path.StartsWith(categoryToUpdate.Path))
                    {
                        ModelState.AddModelError("ParentId", "Không thể di chuyển danh mục này vào một danh mục con của nó.");
                    }
                }
            }
            // --- KẾT THÚC SỬA ĐỔI VALIDATION ---

            if (ModelState.IsValid)
            {
                try
                {
                    // --- SỬA ĐỔI LOGIC UPDATE PATH (QUAN TRỌNG) ---

                    int? oldParentId = categoryToUpdate.ParentId;
                    string oldPath = categoryToUpdate.Path;

                    // TRƯỜNG HỢP 1: ParentId KHÔNG THAY ĐỔI
                    // (Chỉ đổi Tên)
                    if (oldParentId == newParentId)
                    {
                        // Gắn lại entity và chỉ cập nhật Tên
                        var trackedCategory = await _context.DanhMucSanPhams.FindAsync(id);
                        trackedCategory.TenDanhMuc = danhMucSanPham.TenDanhMuc;
                        _context.Update(trackedCategory);
                        await _context.SaveChangesAsync();
                    }
                    // TRƯỜNG HỢP 2: ParentId CÓ THAY ĐỔI
                    else
                    {
                        // B1: Lấy Path của cha mới
                        string newParentPath = "";
                        if (newParentId.HasValue)
                        {
                            var newParent = await _context.DanhMucSanPhams.FindAsync(newParentId.Value);
                            if (newParent != null)
                            {
                                newParentPath = newParent.Path;
                            }
                        }
                        string newPath = newParentPath + categoryToUpdate.Id + "/";

                        // B2: Lấy TẤT CẢ con cháu (dùng Path cũ)
                        var descendants = await _context.DanhMucSanPhams
                            .Where(c => c.Path.StartsWith(oldPath) && c.Id != id)
                            .ToListAsync();

                        // B3: Cập nhật chính nó (lấy entity đang được theo dõi)
                        var trackedCategory = await _context.DanhMucSanPhams.FindAsync(id);
                        trackedCategory.TenDanhMuc = danhMucSanPham.TenDanhMuc;
                        trackedCategory.ParentId = newParentId;
                        trackedCategory.Path = newPath;
                        _context.Update(trackedCategory);

                        // B4: Cập nhật tất cả con cháu (thay thế phần đầu Path)
                        foreach (var descendant in descendants)
                        {
                            // Lấy phần Path con ("5/6/") từ Path cũ ("1/2/5/6/")
                            string childPathPart = descendant.Path.Substring(oldPath.Length);
                            // Nối vào Path mới
                            descendant.Path = newPath + childPathPart;
                            _context.Update(descendant);
                        }

                        // B5: Lưu tất cả thay đổi trong 1 transaction
                        await _context.SaveChangesAsync();
                    }
                    // --- KẾT THÚC SỬA ĐỔI LOGIC UPDATE ---
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DanhMucSanPhams.Any(e => e.Id == danhMucSanPham.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // NẾU THẤT BẠI: Tải lại SelectList (dùng logic Path giống GET)
            string currentPathOnFail = categoryToUpdate.Path;
            var forbiddenIdsOnFail = await _context.DanhMucSanPhams
                .Where(c => c.Path != null && c.Path.StartsWith(currentPathOnFail))
                .Select(c => c.Id)
                .ToListAsync();
            var possibleParents = await _context.DanhMucSanPhams
                .Where(c => !forbiddenIdsOnFail.Contains(c.Id))
                .OrderBy(c => c.TenDanhMuc)
                .ToListAsync();
            ViewData["ParentId"] = new SelectList(possibleParents, "Id", "TenDanhMuc", danhMucSanPham.ParentId);

            return View(danhMucSanPham);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tải danh mục CÙNG VỚI các con và sản phẩm của nó để kiểm tra
            var danhMucSanPham = await _context.DanhMucSanPhams
                .Include(d => d.Parent)         // Để hiển thị tên cha
                .Include(d => d.InverseParent)  // Để kiểm tra (danh sách con)
                .Include(d => d.SanPhams)       // Để kiểm tra (danh sách sản phẩm)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (danhMucSanPham == null)
            {
                return NotFound();
            }

            // --- KIỂM TRA ĐIỀU KIỆN XÓA ---
            // Kiểm tra xem có danh mục con nào không
            if (danhMucSanPham.InverseParent.Any())
            {
                // Gửi thông báo lỗi sang View
                ViewData["ErrorMessage"] = "Không thể xóa danh mục này vì nó đang chứa các danh mục con. Vui lòng di chuyển hoặc xóa các danh mục con trước.";
            }
            // Kiểm tra xem có sản phẩm nào không
            else if (danhMucSanPham.SanPhams.Any())
            {
                ViewData["ErrorMessage"] = "Không thể xóa danh mục này vì nó đang chứa sản phẩm. Vui lòng di chuyển hoặc xóa các sản phẩm thuộc danh mục này trước.";
            }

            // Trả về View "Delete.cshtml"
            return View(danhMucSanPham);
        }

        // POST: DanhMucSanPhams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Tải lại danh mục và các liên kết để kiểm tra lần cuối
            var danhMucSanPham = await _context.DanhMucSanPhams
                .Include(d => d.InverseParent)
                .Include(d => d.SanPhams)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (danhMucSanPham == null)
            {
                return NotFound();
            }

            // --- KIỂM TRA LẠI PHÍA SERVER (Rất quan trọng) ---
            if (danhMucSanPham.InverseParent.Any())
            {
                // Dùng TempData để gửi lỗi sau khi Redirect
                TempData["ErrorMessage"] = "Lỗi: Vẫn còn danh mục con, không thể xóa.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            if (danhMucSanPham.SanPhams.Any())
            {
                TempData["ErrorMessage"] = "Lỗi: Vẫn còn sản phẩm trong danh mục, không thể xóa.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            // Nếu mọi thứ đều ổn -> Xóa
            _context.DanhMucSanPhams.Remove(danhMucSanPham);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // --- HÀM HỖ TRỢ ---

        // Hàm đệ quy để lấy tất cả các danh mục con, cháu, chắt...
        private async Task<List<DanhMucSanPham>> GetDescendantsAsync(int categoryId)
        {
            var descendants = new List<DanhMucSanPham>();
            var children = await _context.DanhMucSanPhams
                                   .Where(c => c.ParentId == categoryId)
                                   .ToListAsync();

            foreach (var child in children)
            {
                descendants.Add(child);
                descendants.AddRange(await GetDescendantsAsync(child.Id));
            }
            return descendants;
        }
    }
}
