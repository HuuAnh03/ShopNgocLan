using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChatConversation
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string? Subject { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int? AssignedStaffId { get; set; }

    public bool IsBotActive { get; set; }

    public virtual User? AssignedStaff { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual User Customer { get; set; } = null!;
}
