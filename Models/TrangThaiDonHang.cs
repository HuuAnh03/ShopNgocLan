using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class TrangThaiDonHang
{
    public int Id { get; set; }

    public string MaTrangThai { get; set; } = null!;

    public string TenTrangThai { get; set; } = null!;

    public int ThuTu { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
