using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;


namespace ShopNgocLan.Controllers
{
    [Authorize]
    public class CheckOutController : Controller
    {
        private readonly DBShopNLContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CheckOutController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // === BƯỚC 1: LẤY DANH SÁCH CHỐT THANH TOÁN TỪ SESSION ===
            var itemsList = HttpContext.Session.GetObject<List<CheckoutItemViewModel>>("CheckoutItems");

            // ====== Fallback: nếu CheckoutItems chưa có hoặc bị mất ======
            if (itemsList == null || !itemsList.Any())
            {
                // Lấy toàn bộ giỏ hàng của user từ DB
                var selectedCartItems = await _context.ChiTietGioHangs
                    .Where(c => c.GioHang.UserId == userId.Value)
                    .Include(c => c.ChiTietSanPham.SanPham.HinhAnhSanPhams)
                    .Include(c => c.ChiTietSanPham.MauSac)
                    .Include(c => c.ChiTietSanPham.Size)
                    .ToListAsync();

                if (!selectedCartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                itemsList = new List<CheckoutItemViewModel>();

                foreach (var item in selectedCartItems)
                {
                    var hinhAnh = item.ChiTietSanPham.SanPham.HinhAnhSanPhams
                                       .FirstOrDefault(h => h.LaAnhDaiDien == true);
                    string imageUrl = (hinhAnh != null) ? hinhAnh.UrlHinhAnh : "/images/default-product.png";

                    itemsList.Add(new CheckoutItemViewModel
                    {
                        ChiTietGioHangId = item.ChiTietGioHangId,
                        Quantity = item.SoLuong,
                        VariantId = item.ChiTietSanPhamId,
                        Price = item.ChiTietSanPham.Gia,
                        ProductName = item.ChiTietSanPham.SanPham.TenSanPham,
                        ColorName = item.ChiTietSanPham.MauSac.TenMau,
                        SizeName = item.ChiTietSanPham.Size.TenSize,
                        ImageUrl = imageUrl
                    });
                }

                // Lưu lại vào Session để OrderController.Create dùng
                HttpContext.Session.SetObject("CheckoutItems", itemsList);
            }
            // ====== Hết phần fallback ======

            // Nếu vẫn không có (trường hợp cực đoan) thì coi như giỏ trống
            if (itemsList == null || !itemsList.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn trống hoặc chưa chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // === BƯỚC 2: TÍNH TOÁN TẠM TÍNH / PHÍ SHIP / TỔNG ===
            var tamtinh = itemsList.Sum(i => (decimal)i.Quantity * i.Price);
            decimal phiVanChuyenGoc = 25000m;
            decimal tongCong = tamtinh + phiVanChuyenGoc;

            // === BƯỚC 3: LƯU SESSION CHO ORDER CONTROLLER SỬ DỤNG ===
            HttpContext.Session.SetDecimal("Subtotal", tamtinh);
            HttpContext.Session.SetDecimal("OriginalShippingFee", phiVanChuyenGoc);
            HttpContext.Session.SetDecimal("DiscountAmount", 0);            //_chưa áp voucher
            HttpContext.Session.SetDecimal("ChosenShippingFee", phiVanChuyenGoc);

            // === BƯỚC 4: TẠO VIEWMODEL CHO VIEW CHECKOUT ===
            var viewmodel = new CheckoutViewModel
            {
                HovaTen = user.Ho + " " + user.Ten,
                SoDienThoai = user.SoDienThoai,
                DiaChiGiaoHangs = await _context.DiaChiGiaoHangs
                                        .Where(p => p.UserId == userId.Value)
                                        .ToListAsync(),
                Items = itemsList,
                Tamtinh = tamtinh,
                Ship = phiVanChuyenGoc,
                Tongtien = tongCong
            };

            return View(viewmodel);
        }





    }
}