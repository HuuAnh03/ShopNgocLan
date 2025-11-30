using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChiTietGioHang
{
    public int GioHangId { get; set; }

    public int ChiTietSanPhamId { get; set; }

    public int SoLuong { get; set; }

    public int ChiTietGioHangId { get; set; }

    public virtual ChiTietSanPham ChiTietSanPham { get; set; } = null!;

    public virtual GioHang GioHang { get; set; } = null!;
}
