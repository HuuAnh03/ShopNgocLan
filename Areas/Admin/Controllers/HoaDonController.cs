using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Hubs;
using ShopNgocLan.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class HoaDonController : Controller
    {
        private readonly DBShopNLContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public HoaDonController(DBShopNLContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Helper: các trạng thái đã TRỪ tồn kho (2..6)
        private bool StatusHasConsumedStock(int trangThaiId)
        {
            return trangThaiId >= 2 && trangThaiId <= 6;
        }

        // ---------------------------------------------------------------------
        // 1. DANH SÁCH HÓA ĐƠN (READ - LIST)
        // ---------------------------------------------------------------------
        public async Task<IActionResult> Index(string searchQuery)
        {
            var hoaDons = _context.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                string search = searchQuery.ToLower().Trim();

                hoaDons = hoaDons.Where(h =>
                    h.Id.ToString().Contains(search) ||
                    h.TenKhachHang.ToLower().Contains(search) ||
                    (h.User != null && (h.User.Ho + " " + h.User.Ten).ToLower().Contains(search)) ||
                    (h.TrangThai != null && h.TrangThai.TenTrangThai.ToLower().Contains(search))
                );

                ViewBag.CurrentSearch = searchQuery;
            }

            var hoaDonList = await hoaDons
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            return View(hoaDonList);
        }

        // Chỉ hiển thị đơn hàng đang "Pending"
        public async Task<IActionResult> IndexPending()
        {
            var hoaDons = _context.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.User)
                .Where(h => h.TrangThai != null && h.TrangThai.MaTrangThai == "Pending");

            var hoaDonList = await hoaDons
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            return View(hoaDonList);
        }

        // ---------------------------------------------------------------------
        // 2. CHI TIẾT HÓA ĐƠN (READ - DETAIL)
        // ---------------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _context.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.User)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.ChiTietSanPham)
                        .ThenInclude(csp => csp.SanPham)
                            .ThenInclude(sp => sp.HinhAnhSanPhams)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.ChiTietSanPham)
                        .ThenInclude(csp => csp.MauSac)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.ChiTietSanPham)
                        .ThenInclude(csp => csp.Size)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hoaDon == null) return NotFound();

            ViewBag.TrangThais = await _context.TrangThaiDonHangs
                .OrderBy(t => t.ThuTu)
                .ToListAsync();

            return View(hoaDon);
        }

        // ---------------------------------------------------------------------
        // 3. CẬP NHẬT TRẠNG THÁI (AJAX) + TRỪ/CỘNG TỒN KHO + LOG
        // ---------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int newStatusId)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.TrangThai)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
            }

            var newStatus = await _context.TrangThaiDonHangs.FindAsync(newStatusId);
            if (newStatus == null)
            {
                return Json(new { success = false, message = "Trạng thái mới không hợp lệ." });
            }

            var oldStatusId = hoaDon.TrangThaiId;
            if (oldStatusId == newStatusId)
            {
                return Json(new { success = false, message = "Trạng thái đơn hàng không thay đổi." });
            }

            bool oldConsumed = StatusHasConsumedStock(oldStatusId);
            bool newConsumed = StatusHasConsumedStock(newStatusId);

            bool needRestoreInventory = false;
            bool needDeductInventory = false;
            string? loaiGiaoDichTonKho = null;

            // false -> true : chưa trừ -> bắt đầu trừ
            if (!oldConsumed && newConsumed)
            {
                needDeductInventory = true;
                loaiGiaoDichTonKho = "DieuChinhTrangThai_Admin";
            }
            // true -> false : đã trừ -> trả lại kho
            else if (oldConsumed && !newConsumed)
            {
                needRestoreInventory = true;
                loaiGiaoDichTonKho = newStatus.MaTrangThai switch
                {
                    "Cancelled" => "HuyDon_Admin",
                    "Returned" => "HoanHang_Admin",
                    "PaymentFailed" => "ThanhToanThatBai_Admin",
                    _ => "DieuChinhTrangThai_Admin"
                };
            }

            hoaDon.TrangThaiId = newStatusId;

            try
            {
                await _context.SaveChangesAsync();

                if (needDeductInventory && loaiGiaoDichTonKho != null)
                {
                    await DeductInventoryForOrder(hoaDon.Id, loaiGiaoDichTonKho);
                }

                if (needRestoreInventory && loaiGiaoDichTonKho != null)
                {
                    await RestoreInventory(hoaDon.Id, loaiGiaoDichTonKho);
                }

                // Gửi thông báo chat cho khách
                await SendOrderStatusNotificationAsync(hoaDon);

                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật trạng thái đơn hàng #{id} thành công.",
                    newStatusName = newStatus.TenTrangThai
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Lỗi đồng bộ dữ liệu. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Nút xác nhận đơn riêng (nếu m vẫn dùng)
        [HttpPost]
        public async Task<IActionResult> XacNhanDon(int id)
        {
            var hoaDon = await _context.HoaDons.FindAsync(id);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
            }

            hoaDon.TrangThaiId = 3; // ví dụ: 3 = Đã xác nhận

            try
            {
                await _context.SaveChangesAsync();

                await SendOrderStatusNotificationAsync(hoaDon);

                var newStatus = await _context.TrangThaiDonHangs.FindAsync(3);

                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật trạng thái đơn hàng #{id} thành công.",
                    newStatusName = newStatus?.TenTrangThai
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Lỗi đồng bộ dữ liệu. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ---------------------------------------------------------------------
        // 4. XÓA (DELETE) + KHÔI PHỤC TỒN KHO + LOG
        // ---------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng để xóa." });
            }

            try
            {
                // 1. Chỉ khôi phục tồn kho nếu trạng thái hiện tại là trạng thái đã TRỪ tồn
                if (StatusHasConsumedStock(hoaDon.TrangThaiId))
                {
                    await RestoreInventory(hoaDon.Id, "XoaDonAdmin");
                }

                // 2. Xóa dữ liệu
                _context.ChiTietHoaDons.RemoveRange(hoaDon.ChiTietHoaDons);
                _context.HoaDons.Remove(hoaDon);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa đơn hàng #{id} thành công."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Lỗi hệ thống khi xóa đơn hàng: {ex.Message}"
                });
            }
        }

        // ---------------------------------------------------------------------
        // HÀM HELPER: TRỪ TỒN KHO KHI ĐƠN BẮT ĐẦU TIÊU HAO TỒN (false -> true)
        // ---------------------------------------------------------------------
        private async Task DeductInventoryForOrder(int hoaDonId, string loaiGiaoDich)
        {
            var chiTietHoaDon = await _context.ChiTietHoaDons
                .Where(ct => ct.HoaDonId == hoaDonId)
                .ToListAsync();

            foreach (var item in chiTietHoaDon)
            {
                var productVariant = await _context.ChiTietSanPhams.FindAsync(item.ChiTietSanPhamId);

                if (productVariant != null)
                {
                    // Tùy m: có cho âm kho không. Ở đây t cho trừ thẳng.
                    productVariant.SoLuongTon -= item.SoLuong;
                    _context.Update(productVariant);

                    var log = new LichSuTonKho
                    {
                        ChiTietSanPhamId = productVariant.Id,
                        SoLuongThayDoi = -item.SoLuong,             // -N vì xuất kho
                        LoaiGiaoDich = loaiGiaoDich,               // "DieuChinhTrangThai_Admin"
                        GhiChu = $"Trừ kho khi đổi trạng thái đơn #{hoaDonId}",
                        NgayTao = DateTime.Now
                    };

                    _context.LichSuTonKhos.Add(log);
                }
            }

            await _context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------------
        // HÀM HELPER: RESTORE INVENTORY (Khôi phục tồn kho + GHI LOG LichSuTonKho)
        // ---------------------------------------------------------------------
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

                    // 2. Ghi log lịch sử tồn kho
                    var log = new LichSuTonKho
                    {
                        ChiTietSanPhamId = productVariant.Id,
                        SoLuongThayDoi = item.SoLuong,            // +N vì hàng quay lại
                        LoaiGiaoDich = loaiGiaoDich,             // "HuyDon_Admin", "HoanHang_Admin", "XoaDonAdmin"...
                        GhiChu = $"Khôi phục tồn kho từ hóa đơn #{hoaDonId}",
                        NgayTao = DateTime.Now
                    };

                    _context.LichSuTonKhos.Add(log);
                }
            }

            await _context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------------
        // 🔔 GỬI THÔNG BÁO ĐƠN HÀNG QUA CHATBOT (REALTIME)
        // ---------------------------------------------------------------------
        private async Task SendOrderStatusNotificationAsync(HoaDon order)
        {
            order = await _context.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.Id == order.Id);

            if (order == null)
                return;

            var customerId = order.UserId ?? 0;

            var chiTiets = await _context.ChiTietHoaDons
                .Include(ct => ct.ChiTietSanPham)
                    .ThenInclude(ctsp => ctsp.SanPham)
                        .ThenInclude(sp => sp.HinhAnhSanPhams)
                .Where(ct => ct.HoaDonId == order.Id)
                .ToListAsync();

            var baseContent = BuildOrderStatusMessage(order, order.TrangThai?.MaTrangThai, chiTiets);
            if (string.IsNullOrWhiteSpace(baseContent))
                return;

            var botUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.TenRole == "Bot");

            if (botUser == null)
                return;

            var conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId &&
                    c.Status != "Closed"
                );

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    CustomerId = customerId,
                    Subject = $"Thông báo đơn hàng #{order.Id}",
                    Status = "Open",
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now,
                    IsBotActive = true
                };

                _context.ChatConversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var now = DateTime.Now;

            var msg = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = botUser.Id,
                SenderType = "Bot",
                Content = baseContent,
                SentAt = now,
                IsRead = false
            };

            _context.ChatMessages.Add(msg);
            conversation.LastMessageAt = now;
            await _context.SaveChangesAsync();

            var groupName = $"conversation-{conversation.Id}";
            var botName = ((botUser.Ho + " " + botUser.Ten) ?? "Ngọc Lan Bot").Trim();
            if (string.IsNullOrWhiteSpace(botName)) botName = "Ngọc Lan Bot";

            var avatarUrl = botUser.AvatarUrl ?? "/admin/images/users/avatar-1.jpg";

            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                id = msg.Id,
                conversationId = msg.ConversationId,
                senderId = msg.SenderId,
                senderType = msg.SenderType,
                senderName = botName,
                avatarUrl = avatarUrl,
                content = msg.Content,
                sentAt = msg.SentAt.ToLocalTime().ToString("HH:mm dd/MM")
            });
        }

        private string BuildOrderStatusMessage(HoaDon order, string? statusCode, System.Collections.Generic.List<ChiTietHoaDon> chiTiets)
        {
            var maDon = order.Id;

            string statusText = statusCode switch
            {
                "Pending" or "ChoXacNhan" =>
                    $"[Thông báo đơn hàng] Đơn hàng #{maDon} đã được tạo và đang chờ shop xác nhận ❤️",

                "Confirmed" or "DaXacNhan" =>
                    $"[Thông báo đơn hàng] Đơn hàng #{maDon} đã được shop xác nhận và chuẩn bị giao. Cảm ơn bạn đã mua hàng tại Ngọc Lan Fashion!",

                "Shipped" or "DangGiao" =>
                    $"[Thông báo đơn hàng] Đơn hàng #{maDon} đang được giao đến bạn 🚚. Vui lòng chú ý điện thoại nhé!",

                "Delivered" or "HoanThanh" =>
                    $"🎉 Đơn hàng #{maDon} đã được giao thành công!\nShop hy vọng bạn hài lòng với sản phẩm 💕",

                "Cancelled" or "Huy" =>
                    $"[Thông báo đơn hàng] Rất tiếc, đơn hàng #{maDon} đã bị hủy. Nếu cần hỗ trợ thêm hãy nhắn cho shop nhé.",

                "ReturnRequested" =>
                    $"[Thông báo đơn hàng] Shop đã nhận được YÊU CẦU HOÀN HÀNG cho đơn #{maDon}. Nhân viên sẽ liên hệ bạn trong thời gian sớm nhất.",

                "Returned" =>
                    $"[Thông báo đơn hàng] Đơn hàng #{maDon} đã được xử lý HOÀN HÀNG. Cảm ơn bạn đã tin tưởng Ngọc Lan Fashion!",

                _ => ""
            };

            if (string.IsNullOrWhiteSpace(statusText))
                return "";

            var sb = new StringBuilder();
            sb.AppendLine(statusText);
            sb.AppendLine("<b>Thông tin đơn hàng:</b>");

            var tenTrangThai = order.TrangThai?.TenTrangThai ?? "Không xác định";

            int tongSoLuong = chiTiets.Sum(ct => ct.SoLuong);
            decimal tongTien = chiTiets.Any() ? chiTiets.Sum(ct => ct.DonGia * ct.SoLuong) : 0;

            sb.AppendLine($"Mã đơn: <b>#{order.Id}</b>");
            sb.AppendLine($"Trạng thái: <b>{tenTrangThai}</b>");
            sb.AppendLine($"Số sản phẩm: <b>{tongSoLuong}</b>");
            sb.AppendLine($"Tổng tạm tính: <b>{tongTien.ToString("N0")} đ</b>");

            var firstItem = chiTiets
                .FirstOrDefault(ct => ct.ChiTietSanPham?.SanPham != null);

            if (firstItem != null)
            {
                var sp = firstItem.ChiTietSanPham.SanPham;

                var imgUrl = sp.HinhAnhSanPhams?
                                 .FirstOrDefault(h => h.LaAnhDaiDien == true)?.UrlHinhAnh
                             ?? sp.HinhAnhSanPhams?.FirstOrDefault()?.UrlHinhAnh
                             ?? "/images/no-image.png";

                var orderLink = $"/HoaDon/Details/{maDon}";
                var productLink = $"/Shop/Details/{sp.Id}";

                sb.AppendLine($"<a href=\"{productLink}\" target=\"_blank\">");
                sb.AppendLine(
                    $"<img src=\"{imgUrl}\" alt=\"{sp.TenSanPham}\" " +
                    "style=\"max-width:120px;max-height:120px;border-radius:8px;object-fit:cover;border:1px solid \\#eee;\" />");
                sb.AppendLine("</a>");

                sb.AppendLine($"<span>{sp.TenSanPham}</span>");
                sb.AppendLine($"<a href=\"{orderLink}\" target=\"_blank\">Xem chi tiết đơn hàng</a>");
            }

            if (statusCode == "Delivered" || statusCode == "HoanThanh")
            {
                sb.AppendLine("✨ Bạn có thể vào lịch sử đơn hàng hoặc trang sản phẩm để để lại đánh giá giúp shop phục vụ tốt hơn nữa nhé! 🌸");
            }

            return sb.ToString();
        }
    }
}
