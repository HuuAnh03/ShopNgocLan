using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using ShopNgocLan.Services;
using System.Security.Claims;

namespace ShopNgocLan.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly DBShopNLContext _context;
        private readonly IChatBotService _chatBotService;

        public ChatHub(DBShopNLContext context, IChatBotService chatBotService)
        {
            _context = context;
            _chatBotService = chatBotService;
        }

        private int GetCurrentUserId()
        {
            // 1. Ưu tiên lấy theo Claims
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var idFromClaims))
            {
                return idFromClaims;
            }

            // 2. Nếu dùng Session login
            var httpContext = Context.GetHttpContext();

            if (httpContext != null && httpContext.Session != null)
            {
                var sessionId = httpContext.Session.GetInt32("UserId");
                if (sessionId.HasValue)
                    return sessionId.Value;
            }

            throw new Exception("User is not logged in.");
        }


        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        private string GetConversationGroupName(int conversationId) => $"conversation-{conversationId}";

        public async Task SendMessage(int conversationId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            int userId = GetCurrentUserId();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            var conversation = await _context.ChatConversations
                .Include(c => c.ChatMessages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                throw new Exception("Conversation not found");

            // Xác định SenderType theo Role
            string senderType = user.Role?.TenRole switch
            {
                "Admin" => "Admin",
                "NhanVien" => "NhanVien",
                "Bot" => "Bot",
                _ => "KhachHang"
            };

            var now = DateTime.Now;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = user.Id,
                SenderType = senderType,
                Content = content.Trim(),
                SentAt = now,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            conversation.LastMessageAt = now;
            await _context.SaveChangesAsync();

            // Chuẩn bị info người gửi
            var senderFullName = (user.Ho + " " + user.Ten ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(senderFullName))
            {
                senderFullName = senderType == "KhachHang" ? "Bạn"
                              : senderType == "Admin" ? "Quản trị viên"
                              : senderType == "NhanVien" ? "Nhân viên hỗ trợ"
                              : "Hệ thống";
            }

            var avatarUrl = user.AvatarUrl ?? "/admin/images/users/avatar-1.jpg";

            // Gửi cho tất cả client join cùng conversation
            await Clients.Group(GetConversationGroupName(conversationId))
                .SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    conversationId = message.ConversationId,
                    senderId = message.SenderId,
                    senderType = message.SenderType,
                    senderName = senderFullName,
                    avatarUrl,
                    content = message.Content,
                    sentAt = message.SentAt.ToLocalTime().ToString("HH:mm dd/MM")
                });

            // Nếu là khách thì cho bot trả lời
            if (senderType == "KhachHang")
            {
                var botReply = await _chatBotService.HandleCustomerMessageAsync(conversation, message);
                if (botReply != null)
                {
                    var botUser = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == botReply.SenderId);

                    string botName = "Bot";
                    string botAvatar = "/admin/images/users/avatar-1.jpg";

                    if (botUser != null)
                    {
                        var tmpName = (botUser.Ho + " " + botUser.Ten ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(tmpName))
                            botName = tmpName;

                        botAvatar = botUser.AvatarUrl ?? botAvatar;
                    }

                    await Clients.Group(GetConversationGroupName(conversationId))
                        .SendAsync("ReceiveMessage", new
                        {
                            id = botReply.Id,
                            conversationId = botReply.ConversationId,
                            senderId = botReply.SenderId,
                            senderType = botReply.SenderType, // "Bot"
                            senderName = botName,
                            avatarUrl = botAvatar,
                            content = botReply.Content,
                            sentAt = botReply.SentAt.ToLocalTime().ToString("HH:mm dd/MM")
                        });
                }
            }
        }
    }
}
