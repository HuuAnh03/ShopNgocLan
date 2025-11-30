using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChatIntentPattern
{
    public int Id { get; set; }

    public int IntentId { get; set; }

    public string PatternText { get; set; } = null!;

    public bool IsRegex { get; set; }

    public bool TrangThai { get; set; }

    public virtual ChatIntent Intent { get; set; } = null!;
}
