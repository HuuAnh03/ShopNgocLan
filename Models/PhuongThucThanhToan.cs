using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class PhuongThucThanhToan
{
    public int Id { get; set; }

    public string TenPhuongThuc { get; set; } = null!;

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
