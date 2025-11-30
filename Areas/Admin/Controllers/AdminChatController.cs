using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class AdminChatController : Controller
    {
        private readonly DBShopNLContext _context;

        public AdminChatController(DBShopNLContext context)
        {
            _context = context;
        }
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }
        // CHỈ CẦN ACTION NÀY ĐỂ HIỂN THỊ MÀN HÌNH QUẢN TRỊ CHAT
        public async Task<IActionResult> Index(int? id)
        {
           
                var conversations = await _context.ChatConversations
                    .Include(c => c.Customer)
                    .Include(c => c.ChatMessages)
                        .ThenInclude(m => m.Sender)
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .ToListAsync();

                ChatConversation? selected = null;

                if (conversations.Any())
                {
                    // nếu không có id -> chọn cuộc đầu tiên
                    var selectedId = id ?? conversations.First().Id;
                    selected = conversations.FirstOrDefault(c => c.Id == selectedId);

                    if (selected != null)
                    {
                        // sắp xếp message tăng dần theo thời gian
                        selected.ChatMessages = selected.ChatMessages
                            .OrderBy(m => m.SentAt)
                            .ToList();
                    }
                }

                var vm = new AdminChatIndexViewModel
                {
                    Conversations = conversations,
                    SelectedConversation = selected
                };

                return View(vm);
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBot(int conversationId, bool isActive)
        {
            var convo = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (convo == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy cuộc trò chuyện."
                });
            }

            convo.IsBotActive = isActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = convo.IsBotActive
            });
        }



    }
}
