using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public int SenderId { get; set; }

    public string SenderType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? MetadataJson { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
