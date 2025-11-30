using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Text.Json;
namespace ShopNgocLan.Controllers
{
    public class CartController : Controller
    {   
        private readonly DBShopNLContext _context;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public CartController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public class UpdateCartRequest
        {
            public int ChiTietGioHangId { get; set; }
            public int NewQuantity { get; set; }
        }

        public class RemoveCartRequest
        {
            public int ChiTietGioHangId { get; set; }
        }
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // [POST] /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int variantId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? -1;

            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (userId == -1)
            {
                
                return Json(new { success = false, message = "Bạn cần đăng nhập để thêm vào giỏ hàng." });
            }

            try
            {
                
                var variant = await _context.ChiTietSanPhams.FindAsync(variantId);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                var gioHang = await _context.GioHangs
                    .FirstOrDefaultAsync(g => g.UserId == userId);
                if (gioHang == null)
                {
                    gioHang = new GioHang { UserId = userId };
                    _context.GioHangs.Add(gioHang);
                }

                
                var cartItem = await _context.ChiTietGioHangs
                    .FirstOrDefaultAsync(ct => ct.GioHangId == gioHang.Id && ct.ChiTietSanPhamId == variantId);

                if (cartItem != null)
                {
                    
                    int soLuongMoi = cartItem.SoLuong + quantity;
                    if (variant.SoLuongTon < soLuongMoi)
                    {
                        return Json(new { success = false, message = $"Số lượng tồn kho không đủ (đã có {cartItem.SoLuong} sản phẩm trong giỏ)." });
                    }
                    cartItem.SoLuong = soLuongMoi;
                }
                else
                {
                    
                    if (variant.SoLuongTon < quantity)
                    {
                        return Json(new { success = false, message = $"Số lượng tồn kho không đủ (chỉ còn {variant.SoLuongTon})." });
                    }
                    cartItem = new ChiTietGioHang
                    {
                        GioHang = gioHang,
                        ChiTietSanPhamId = variantId,
                        SoLuong = quantity
                    };
                    _context.ChiTietGioHangs.Add(cartItem);
                }

                
                await _context.SaveChangesAsync();

               
                return ViewComponent("AddToCart");
                
            }
            catch (Exception ex)
            {
                
                return BadRequest("Lỗi hệ thống: " + ex.Message);
            }
        }
        // [POST] /Cart/BuyNow
        [HttpPost]
        public async Task<IActionResult> BuyNow(int variantId, int quantity)
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId") ?? -1;
            if (userId == -1)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để mua hàng." });
            }

            // 2. Lấy thông tin sản phẩm từ CSDL
            var product = await _context.ChiTietSanPhams.FindAsync(variantId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            
            if (quantity > product.SoLuongTon)
            {
                // Trả về thông báo lỗi cụ thể cho người dùng
                return Json(new { success = false, message = $"Số lượng tồn kho chỉ còn {product.SoLuongTon} sản phẩm." });
            }
            
            var gioHang = await _context.GioHangs
                    .FirstOrDefaultAsync(g => g.UserId == userId);
            if (gioHang == null)
            {
                gioHang = new GioHang { UserId = userId };
                _context.GioHangs.Add(gioHang);
            }


            var cartItem = await _context.ChiTietGioHangs
                .FirstOrDefaultAsync(ct => ct.GioHangId == gioHang.Id && ct.ChiTietSanPhamId == variantId);

            if (cartItem != null)
            {

                int soLuongMoi = cartItem.SoLuong + quantity;
                if (product.SoLuongTon < soLuongMoi)
                {
                    return Json(new { success = false, message = $"Số lượng tồn kho không đủ (đã có {cartItem.SoLuong} sản phẩm trong giỏ)." });
                }
                cartItem.SoLuong = soLuongMoi;
            }
            else
            {

                if (product.SoLuongTon < quantity)
                {
                    return Json(new { success = false, message = $"Số lượng tồn kho không đủ (chỉ còn {product.SoLuongTon})." });
                }
                cartItem = new ChiTietGioHang
                {
                    GioHang = gioHang,
                    ChiTietSanPhamId = variantId,
                    SoLuong = quantity
                };
                _context.ChiTietGioHangs.Add(cartItem);
            }


            await _context.SaveChangesAsync();
            var itemToCheckout = await _context.ChiTietGioHangs
        .Where(c => c.ChiTietGioHangId == cartItem.ChiTietGioHangId) // Dùng ID vừa thao tác
        .Include(c => c.ChiTietSanPham.SanPham.HinhAnhSanPhams)
        .Include(c => c.ChiTietSanPham.MauSac)
        .Include(c => c.ChiTietSanPham.Size)
        .FirstOrDefaultAsync();

            if (itemToCheckout == null)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi lấy chi tiết sản phẩm." });
            }

            // --- BƯỚC QUAN TRỌNG: CHUẨN BỊ SESSION CHO CHỈ 1 ITEM ---

            // Tái sử dụng logic chuyển đổi sang CheckoutItemViewModel (từ PrepareCheckout)
            var hinhAnh = itemToCheckout.ChiTietSanPham.SanPham.HinhAnhSanPhams
                                             .FirstOrDefault(h => h.LaAnhDaiDien == true);
            string imageUrl = (hinhAnh != null) ? hinhAnh.UrlHinhAnh : "/images/default-product.png";

            var itemsList = new List<CheckoutItemViewModel>
    {
        new CheckoutItemViewModel
        {
            ChiTietGioHangId = itemToCheckout.ChiTietGioHangId,
            Quantity = itemToCheckout.SoLuong,
            VariantId = itemToCheckout.ChiTietSanPhamId,
            Price = itemToCheckout.ChiTietSanPham.Gia,
            ProductName = itemToCheckout.ChiTietSanPham.SanPham.TenSanPham,
            ColorName = itemToCheckout.ChiTietSanPham.MauSac.TenMau,
            SizeName = itemToCheckout.ChiTietSanPham.Size.TenSize,
            ImageUrl = imageUrl
        }
    };

            // Lưu DANH SÁCH CHỈ MỘT ITEM này vào Session.
            // LƯU Ý: Điều này ghi đè lên bất kỳ CheckoutItems nào đã được lưu từ giỏ hàng.
            HttpContext.Session.SetObject("CheckoutItems", itemsList);


            return Json(new { success = true, redirectUrl = "/Checkout/Index" });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.ChiTietGioHangs
                .Where(c => c.GioHang.UserId == userId.Value)
                .Select(c => new ChiTietGioHang // Chỉ lấy các trường cần thiết
                {
                    ChiTietGioHangId = c.ChiTietGioHangId,
                    SoLuong = c.SoLuong,
                    ChiTietSanPhamId = c.ChiTietSanPhamId,
                    ChiTietSanPham = new ChiTietSanPham
                    {
                        Gia = c.ChiTietSanPham.Gia,
                        SanPham = new SanPham
                        {   Id= c.ChiTietSanPham.SanPham.Id,
                            TenSanPham = c.ChiTietSanPham.SanPham.TenSanPham,
                            HinhAnhSanPhams = c.ChiTietSanPham.SanPham.HinhAnhSanPhams.Where(h => h.LaAnhDaiDien == true).ToList()
                        },
                        MauSac = c.ChiTietSanPham.MauSac,
                        Size = c.ChiTietSanPham.Size
                    }
                })
                .ToListAsync();

            return View(cartItems);
        }

        //==================================================================
        // SỬA GIỎ HÀNG (UPDATE) - SỬA LẠI ĐỂ TRẢ VỀ JSON
        //==================================================================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            var cartItem = await _context.ChiTietGioHangs
                .Include(c => c.GioHang)
                .Include(c => c.ChiTietSanPham)
                .FirstOrDefaultAsync(c =>
                    c.ChiTietGioHangId == request.ChiTietGioHangId &&
                    c.GioHang.UserId == userId.Value);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            if (request.NewQuantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
            }

            if (request.NewQuantity > cartItem.ChiTietSanPham.SoLuongTon)
            {
                return Json(new { success = false, message = $"Số lượng tồn kho không đủ (chỉ còn {cartItem.ChiTietSanPham.SoLuongTon}).", newQuantity = cartItem.SoLuong });
            }

            cartItem.SoLuong = request.NewQuantity;
            await _context.SaveChangesAsync();

            // Trả về Tạm tính MỚI của CHỈ RIÊNG item này
            decimal itemSubtotal = cartItem.SoLuong * cartItem.ChiTietSanPham.Gia;

            return Json(new
            {
                success = true,
                itemSubtotal = itemSubtotal.ToString("N0") + "đ"
            });
        }

        //==================================================================
        // XÓA KHỎI GIỎ HÀNG (DELETE) - SỬA LẠI ĐỂ TRẢ VỀ JSON
        //==================================================================
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveCartRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            var cartItem = await _context.ChiTietGioHangs
                .Include(c => c.GioHang)
                .FirstOrDefaultAsync(c =>
                    c.ChiTietGioHangId == request.ChiTietGioHangId &&
                    c.GioHang.UserId == userId.Value);

            if (cartItem != null)
            {
                _context.ChiTietGioHangs.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        //==================================================================
        // [ACTION MỚI] LẤY TÓM TẮT ĐƠN HÀNG (Dựa trên ID đã chọn)
        //==================================================================
        [HttpPost]
        public async Task<IActionResult> GetCartSummary([FromBody] List<int> selectedIds)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            if (selectedIds == null || !selectedIds.Any())
            {
                // Nếu không chọn gì, trả về 0
                return Ok(new
                {
                    subtotal = "0đ",
                    shipping = "0đ",
                    total = "0đ",
                    checkoutEnabled = false
                });
            }

            // Tính Tạm tính CHỈ TỪ các ID đã chọn
            var tamTinh = await _context.ChiTietGioHangs
                .Where(c => c.GioHang.UserId == userId.Value && selectedIds.Contains(c.ChiTietGioHangId))
                .SumAsync(c => c.SoLuong * c.ChiTietSanPham.Gia);

            decimal phiVanChuyen = (tamTinh > 0) ? 25000 : 0;
            decimal tongCong = tamTinh + phiVanChuyen;

            return Ok(new
            {
                subtotal = tamTinh.ToString("N0") + "đ",
                shipping = phiVanChuyen.ToString("N0") + "đ",
                total = tongCong.ToString("N0") + "đ",
                checkoutEnabled = (tamTinh > 0) // Chỉ cho thanh toán khi có > 0
            });
        }

        //==================================================================
        // [ACTION MỚI] CHUẨN BỊ THANH TOÁN (Lưu vào Session)
        //==================================================================
        [HttpPost]
        public async Task<IActionResult> PrepareCheckout([FromBody] List<int> selectedIds)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, redirectUrl = "/Account/Login" });
            }

            if (selectedIds == null || !selectedIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm." });
            }

            // Lấy các mục đã chọn từ DB để tạo "itemsList"
            var selectedCartItems = await _context.ChiTietGioHangs
                .Where(c => c.GioHang.UserId == userId.Value && selectedIds.Contains(c.ChiTietGioHangId))
                .Include(c => c.ChiTietSanPham.SanPham.HinhAnhSanPhams)
                .Include(c => c.ChiTietSanPham.MauSac)
                .Include(c => c.ChiTietSanPham.Size)
                .ToListAsync();

            var itemsList = new List<CheckoutItemViewModel>();
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

            // Lưu danh sách chốt vào Session
            HttpContext.Session.SetObject("CheckoutItems", itemsList);

            // Xóa CTGHId (của Mua ngay) để tránh xung đột
            HttpContext.Session.Remove("CTGHId");

            return Json(new { success = true, redirectUrl = "/CheckOut/Index" });
        }

        
        
    }
}


