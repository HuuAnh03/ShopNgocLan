using System;
using System.Collections.Generic;

namespace ShopNgocLan.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Ho { get; set; }

    public string Ten { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string MatKhau { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public DateTime? NgayTao { get; set; }

    public int RoleId { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual ICollection<ChatConversation> ChatConversationAssignedStaffs { get; set; } = new List<ChatConversation>();

    public virtual ICollection<ChatConversation> ChatConversationCustomers { get; set; } = new List<ChatConversation>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhamAdminUsers { get; set; } = new List<DanhGiaSanPham>();

    public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhamUsers { get; set; } = new List<DanhGiaSanPham>();

    public virtual ICollection<DiaChiGiaoHang> DiaChiGiaoHangs { get; set; } = new List<DiaChiGiaoHang>();

    public virtual GioHang? GioHang { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<LichSuTimKiem> LichSuTimKiems { get; set; } = new List<LichSuTimKiem>();

    public virtual ICollection<LichSuTonKho> LichSuTonKhos { get; set; } = new List<LichSuTonKho>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TinTuc> TinTucs { get; set; } = new List<TinTuc>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
