using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class TinTuc
{
    public int Id { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? UrlHinhAnh { get; set; }

    public int TacGiaId { get; set; }

    public DateTime? NgayDang { get; set; }

    public string? MoTaNgan { get; set; }

    public string? Slug { get; set; }

    public bool IsExternal { get; set; }

    public string? ExternalUrl { get; set; }

    public bool TrangThai { get; set; }

    public virtual User TacGia { get; set; } = null!;
}
