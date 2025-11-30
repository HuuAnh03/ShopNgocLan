using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChatIntentReply
{
    public int Id { get; set; }

    public int IntentId { get; set; }

    public string ReplyText { get; set; } = null!;

    public bool TrangThai { get; set; }

    public virtual ChatIntent Intent { get; set; } = null!;
}
