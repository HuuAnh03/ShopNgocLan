namespace ShopNgocLan.Models
{
    public class AdminChatIndexViewModel
    {
        public List<ChatConversation> Conversations { get; set; } = new();
        public ChatConversation? SelectedConversation { get; set; }
    }
}
