using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using ShopNgocLan.Models;
using ShopNgocLan.Models.Order;
using ShopNgocLan.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ShopNgocLan.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly DBShopNLContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMomoService _momoService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment, IMomoService momoService, ILogger<OrderController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _momoService = momoService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private async Task HandleDefaultAddressLogic(int userId, DiaChiGiaoHang newDefaultAddress)
        {
            if (newDefaultAddress.LaMacDinh == true)
            {
                var otherDefaultAddresses = await _context.DiaChiGiaoHangs
                  .Where(d => d.UserId == userId && d.Id != newDefaultAddress.Id && d.LaMacDinh == true)
                  .ToListAsync();

                if (otherDefaultAddresses.Any())
                {
                    foreach (var addr in otherDefaultAddresses)
                    {
                        addr.LaMacDinh = false;
                    }
                    _context.UpdateRange(otherDefaultAddresses);
                }
            }
        }

        // Helper: các trạng thái đã TRỪ tồn kho (2..6)
        private bool StatusHasConsumedStock(int trangThaiId)
        {
            // 2: Pending, 3: Confirmed, 4: Processing, 5: Shipped, 6: Delivered
            return trangThaiId >= 2 && trangThaiId <= 6;
        }

        // =========================================================================
        // POST: /Order/Create
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckoutViewModel viewModel)
        {
            // === BƯỚC 1: LẤY DỮ LIỆU CƠ BẢN ===
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var itemsList = HttpContext.Session.GetObject<List<CheckoutItemViewModel>>("CheckoutItems");
            if (itemsList == null || !itemsList.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào để thanh toán.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin tài chính từ Session
            var tamtinh = HttpContext.Session.GetDecimal("Subtotal") ?? 0;
            var giamGia = HttpContext.Session.GetDecimal("DiscountAmount") ?? 0;
            var phiVanChuyen = HttpContext.Session.GetDecimal("ChosenShippingFee") ?? 25000;
            var thanhTienDecimal = tamtinh + phiVanChuyen - giamGia;
            decimal thanhTienMoMo = thanhTienDecimal;

            // === BƯỚC 2: XỬ LÝ ĐỊA CHỈ GIAO HÀNG ===
            DiaChiGiaoHang finalAddress;
            if (viewModel.address_choice == "new")
            {
                var newAddress = viewModel.NewAddress;
                newAddress.UserId = userId.Value;
                await HandleDefaultAddressLogic(userId.Value, newAddress);
                _context.DiaChiGiaoHangs.Add(newAddress);
                finalAddress = newAddress;
            }
            else
            {
                int selectedAddressId = int.Parse(viewModel.address_choice);
                finalAddress = await _context.DiaChiGiaoHangs
                  .AsNoTracking()
                  .FirstOrDefaultAsync(d => d.Id == selectedAddressId && d.UserId == userId.Value);
                if (finalAddress == null)
                {
                    TempData["ErrorMessage"] = "Lỗi: Địa chỉ giao hàng không hợp lệ.";
                    return RedirectToAction("Index", "Checkout");
                }
            }
            if (viewModel.address_choice == "new") await _context.SaveChangesAsync();

            // === BƯỚC 3: TẠO HÓA ĐƠN TẠM THỜI (Lưu vào DB để lấy Id) ===
            int ptttId = 1;
            if (viewModel.payment_method == "qr") ptttId = 2;
            if (viewModel.payment_method == "momo") ptttId = 3;

            // Chỉ MoMo (3) là AwaitingPayment (1), còn lại (COD, QR) là Pending (2)
            int initialStatusId = ptttId == 3 ? 1 : 2; // 1: AwaitingPayment, 2: Pending

            var hoaDon = new HoaDon
            {
                UserId = userId.Value,
                TenKhachHang = user.Ho + " " + user.Ten,
                EmailKhachHang = user.Email,
                SoDienThoaiKhachHang = user.SoDienThoai,
                NgayTao = DateTime.Now,
                TrangThaiId = initialStatusId,
                TongTien = tamtinh,
                PhiVanChuyen = phiVanChuyen,
                GiamGia = giamGia,
                ThanhTien = thanhTienDecimal,
                PhuongThucThanhToanId = ptttId,
                DiaChiGiaoHang = $"{finalAddress.DiaChiChiTiet}, {finalAddress.PhuongXa}, {finalAddress.QuanHuyen}, {finalAddress.TinhThanhPho}"
            };

            foreach (var item in itemsList)
            {
                var productVariant = await _context.ChiTietSanPhams.FindAsync(item.VariantId);
                if (productVariant == null || productVariant.SoLuongTon < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {item.ProductName} không đủ số lượng tồn kho.";
                    return RedirectToAction("Index", "Checkout");
                }

                hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                {
                    ChiTietSanPhamId = item.VariantId,
                    SoLuong = item.Quantity,
                    DonGia = item.Price
                });
            }

            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync(); // để lấy Id hóa đơn

            // === TĂNG SỐ LƯỢT SỬ DỤNG VOUCHER (NẾU CÓ) ===
            var appliedVoucherCode = HttpContext.Session.GetString("AppliedVoucherCode");

            if (!string.IsNullOrEmpty(appliedVoucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.MaVoucher == appliedVoucherCode);

                if (voucher != null)
                {
                    if (!voucher.SoLuotSuDungToiDa.HasValue ||
                        voucher.SoLuotDaSuDung < voucher.SoLuotSuDungToiDa.Value)
                    {
                        voucher.SoLuotDaSuDung++;

                        if (voucher.SoLuotSuDungToiDa.HasValue &&
                            voucher.SoLuotDaSuDung >= voucher.SoLuotSuDungToiDa.Value)
                        {
                            voucher.KichHoat = false;
                        }

                        _context.Vouchers.Update(voucher);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // === BƯỚC 4: XỬ LÝ THEO PHƯƠNG THỨC THANH TOÁN ===
            if (ptttId == 3) // MoMo Payment
            {
                var orderInfo = new OrderInfoModel
                {
                    FullName = user.Ho + " " + user.Ten,
                    Amount = (double)thanhTienMoMo,
                    OrderId = hoaDon.Id.ToString(),
                    OrderInfo = $"Thanh toan don hang {hoaDon.Id} - Shop Ngoc Lan"
                };

                var momoResponse = await _momoService.CreatePaymentAsync(orderInfo);

                ClearPaymentSessions();
                return Redirect(momoResponse.PayUrl);
            }
            else // COD hoặc QR (đã ở trạng thái Pending, được TRỪ TỒN luôn)
            {
                // Giảm tồn kho + log "Bán hàng"
                await UpdateInventoryAndRemoveCartItems(itemsList, userId.Value, hoaDon.Id, "BanHang");
                return RedirectToAction("Details", "HoaDon", new { id = hoaDon.Id });
            }
        }

        // =========================================================================
        // GET: /Order/PaymentCallBack (MoMo)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);

            if (!int.TryParse(response.OrderId, out int hoaDonId))
            {
                TempData["ErrorMessage"] = "Lỗi định dạng OrderId từ MoMo.";
                return RedirectToAction("Index", "Home");
            }

            var hoaDon = await _context.HoaDons
              .Include(h => h.ChiTietHoaDons)
              .FirstOrDefaultAsync(h => h.Id == hoaDonId && h.PhuongThucThanhToanId == 3);

            if (hoaDon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn MoMo hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            // --- BƯỚC 2: KIỂM TRA BẢO MẬT ---
            if (!response.IsVerified)
            {
                _logger.LogError($"MoMo Callback Failed: Signature Mismatch for Order {hoaDonId}");
                TempData["ErrorMessage"] = "Lỗi bảo mật: Phản hồi từ cổng thanh toán không hợp lệ.";

                if (hoaDon.TrangThaiId == 1) hoaDon.TrangThaiId = 7; // PaymentFailed
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "HoaDon", new { id = hoaDonId });
            }

            // THANH TOÁN THÀNH CÔNG
            if (response.ErrorCode == 0)
            {
                if (hoaDon.TrangThaiId == 1) // Chỉ xử lý nếu đang Chờ thanh toán
                {
                    hoaDon.TrangThaiId = 3; // Đã xác nhận
                    await _context.SaveChangesAsync();

                    // Giảm tồn kho + log "BanHangOnline"
                    var items = hoaDon.ChiTietHoaDons
                        .Select(ct => new CheckoutItemViewModel
                        {
                            VariantId = ct.ChiTietSanPhamId,
                            Quantity = ct.SoLuong
                        })
                        .ToList();

                    await UpdateInventoryAndRemoveCartItems(items, hoaDon.UserId.Value, hoaDon.Id, "BanHangOnline");

                    TempData["SuccessMessage"] = $"Thanh toán MoMo thành công cho đơn hàng #{hoaDonId}.";
                }
            }
            else // THANH TOÁN THẤT BẠI
            {
                if (hoaDon.TrangThaiId == 1)
                {
                    hoaDon.TrangThaiId = 7; // Thanh toán thất bại
                    await _context.SaveChangesAsync();
                    TempData["ErrorMessage"] = $"Thanh toán thất bại: {response.Message} (Mã: {response.ErrorCode})";
                }
            }

            return RedirectToAction("Details", "HoaDon", new { id = hoaDonId });
        }

        // =========================================================================
        // POST: /Order/MomoNotify (tạm thời)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> MomoNotify([FromBody] object momoRequest)
        {
            return Json(new { message = "Success" });
        }

        // =========================================================================
        // HÀM HELPER: CLEAR SESSION THANH TOÁN
        // =========================================================================
        private void ClearPaymentSessions()
        {
            HttpContext.Session.Remove("Subtotal");
            HttpContext.Session.Remove("OriginalShippingFee");
            HttpContext.Session.Remove("AppliedVoucherCode");
            HttpContext.Session.Remove("DiscountAmount");
            HttpContext.Session.Remove("ShippingFee");
            HttpContext.Session.Remove("Total");
            HttpContext.Session.Remove("ChosenShippingFee");
        }

        /// <summary>
        /// Giảm tồn kho khi bán + xóa item trong giỏ + ghi LichSuTonKho.
        /// </summary>
        private async Task UpdateInventoryAndRemoveCartItems(
            List<CheckoutItemViewModel> itemsList,
            int userId,
            int hoaDonId,
            string loaiGiaoDich)
        {
            var gioHang = await _context.GioHangs
              .Include(g => g.ChiTietGioHangs)
              .FirstOrDefaultAsync(g => g.UserId == userId);

            var cartItemsToRemove = new List<ChiTietGioHang>();
            bool hasDbChanges = false;

            foreach (var item in itemsList)
            {
                var productVariant = await _context.ChiTietSanPhams.FindAsync(item.VariantId);
                if (productVariant != null)
                {
                    if (productVariant.SoLuongTon >= item.Quantity)
                    {
                        // 1. Giảm tồn kho
                        productVariant.SoLuongTon -= item.Quantity;
                        _context.Update(productVariant);

                        // 2. Ghi Lịch sử tồn kho (số lượng âm vì bán ra)
                        var log = new LichSuTonKho
                        {
                            ChiTietSanPhamId = productVariant.Id,
                            SoLuongThayDoi = -item.Quantity,
                            LoaiGiaoDich = loaiGiaoDich, // "BanHang" / "BanHangOnline"
                            GhiChu = $"Bán hàng từ hóa đơn #{hoaDonId}",
                            // NhanVienId = null (vì khách tự đặt, không phải admin nhập kho)
                            NgayTao = DateTime.Now
                        };
                        _context.LichSuTonKhos.Add(log);

                        hasDbChanges = true;
                    }
                }

                if (gioHang != null)
                {
                    var cartItem = gioHang.ChiTietGioHangs
                      .FirstOrDefault(ctgh => ctgh.ChiTietSanPhamId == item.VariantId);

                    if (cartItem != null)
                    {
                        cartItemsToRemove.Add(cartItem);
                        hasDbChanges = true;
                    }
                }
            }

            if (cartItemsToRemove.Any())
            {
                _context.ChiTietGioHangs.RemoveRange(cartItemsToRemove);
            }

            if (hasDbChanges)
            {
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.Remove("CheckoutItems");
        }

        // =========================================================================
        // POST: /Order/CancelOrder (KHÁCH TỰ HỦY ĐƠN)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] int orderId)
        {
            var userId = GetCurrentUserId();

            // 1. Kiểm tra đăng nhập
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." });
            }

            // 2. Tìm hóa đơn
            var hoaDon = await _context.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.Id == orderId && h.UserId == userId.Value);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy." });
            }

            var oldStatusId = hoaDon.TrangThaiId;

            // 3. Chỉ cho phép hủy khi trạng thái là: 1,2,3,4
            if (oldStatusId != 1 && oldStatusId != 2 &&
                oldStatusId != 3 && oldStatusId != 4)
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng đang ở trạng thái '{hoaDon.TrangThai?.TenTrangThai}', không thể hủy."
                });
            }

            try
            {
                // 4. Cập nhật trạng thái thành Cancelled (ID 8)
                hoaDon.TrangThaiId = 8;
                await _context.SaveChangesAsync();

                // 5. Chỉ hoàn lại tồn kho nếu TRƯỚC ĐÓ đã trừ tồn (2..6)
                if (StatusHasConsumedStock(oldStatusId))
                {
                    await RestoreInventory(hoaDon.Id, "HuyDonKH");
                }

                return Json(new { success = true, message = "Đơn hàng đã được hủy thành công.", newStatusId = 8 });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: Không thể hủy đơn hàng." });
            }
        }

        /// <summary>
        /// Hoàn lại tồn kho khi hủy / hoàn hàng (dùng phía khách).
        /// SoLuongThayDoi dương vì hàng quay lại kho.
        /// </summary>
        private async Task RestoreInventory(int hoaDonId, string loaiGiaoDich)
        {
            var chiTietHoaDon = await _context.ChiTietHoaDons
                .Where(ct => ct.HoaDonId == hoaDonId)
                .ToListAsync();

            foreach (var item in chiTietHoaDon)
            {
                var productVariant = await _context.ChiTietSanPhams.FindAsync(item.ChiTietSanPhamId);
                if (productVariant != null)
                {
                    // 1. Cộng lại tồn kho
                    productVariant.SoLuongTon += item.SoLuong;
                    _context.Update(productVariant);

                    // 2. Ghi log Lịch sử tồn kho (số lượng dương vì trả về kho)
                    var log = new LichSuTonKho
                    {
                        ChiTietSanPhamId = productVariant.Id,
                        SoLuongThayDoi = item.SoLuong,
                        LoaiGiaoDich = loaiGiaoDich, // "HuyDonKH", sau này có thể thêm "HoanHangKH"
                        GhiChu = $"Hoàn lại tồn kho từ hóa đơn #{hoaDonId}",
                        NgayTao = DateTime.Now
                    };
                    _context.LichSuTonKhos.Add(log);
                }
            }

            await _context.SaveChangesAsync();
        }

        // =========================================================================
        // POST: /Order/RequestReturn (KH xin hoàn hàng)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> RequestReturn([FromBody] int orderId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." });
            }

            var hoaDon = await _context.HoaDons
                .Include(h => h.TrangThai)
                .FirstOrDefaultAsync(h => h.Id == orderId && h.UserId == userId.Value);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc bạn không có quyền thao tác." });
            }

            // Chỉ cho phép RequestReturn khi đơn đã Delivered
            if (hoaDon.TrangThai.MaTrangThai != "Delivered")
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng đang ở trạng thái '{hoaDon.TrangThai.TenTrangThai}', không thể yêu cầu hoàn hàng."
                });
            }

            var returnRequestedStatus = await _context.TrangThaiDonHangs
                .FirstOrDefaultAsync(t => t.MaTrangThai == "ReturnRequested");

            if (returnRequestedStatus == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Chưa cấu hình trạng thái 'Yêu cầu hoàn hàng' trong hệ thống. Vui lòng liên hệ quản trị viên."
                });
            }

            try
            {
                hoaDon.TrangThaiId = returnRequestedStatus.Id;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Yêu cầu hoàn hàng đã được gửi. Cửa hàng sẽ liên hệ để hỗ trợ bạn trong thời gian sớm nhất."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi hệ thống: Không thể gửi yêu cầu hoàn hàng lúc này."
                });
            }
        }
    }
}
