using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class GioHang
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual User User { get; set; } = null!;
}
