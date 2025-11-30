using Microsoft.AspNetCore.Mvc;
using ShopNgocLan.Models;
using Microsoft.EntityFrameworkCore;

namespace ShopNgocLan.Controllers
{
    [ApiController] // Quan trọng: Đánh dấu đây là API Controller
    [Route("api/[controller]")]
    public class VoucherApiController : ControllerBase
    {
        private readonly DBShopNLContext _context;

        public VoucherApiController(DBShopNLContext context)
        {
            _context = context;
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyVoucher([FromBody] VoucherRequest request)
        {
            var tamtinh = HttpContext.Session.GetDecimal("Subtotal") ?? 0;

            // === SỬA LỖI Ở ĐÂY ===
            // Đọc phí vận chuyển mà người dùng ĐÃ CHỌN (25k hoặc 40k)
            var phiVanChuyenDaChon = HttpContext.Session.GetDecimal("ChosenShippingFee") ?? 25000;

            if (tamtinh == 0)
            {
                return BadRequest(new { success = false, message = "Lỗi: Không tìm thấy giỏ hàng." });
            }

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.MaVoucher == request.Code);

            // ... (Giữ nguyên tất cả các kiểm tra voucher: null, KichHoat, NgayKetThuc...) ...
            if (voucher == null) { /* ... */ }
            if (!voucher.KichHoat) { /* ... */ }
            // ...

            // 5. TÍNH TOÁN LẠI TỪ ĐẦU
            decimal giamGia = 0;

            // === SỬA LỖI Ở ĐÂY ===
            // Phí vận chuyển MỚI ban đầu phải bằng phí NGƯỜI DÙNG CHỌN
            decimal phiVanChuyenMoi = phiVanChuyenDaChon;

            switch (voucher.LoaiGiamGia)
            {
                case 1: // Phần trăm (%)
                    giamGia = tamtinh * (voucher.GiaTriGiam / 100);
                    if (voucher.GiamGiaToiDa.HasValue && giamGia > voucher.GiamGiaToiDa.Value)
                    {
                        giamGia = voucher.GiamGiaToiDa.Value;
                    }
                    // (Phí vận chuyển 'phiVanChuyenMoi' vẫn là 40k)
                    break;
                case 2: // Tiền cố định
                    giamGia = voucher.GiaTriGiam;
                    if (giamGia > tamtinh) giamGia = tamtinh;
                    // (Phí vận chuyển 'phiVanChuyenMoi' vẫn là 40k)
                    break;
                case 3: // Freeship
                    phiVanChuyenMoi = phiVanChuyenMoi-voucher.GiaTriGiam ;
                    if (phiVanChuyenMoi < 0)
                    {
                        phiVanChuyenMoi = 0;
                    }
                    break;
            }

            decimal tongCongMoi = tamtinh - giamGia + phiVanChuyenMoi;

            // 6. LƯU KẾT QUẢ MỚI VÀO SESSION
            HttpContext.Session.SetString("AppliedVoucherCode", voucher.MaVoucher);
            HttpContext.Session.SetDecimal("DiscountAmount", giamGia);
            HttpContext.Session.SetDecimal("ShippingFee", phiVanChuyenMoi); // <-- Lưu phí MỚI
            HttpContext.Session.SetDecimal("Total", tongCongMoi);

            // 7. TRẢ VỀ CHO JAVASCRIPT
            return Ok(new
            {
                success = true,
                message = "Áp dụng mã thành công!",
                tamtinh = tamtinh,
                giamGia = giamGia,
                phiVanChuyen = phiVanChuyenMoi, // <-- Trả về phí mới
                tongCong = tongCongMoi
            });
        }
        public class ShippingRequest
        {
            public string Method { get; set; }
        }

        [HttpPost("update-shipping")]
        public async Task<IActionResult> UpdateShipping([FromBody] ShippingRequest request)
        {
            var tamtinh = HttpContext.Session.GetDecimal("Subtotal") ?? 0;
            var giamGia = HttpContext.Session.GetDecimal("DiscountAmount") ?? 0;
            var appliedVoucherCode = HttpContext.Session.GetString("AppliedVoucherCode");

            // 1. Tính phí vận chuyển MỚI dựa trên LỰA CHỌN
            decimal phiVanChuyenDaChon; // Phí người dùng mới chọn
            if (request.Method == "fast")
            {
                phiVanChuyenDaChon = 40000;
            }
            else
            {
                phiVanChuyenDaChon = 25000;
            }

            // 2. Lưu lại lựa chọn này (quan trọng)
            HttpContext.Session.SetDecimal("ChosenShippingFee", phiVanChuyenDaChon);

            // 3. Tính phí vận chuyển thực tế (có thể bị Freeship ghi đè)
            decimal phiVanChuyenMoi = phiVanChuyenDaChon; // Mặc định là phí đã chọn
            if (!string.IsNullOrEmpty(appliedVoucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.MaVoucher == appliedVoucherCode);

                if (voucher != null && voucher.LoaiGiamGia == 3) // 3 = Freeship
                {
                    phiVanChuyenMoi = phiVanChuyenMoi - voucher.GiaTriGiam;
                    if (phiVanChuyenMoi < 0)
                    {
                        phiVanChuyenMoi = 0;
                    }
                }
            }

            // 4. Tính tổng tiền mới
            decimal tongCongMoi = tamtinh - giamGia + phiVanChuyenMoi;

            // 5. Lưu kết quả cuối cùng
            HttpContext.Session.SetDecimal("ShippingFee", phiVanChuyenMoi); // Phí cuối cùng
            HttpContext.Session.SetDecimal("Total", tongCongMoi);

            // 6. Trả về cho JavaScript
            return Ok(new
            {
                success = true,
                phiVanChuyen = phiVanChuyenMoi,
                tongCong = tongCongMoi
            });
        }
    }
}
