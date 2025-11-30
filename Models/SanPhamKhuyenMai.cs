using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class SanPhamKhuyenMai
{
    public int SanPhamId { get; set; }

    public int KhuyenMaiId { get; set; }

    public int Id { get; set; }

    public virtual KhuyenMai KhuyenMai { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
