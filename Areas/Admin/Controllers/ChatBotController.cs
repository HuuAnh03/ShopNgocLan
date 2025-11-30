using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatBotController : Controller
    {
        private readonly DBShopNLContext _context;

        public ChatBotController(DBShopNLContext context)
        {
            _context = context;
        }

        // ========== DANH SÁCH INTENT + PATTERN + REPLY ==========
        public async Task<IActionResult> Index()
        {
            var intents = await _context.ChatIntents
                .Include(i => i.ChatIntentPatterns.OrderBy(p => p.Id))
                .Include(i => i.ChatIntentReplies.OrderBy(r => r.Id))
                .OrderBy(i => i.DoUuTien)
                .ThenBy(i => i.Id)
                .ToListAsync();

            return View(intents);
        }

        // ========== TẠO INTENT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIntent(string code, string tenIntent, string? moTa, int doUuTien = 0)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(tenIntent))
            {
                TempData["Error"] = "Code và Tên intent không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            code = code.Trim();
            tenIntent = tenIntent.Trim();

            // Không cho trùng code
            bool exists = await _context.ChatIntents.AnyAsync(i => i.Code == code);
            if (exists)
            {
                TempData["Error"] = $"Intent code '{code}' đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var intent = new ChatIntent
            {
                Code = code,
                TenIntent = tenIntent,
                MoTa = moTa?.Trim(),
                DoUuTien = doUuTien,
                TrangThai = true
            };

            _context.ChatIntents.Add(intent);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm intent mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ========== CẬP NHẬT INTENT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIntent(int id, string code, string tenIntent, string? moTa, int doUuTien = 0, bool trangThai = true)
        {
            var intent = await _context.ChatIntents.FindAsync(id);
            if (intent == null)
            {
                TempData["Error"] = "Không tìm thấy intent.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(tenIntent))
            {
                TempData["Error"] = "Code và Tên intent không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            code = code.Trim();
            tenIntent = tenIntent.Trim();

            bool exists = await _context.ChatIntents.AnyAsync(i => i.Id != id && i.Code == code);
            if (exists)
            {
                TempData["Error"] = $"Intent code '{code}' đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            intent.Code = code;
            intent.TenIntent = tenIntent;
            intent.MoTa = moTa?.Trim();
            intent.DoUuTien = doUuTien;
            intent.TrangThai = trangThai;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật intent thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ========== XÓA INTENT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIntent(int id)
        {
            var intent = await _context.ChatIntents
                .Include(i => i.ChatIntentPatterns)
                .Include(i => i.ChatIntentReplies)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (intent == null)
            {
                TempData["Error"] = "Không tìm thấy intent.";
                return RedirectToAction(nameof(Index));
            }

            _context.ChatIntents.Remove(intent);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa intent thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ========== THÊM PATTERN ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPattern(int intentId, string patternText, bool isRegex = false)
        {
            var intent = await _context.ChatIntents.FindAsync(intentId);
            if (intent == null)
            {
                TempData["Error"] = "Không tìm thấy intent.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(patternText))
            {
                TempData["Error"] = "Pattern không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var pattern = new ChatIntentPattern
            {
                IntentId = intentId,
                PatternText = patternText.Trim(),
                IsRegex = isRegex,
                TrangThai = true
            };

            _context.ChatIntentPatterns.Add(pattern);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm pattern thành công.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePattern(int id, int intentId, string patternText, bool isRegex, bool trangThai)
        {
            var p = await _context.ChatIntentPatterns.FindAsync(id);
            if (p == null)
            {
                TempData["Error"] = "Không tìm thấy pattern.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(patternText))
            {
                TempData["Error"] = "Pattern không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            p.PatternText = patternText.Trim();
            p.IsRegex = isRegex;
            p.TrangThai = trangThai;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật pattern thành công.";
            return RedirectToAction(nameof(Index));
        }


        // ========== XÓA PATTERN ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePattern(int id)
        {
            var pattern = await _context.ChatIntentPatterns.FindAsync(id);
            if (pattern == null)
            {
                TempData["Error"] = "Không tìm thấy pattern.";
                return RedirectToAction(nameof(Index));
            }

            _context.ChatIntentPatterns.Remove(pattern);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa pattern thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ========== THÊM REPLY ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(int intentId, string replyText)
        {
            var intent = await _context.ChatIntents.FindAsync(intentId);
            if (intent == null)
            {
                TempData["Error"] = "Không tìm thấy intent.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["Error"] = "Nội dung trả lời không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var reply = new ChatIntentReply
            {
                IntentId = intentId,
                ReplyText = replyText.Trim(),
                TrangThai = true
            };

            _context.ChatIntentReplies.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm câu trả lời thành công.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReply(int id, int intentId, string replyText, bool trangThai)
        {
            var reply = await _context.ChatIntentReplies.FindAsync(id);
            if (reply == null)
            {
                TempData["Error"] = "Không tìm thấy câu trả lời.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["Error"] = "Nội dung trả lời không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            reply.ReplyText = replyText.Trim();
            reply.TrangThai = trangThai;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật câu trả lời thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ========== XÓA REPLY ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var reply = await _context.ChatIntentReplies.FindAsync(id);
            if (reply == null)
            {
                TempData["Error"] = "Không tìm thấy câu trả lời.";
                return RedirectToAction(nameof(Index));
            }

            _context.ChatIntentReplies.Remove(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa câu trả lời thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
