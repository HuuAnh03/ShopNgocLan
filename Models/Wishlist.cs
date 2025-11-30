using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class Wishlist
{
    public int UserId { get; set; }

    public int SanPhamId { get; set; }

    public DateTime? NgayThem { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
