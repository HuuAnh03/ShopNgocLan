using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class ChiTietSanPham
{
    public int Id { get; set; }

    public int SanPhamId { get; set; }

    public decimal Gia { get; set; }

    public int SoLuongTon { get; set; }

    public int MauSacId { get; set; }

    public int SizeId { get; set; }

    public decimal? GiaNhap { get; set; }

    public decimal GiaGoc { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<LichSuTonKho> LichSuTonKhos { get; set; } = new List<LichSuTonKho>();

    public virtual MauSac MauSac { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;

    public virtual Size Size { get; set; } = null!;
}
