using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChatIntent
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string TenIntent { get; set; } = null!;

    public string? MoTa { get; set; }

    public int DoUuTien { get; set; }

    public bool TrangThai { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatIntentPattern> ChatIntentPatterns { get; set; } = new List<ChatIntentPattern>();

    public virtual ICollection<ChatIntentReply> ChatIntentReplies { get; set; } = new List<ChatIntentReply>();
}
