using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Security.Claims;

namespace ShopNgocLan.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly DBShopNLContext _context;

        public ChatController(DBShopNLContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId")??0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentUserId().Value;
            if (customerId==0)
            {
                // Nếu chưa đăng nhập thì cho về trang login
                return RedirectToAction("Login", "Account");
                // hoặc return Unauthorized(); nếu m làm API
            }
            var conversation = await _context.ChatConversations
                .Include(c => c.Customer)                       // <-- thêm dòng này
                .Include(c => c.ChatMessages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId &&
                    c.Status == "Open"
                );

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    CustomerId = customerId,
                    Status = "Open",
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now,
                    IsBotActive = true
                };

                _context.ChatConversations.Add(conversation);
                await _context.SaveChangesAsync();

                // load lại kèm Customer
                conversation = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.ChatMessages)
                        .ThenInclude(m => m.Sender)
                    .FirstAsync(c => c.Id == conversation.Id);
            }

            // Sort
            conversation.ChatMessages = conversation.ChatMessages
                .OrderBy(m => m.SentAt)
                .ToList();

            return View(conversation);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBot(int conversationId, bool isActive)
        {
            var convo = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (convo == null)
                return Json(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });

            // TODO: kiểm tra conversation có thuộc về user hiện tại không (nếu muốn bảo mật chặt hơn)

            convo.IsBotActive = isActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = convo.IsBotActive });
        }
    }
}
