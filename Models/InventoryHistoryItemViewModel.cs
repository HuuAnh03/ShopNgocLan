namespace ShopNgocLan.Models
{
    public class InventoryHistoryItemViewModel
    {
        public int Id { get; set; }

        public int ChiTietSanPhamId { get; set; }

        public string TenSanPham { get; set; } = string.Empty;
        public string DanhMuc { get; set; } = string.Empty;
        public string MauSac { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;

        public string AnhSanPham { get; set; } = "/images/no-image.jpg";

        // +N hoặc -N
        public int SoLuongThayDoi { get; set; }

        // "NhapHang", "BanHang", "DieuChinh", "TraHang", ...
        public string LoaiGiaoDich { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; }

        // Nhân viên thao tác (nếu có)
        public int? NhanVienId { get; set; }
        public string? TenNhanVien { get; set; }

        // Sử dụng cho filter date hiển thị
        public string? KhoangThoiGianLabel { get; set; }
    }
}
