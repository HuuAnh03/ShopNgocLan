using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class LichSuTimKiem
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public string TuKhoa { get; set; } = null!;

    public DateTime? ThoiGianTimKiem { get; set; }

    public virtual User? User { get; set; }
}
