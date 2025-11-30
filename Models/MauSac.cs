using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class MauSac
{
    public int Id { get; set; }

    public string TenMau { get; set; } = null!;

    public string? MaMauHex { get; set; }

    public virtual ICollection<ChiTietSanPham> ChiTietSanPhams { get; set; } = new List<ChiTietSanPham>();

    public virtual ICollection<HinhAnhSanPham> HinhAnhSanPhams { get; set; } = new List<HinhAnhSanPham>();
}
