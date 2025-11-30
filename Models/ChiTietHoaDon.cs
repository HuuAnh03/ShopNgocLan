using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChiTietHoaDon
{
    public int Id { get; set; }

    public int HoaDonId { get; set; }

    public int ChiTietSanPhamId { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual ChiTietSanPham ChiTietSanPham { get; set; } = null!;

    public virtual HoaDon HoaDon { get; set; } = null!;
}
