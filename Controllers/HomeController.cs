using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;

namespace ShopNgocLan.Controllers
{
    public class HomeController : Controller
    {
        private readonly DBShopNLContext _context;

        // Constructor để Dependency Injection DbContext
        public HomeController(DBShopNLContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // Lấy danh sách sản phẩm (chỉ sản phẩm đang Active)
            var sanPhams = _context.SanPhams
                .Include(d => d.DanhGiaSanPhams)
                .Include(d => d.HinhAnhSanPhams)
                .Include(d => d.DanhMuc)
                    .ThenInclude(dm => dm.Parent)
                .Include(d => d.ChiTietSanPhams)
                .Where(sp => sp.IsActive == true)   // nếu IsActive là bool?, có thể thêm sp.IsActive == true
                .ToList();

            // Lấy tất cả danh mục để xử lý cây
            var allCategories = _context.DanhMucSanPhams
                .AsNoTracking()
                .ToList();

            var vm = new HomeViewModel
            {
                SanPhams = sanPhams,
                AllCategories = allCategories
            };

            return View(vm);
        }

    }
}
