using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models; 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


public class AccountController : Controller
{
    
    // Dùng interface hoặc lớp DbContext thực tế của bạn
    private readonly DBShopNLContext _context;

    // Constructor để Dependency Injection DbContext
    private readonly IWebHostEnvironment _webHostEnvironment;
    public AccountController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    private int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
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

    // ------------------------------------------------------------------
    // [1] HIỂN THỊ FORM LOGIN (GET)
    // ------------------------------------------------------------------
    [HttpGet]
    public IActionResult Login()
    {
        
        if (User.Identity.IsAuthenticated)
        {
            
            return RedirectToAction("Home", "Index");
        }
        return View();
    }

    // ------------------------------------------------------------------
    // [2] XỬ LÝ ĐĂNG NHẬP (POST)
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe)
    {
        // 1. TÌM USER DỰA TRÊN USERNAME (Kiểm tra cả Email hoặc Số điện thoại)
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == username || u.SoDienThoai == username);

        if (user == null)
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        // 2. KIỂM TRA MẬT KHẨU (THAY THẾ BẰNG HÀM HASH THỰC TẾ)
        bool isValidPassword = (password) == user.MatKhau;

        if (!isValidPassword)
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        // 3. XÁC THỰC THÀNH CÔNG -> TẠO CLAIMS VÀ COOKIE AUTHENTICATION

        string userRole = user.Role?.TenRole ?? "KhachHang";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Ten),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, userRole)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = rememberMe };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // -----------------------------------------------------------
        // 4. LƯU THÔNG TIN BỔ SUNG VÀO HTTP SESSION
        // -----------------------------------------------------------
        // Lưu Tên đầy đủ và Role vào Session để dễ truy cập trong View/Controller
        HttpContext.Session.SetString("FullUserName", $"{user.Ho} {user.Ten}");
        HttpContext.Session.SetString("AvartarUrl", $"{user.AvatarUrl}");
        HttpContext.Session.SetInt32("UserId", user.Id);

        HttpContext.Session.SetString("UserRole", userRole);

        // 5. CHUYỂN HƯỚNG DỰA TRÊN ROLE
        if (userRole == "Admin")
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        else if (userRole == "NhanVien")
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    // ------------------------------------------------------------------
    // [3] LOGOUT
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        // Xóa Session (lưu ý: cần phải cấu hình Session Extension Methods)
        HttpContext.Session.Clear();

        // Xóa Cookie xác thực
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
    // [4] HIỂN THỊ FORM REGISTER (GET)
// ------------------------------------------------------------------
[HttpGet]
public IActionResult Register()
{
    return View();
}

    // ------------------------------------------------------------------
    // [5] XỬ LÝ ĐĂNG KÝ (POST)
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.SoDienThoai == model.SoDienThoai);
            if (existingUser != null)
            {
                ModelState.AddModelError("SĐT", "SĐT này đã được sử dụng.");
                return View(model);
            }

            var user = new User
            {
                
                Ho = model.Ho,
                Ten = model.Ten,

                SoDienThoai = model.SoDienThoai,
                Email = $"{model.SoDienThoai}@tamthoi.com",
                MatKhau = model.Password,
                NgayTao = DateTime.UtcNow,
                RoleId = 3
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }

    
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

       
        

        return View(user);
    }



    // POST: Admin/Users/Edit/5

    [HttpPost]
    [ValidateAntiForgeryToken]
    // BƯỚC 1: Sửa chữ ký, thêm IFormFile? avatarFile
    public async Task<IActionResult> Edit([Bind("Id,Ho,Ten,Email,SoDienThoai,MatKhau,NgaySinh,GioiTinh")] User userFormData,
    IFormFile? avatarFile)
    {
        var id = GetCurrentUserId();
        if (id == null)
        {
            return RedirectToAction("Login", "Account");
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
                HttpContext.Session.SetString("FullUserName", $"{userToUpdate.Ho} {userToUpdate.Ten}");
                HttpContext.Session.SetString("AvartarUrl", $"{userToUpdate.AvatarUrl}");
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
                
                return RedirectToAction(nameof(Edit)); // Cập nhật thành công, về trang Index

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
    public IActionResult AccessDenied()
    {
        return View();
    }
}
