using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class SanPham
{
    public int Id { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? ThuongHieu { get; set; }

    public string? ChatLieu { get; set; }

    public int DanhMucId { get; set; }

    public DateTime? NgayTao { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ChiTietSanPham> ChiTietSanPhams { get; set; } = new List<ChiTietSanPham>();

    public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhams { get; set; } = new List<DanhGiaSanPham>();

    public virtual DanhMucSanPham DanhMuc { get; set; } = null!;

    public virtual ICollection<HinhAnhSanPham> HinhAnhSanPhams { get; set; } = new List<HinhAnhSanPham>();

    public virtual ICollection<SanPhamKhuyenMai> SanPhamKhuyenMais { get; set; } = new List<SanPhamKhuyenMai>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
