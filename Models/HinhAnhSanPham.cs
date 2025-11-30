using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class HinhAnhSanPham
{
    public int Id { get; set; }

    public int SanPhamId { get; set; }

    public string UrlHinhAnh { get; set; } = null!;

    public bool? LaAnhDaiDien { get; set; }

    public int? MauSacId { get; set; }

    public virtual MauSac? MauSac { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
