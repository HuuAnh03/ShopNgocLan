using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class Size
{
    public int Id { get; set; }

    public string TenSize { get; set; } = null!;

    public virtual ICollection<ChiTietSanPham> ChiTietSanPhams { get; set; } = new List<ChiTietSanPham>();
}
