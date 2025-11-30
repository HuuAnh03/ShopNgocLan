using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class HoaDon
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? TenKhachHang { get; set; }

    public string? EmailKhachHang { get; set; }

    public string? SoDienThoaiKhachHang { get; set; }

    public DateTime? NgayTao { get; set; }

    public decimal TongTien { get; set; }

    public decimal? PhiVanChuyen { get; set; }

    public decimal? GiamGia { get; set; }

    public decimal ThanhTien { get; set; }

    public string DiaChiGiaoHang { get; set; } = null!;

    public int PhuongThucThanhToanId { get; set; }

    public int TrangThaiId { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual PhuongThucThanhToan PhuongThucThanhToan { get; set; } = null!;

    public virtual TrangThaiDonHang TrangThai { get; set; } = null!;

    public virtual User? User { get; set; }
}
