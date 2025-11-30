using ShopNgocLan.Models;

namespace ShopNgocLan.Services
{
    public interface  IChatBotService
    {
        Task<ChatMessage?> HandleCustomerMessageAsync(ChatConversation conversation, ChatMessage customerMessage);
    }
}
