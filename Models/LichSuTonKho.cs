using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class LichSuTonKho
{
    public int Id { get; set; }

    public int ChiTietSanPhamId { get; set; }

    public int SoLuongThayDoi { get; set; }

    public string LoaiGiaoDich { get; set; } = null!;

    public string? GhiChu { get; set; }

    public int? NhanVienId { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChiTietSanPham ChiTietSanPham { get; set; } = null!;

    public virtual User? NhanVien { get; set; }
}
