using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using X.PagedList;

[Area("Admin")] 
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly DBShopNLContext _context;

    private readonly IWebHostEnvironment _webHostEnvironment;
    public UsersController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment; 
    }


    // ========== HÀM HELPER: LƯU FILE ẢNH ==========
    private async Task<string> SaveAvatarAsync(IFormFile file)
    {
        // 1. Lấy đường dẫn thư mục gốc wwwroot
        string wwwRootPath = _webHostEnvironment.WebRootPath;

        // 2. Tạo đường dẫn thư mục lưu ảnh (ví dụ: wwwroot/images/avatars)
        string uploadFolder = Path.Combine(wwwRootPath, "images", "avatars");

        // 3. Tạo thư mục nếu nó chưa tồn tại
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        // 4. Tạo một tên file ĐỘC NHẤT (tránh trùng lặp)
        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadFolder, uniqueFileName);

        // 5. Lưu file vào server
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // 6. Trả về đường dẫn web-accessible (để lưu vào CSDL)
        // Ví dụ: /images/avatars/ten_file_doc_nhat.jpg
        return "/images/avatars/" + uniqueFileName;
    }

    // ========== HÀM HELPER: XÓA FILE ẢNH CŨ ==========
    private void DeleteOldAvatar(string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl) || avatarUrl == "/images/avatars/default.jpg")
        {
            return; // Không xóa file default
        }

        try
        {
            // Chuyển URL (ví dụ: /images/...) thành đường dẫn vật lý (ví dụ: C:\project\wwwroot\images\...)
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string physicalPath = Path.Combine(wwwRootPath, avatarUrl.TrimStart('/'));

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }
        catch (Exception)
        {
            // Có thể log lỗi ở đây, nhưng không crash trang
        }
    }
    // URL sẽ là: /Admin/Users/Index
    public async Task<IActionResult> Index(int? page)
    {
        // 1. Cấu hình phân trang
        int pageSize = 6; // Số lượng user trên mỗi trang
        int pageNumber = (page ?? 1); // Nếu 'page' là null, mặc định là trang 1

        // 2. Lấy dữ liệu từ CSDL
        // Rất quan trọng: Thêm .Include(u => u.Role) để code ở View của bạn không bị lỗi
        var usersQuery = _context.Users
                                 .OrderBy(u => u.Ten); // Luôn OrderBy khi phân trang

        PagedList<User> usersList= new PagedList<User>(usersQuery,pageNumber,pageSize);


        // 1. Lấy ngày hiện tại
        var homNay = DateTime.Now;

        // 2. Xác định ngày bắt đầu và kết thúc của tháng này
        var ngayDauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
        var ngayDauThangSau = ngayDauThangNay.AddMonths(1);

        // 3. Xác định ngày bắt đầu và kết thúc của tháng trước
        var ngayDauThangTruoc = ngayDauThangNay.AddMonths(-1);

        // 4. Đếm số lượng người dùng đăng ký trong tháng này
        // Chúng ta dùng NgayTao.Value vì NgayTao là kiểu DateTime? (nullable)
        int nguoiDungThangNay = await _context.Users
            .CountAsync(u => u.NgayTao.HasValue &&
                             u.NgayTao.Value >= ngayDauThangNay &&
                             u.NgayTao.Value < ngayDauThangSau);

        // 5. Đếm số lượng người dùng đăng ký trong tháng trước
        int nguoiDungThangTruoc = await _context.Users
            .CountAsync(u => u.NgayTao.HasValue &&
                             u.NgayTao.Value < ngayDauThangNay);

        // 6. Tính toán phần trăm thay đổi
        double phanTramThayDoi = 0;

            // Công thức: ((Mới - Cũ) / Cũ) * 100
            phanTramThayDoi = ((double)(nguoiDungThangNay - nguoiDungThangTruoc) / nguoiDungThangTruoc) * 100;
        ViewBag.UserPercentageChange = phanTramThayDoi;
        ViewBag.NewUserCount = nguoiDungThangNay; // Gửi thêm nếu bạn muốn hiển thị (ví dụ: +15 người)
        //Khách hàng đã mua hàng
        int khachHangMoiThangNay = await _context.Users
            .CountAsync(u =>
                // Có hóa đơn trong tháng này
                u.HoaDons.Any(h => h.NgayTao >= ngayDauThangNay && h.NgayTao < ngayDauThangSau));

        // 3. Đếm số khách hàng MỚI tháng trước
        // (Là người có hóa đơn trong tháng trước VÀ KHÔNG có bất kỳ hóa đơn nào trước đó nữa)
        int khachHangMoiThangTruoc = await _context.Users
            .CountAsync(u =>
                // Có hóa đơn trong tháng trước
                u.HoaDons.Any(h =>h.NgayTao < ngayDauThangNay)
            );

        // 4. Tính toán phần trăm thay đổi
        double phanTramThayDoiKhachHang = 0;
        if (khachHangMoiThangTruoc > 0)
        {
            phanTramThayDoiKhachHang = ((double)(khachHangMoiThangNay - khachHangMoiThangTruoc) / khachHangMoiThangTruoc) * 100;
        }
        else if (khachHangMoiThangNay > 0)
        {
            phanTramThayDoiKhachHang = 100.0;
        }

        // 5. Gửi dữ liệu qua View
        ViewBag.NewCustomersThisMonth = khachHangMoiThangNay;
        ViewBag.CustomerPercentageChange = phanTramThayDoiKhachHang;
        return View(usersList);
    }
    public async Task<IActionResult> Detail(int id)
    {
        var user = await _context.Users
                             .Include(u => u.DiaChiGiaoHangs) // Ví dụ nếu muốn lấy cả địa chỉ
                             .Include(u => u.Role) // Ví dụ nếu muốn lấy cả địa chỉ
                             .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(); // Không tìm thấy user
        }

        // Trả về View và truyền đối tượng user vào
        // Lấy danh sách tất cả hóa đơn của user này
        var userInvoices = await _context.HoaDons.Where(h => h.UserId == id)
            .Include(h => h.PhuongThucThanhToan)
            .Include(h => h.TrangThai)
            .ToListAsync();

        // Tính toán các chỉ số
        // Tổng số hóa đơn đã tạo (bao gồm tất cả trạng thái)
        ViewBag.TotalInvoices = userInvoices.Count();

        // 1. Tổng chi tiêu (chỉ tính những hóa đơn 'Hoàn thành')
        ViewBag.TotalExpense = userInvoices
            .Where(h => h.TrangThai.MaTrangThai == "Delivered")
            .Sum(h => h.ThanhTien);

        // 2. Đếm số lượng theo từng trạng thái cụ thể
        ViewBag.CompletedOrders = userInvoices
            .Count(h => h.TrangThai.MaTrangThai == "Delivered"); // Hoặc "HoanThanh" nếu bạn dùng mã cũ

        ViewBag.ShippingOrders = userInvoices
            .Count(h => h.TrangThai.MaTrangThai == "Shipped"); // Đang giao (Shipped)

        ViewBag.ProcessingOrders = userInvoices
            .Count(h => h.TrangThai.MaTrangThai == "Processing"); // Đang xử lý

        ViewBag.CancelledOrders = userInvoices
            .Count(h => h.TrangThai.MaTrangThai == "Cancelled"); // Đã hủy
        ViewBag.AwaitingPaymentOrders = userInvoices
    .Count(h => h.TrangThai.MaTrangThai == "AwaitingPayment"); // Chờ thanh toán trực tuyến

        ViewBag.PaymentFailedOrders = userInvoices
            .Count(h => h.TrangThai.MaTrangThai == "PaymentFailed"); // Thanh toán thất bại
        ViewBag.UserInvoices = userInvoices;
        return View(user);
    }

public IActionResult Create()
{
    // BẮT BUỘC: Phải tải danh sách Roles cho dropdown
    // (Nếu bạn "để cứng" 3 <option> trong HTML thì có thể BỎ dòng này)
    ViewBag.Roles = new SelectList(_context.Roles, "Id", "TenRole");

    // Trả về View với một User model rỗng
    return View(new User());
}

    // POST: /Admin/Users/Create
    // Action này XỬ LÝ dữ liệu từ form
    [HttpPost]
    [ValidateAntiForgeryToken]
    // BƯỚC 1: SỬA CHỮ KÝ - Thêm IFormFile? avatarFile
    public async Task<IActionResult> Create(
        [Bind("Ho,Ten,Email,SoDienThoai,MatKhau,NgaySinh,GioiTinh,RoleId")] User user,
        IFormFile? avatarFile) // <-- Thêm tham số này
    {
        // Bỏ qua kiểm tra các thuộc tính không cần thiết
        ModelState.Remove("Id");
        ModelState.Remove("Role");
        ModelState.Remove("DiaChiGiaoHangs");
        ModelState.Remove("HoaDons");
        ModelState.Remove("AvatarUrl"); // Thêm dòng này

        try
        {
            // Kiểm tra xem các trường [Required] đã được nhập chưa
            if (ModelState.IsValid)
            {
                // (!!! QUAN TRỌNG: NHỚ HASH MẬT KHẨU ở đây trước khi lưu !!!)
                // Ví dụ: user.MatKhau = MyPasswordHasher.Hash(user.MatKhau);

                user.NgayTao = DateTime.Now; // Gán ngày tạo

                // BƯỚC 2: THÊM LOGIC LƯU AVATAR
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Gọi hàm helper để lưu file và lấy URL
                    user.AvatarUrl = await SaveAvatarAsync(avatarFile);
                }
                // --- Kết thúc xử lý avatar ---

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
                return RedirectToAction(nameof(Index)); // Chuyển về trang danh sách
            }
        }
        catch (DbUpdateException ex) // Bắt lỗi CSDL (ví dụ trùng Email/SĐT)
        {
            if (ex.InnerException != null && ex.InnerException.Message.Contains("UNIQUE constraint"))
            {
                ModelState.AddModelError("Email", "Email hoặc Số điện thoại này đã tồn tại.");
            }
            else
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào CSDL.");
            }
        }

        // Nếu code chạy đến đây, nghĩa là ModelState.IsValid == false (ví dụ: quên chọn Role)
        // Tải lại ViewBag.Roles trước khi trả về View
        ViewBag.Roles = new SelectList(_context.Roles, "Id", "TenRole", user.RoleId);

        // Trả về View với dữ liệu người dùng đã nhập (user) và các lỗi trong ModelState
        return View(user);
    }

    // (Đảm bảo bạn đã có hàm private Task<string> SaveAvatarAsync(IFormFile file) trong Controller này)
    // Trong file: /Areas/Admin/Controllers/UsersController.cs

    // GET: Admin/Users/Edit/5
    public async Task<IActionResult> Edit(int? id, string returnUrl)
    {
        if (id == null) return NotFound();

        // SỬA Ở ĐÂY: Thêm .Include(u => u.DiaChiGiaoHangs)
        // Để tải user KÈM theo tất cả địa chỉ của họ
        var user = await _context.Users
            .Include(u => u.DiaChiGiaoHangs) // <--- THÊM DÒNG NÀY
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        ViewBag.Roles = new SelectList(_context.Roles, "Id", "TenRole", user.RoleId);
        user.MatKhau = "";
        ViewBag.ReturnUrl = returnUrl;

        return View(user);
    }

    // Giữ nguyên [HttpPost] Edit của User. Nó không cần thay đổi.

    // POST: Admin/Users/Edit/5
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    // BƯỚC 1: Sửa chữ ký, thêm IFormFile? avatarFile
    public async Task<IActionResult> Edit(int id,
    [Bind("Id,Ho,Ten,Email,SoDienThoai,MatKhau,NgaySinh,GioiTinh,RoleId")] User userFormData,
    IFormFile? avatarFile)
    {
        if (id != userFormData.Id)
        {
            return NotFound();
        }

        // Nếu người dùng để trống mật khẩu, chúng ta không muốn validate nó
        if (string.IsNullOrEmpty(userFormData.MatKhau))
        {
            ModelState.Remove("MatKhau"); // Bỏ qua validate [Required] cho MatKhau
        }

        // Bỏ qua validate các thuộc tính điều hướng
        ModelState.Remove("Role");
        ModelState.Remove("DiaChiGiaoHangs");
        ModelState.Remove("HoaDons");
        ModelState.Remove("AvatarUrl"); // Thêm dòng này cho an toàn

        if (ModelState.IsValid)
        {
            try
            {
                // Lấy user GỐC từ CSDL để cập nhật
                var userToUpdate = await _context.Users.FindAsync(id);
                if (userToUpdate == null)
                {
                    return NotFound();
                }

                // --- Cập nhật thông tin từ form ---
                userToUpdate.Ho = userFormData.Ho;
                userToUpdate.Ten = userFormData.Ten;
                userToUpdate.Email = userFormData.Email;
                userToUpdate.SoDienThoai = userFormData.SoDienThoai;
                userToUpdate.NgaySinh = userFormData.NgaySinh;
                userToUpdate.GioiTinh = userFormData.GioiTinh;
                userToUpdate.RoleId = userFormData.RoleId;

                // Xử lý Mật khẩu (như cũ)
                if (!string.IsNullOrEmpty(userFormData.MatKhau))
                {
                    // (BẠN PHẢI HASH MẬT KHẨU MỚI Ở ĐÂY)
                    userToUpdate.MatKhau = userFormData.MatKhau;
                }

                // BƯỚC 2: THÊM LOGIC XỬ LÝ AVATAR
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // 1. Xóa ảnh cũ (nếu có)
                    DeleteOldAvatar(userToUpdate.AvatarUrl);

                    // 2. Lưu ảnh mới và lấy đường dẫn
                    userToUpdate.AvatarUrl = await SaveAvatarAsync(avatarFile);
                }
                // --- Kết thúc xử lý avatar ---

                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("UNIQUE constraint"))
                {
                    ModelState.AddModelError("Email", "Email hoặc Số điện thoại này đã tồn tại.");
                }
                else
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào CSDL.");
                }
            }

            // Kiểm tra lại ModelState (vì khối catch có thể đã thêm lỗi)
            if (ModelState.ErrorCount == 0)
            {
                return RedirectToAction(nameof(Index)); // Cập nhật thành công, về trang Index
            }
        }

        // Nếu code chạy đến đây, nghĩa là ModelState bị lỗi (hoặc có lỗi CSDL)

        // Tải lại ViewBag.Roles
        ViewBag.Roles = new SelectList(_context.Roles, "Id", "TenRole", userFormData.RoleId);

        // Xóa mật khẩu trước khi trả về View
        userFormData.MatKhau = "";

        // BƯỚC 3: SỬA LỖI MẤT ẢNH KHI VALIDATE SAI
        // Lấy lại AvatarUrl cũ, vì userFormData từ form không chứa nó.
        var existingAvatarUrl = await _context.Users
                                    .Where(u => u.Id == userFormData.Id)
                                    .Select(u => u.AvatarUrl)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();
        userFormData.AvatarUrl = existingAvatarUrl;

        return View(userFormData); // Trả về View với dữ liệu đã nhập và thông báo lỗi
    }
    // GET: Admin/Users/Delete/5
    // Action này hiển thị trang xác nhận XÓA
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Lấy thông tin user (kèm cả Role) để hiển thị xác nhận
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Trả về View xác nhận
        return View(user);
    }

    // POST: Admin/Users/Delete/5
    // Action này thực sự XÓA user khỏi CSDL
    [HttpPost, ActionName("Delete")] // Đặt tên "Delete" để khớp với <form asp-action="Delete">
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return RedirectToAction(nameof(Index)); // User đã bị ai đó xóa rồi
        }

        try
        {
            // Thử xóa user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) // Bắt lỗi CSDL
        {
            // Lỗi này xảy ra khi user có Hóa Đơn hoặc Bài Viết (Lỗi khóa ngoại REFERENCE)
            if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
            {
                ModelState.AddModelError("", "Không thể xóa người dùng này. Người dùng này đã có Hóa đơn, Bài viết, hoặc Dữ liệu liên quan khác không thể xóa.");
            }
            else
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi CSDL khi xóa: " + ex.Message);
            }

            // Nếu có lỗi, ta phải tải lại thông tin user (với Role)
            // và trả về trang xác nhận để hiển thị lỗi
            var userForView = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.Id == id);

            return View("Delete", userForView); // Chỉ định rõ trả về View "Delete"
        }
    }
}

