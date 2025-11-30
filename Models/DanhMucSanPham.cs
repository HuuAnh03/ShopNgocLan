using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class DanhMucSanPham
{
    public int Id { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    public int? ParentId { get; set; }

    public string? Path { get; set; }

    public virtual ICollection<DanhMucSanPham> InverseParent { get; set; } = new List<DanhMucSanPham>();

    public virtual DanhMucSanPham? Parent { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
