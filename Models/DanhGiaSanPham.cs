using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class DanhGiaSanPham
{
    public int Id { get; set; }

    public int SanPhamId { get; set; }

    public int UserId { get; set; }

    public int DiemDanhGia { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public string? PhanHoiAdmin { get; set; }

    public DateTime? NgayPhanHoi { get; set; }

    public int? AdminUserId { get; set; }

    public bool IsPublished { get; set; }

    public virtual User? AdminUser { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
