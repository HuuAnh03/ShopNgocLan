namespace ShopNgocLan.Models.Inventory
{
    public class InventoryItemViewModel
    {
        public int ChiTietSanPhamId { get; set; }

        public string TenSanPham { get; set; } = string.Empty;
        public string AnhSanPham { get; set; } = "";

        public string DanhMuc { get; set; } = string.Empty;
        public string MauSac { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;

        // ====== TỒN HIỆN TẠI (lấy trực tiếp từ ChiTietSanPham) ======
        public int SoLuongTonHienTai { get; set; }

        // ====== TỔNG NHẬP/XUẤT TOÀN THỜI GIAN (để thống kê dài hạn) ======
        public int TongNhapAll { get; set; }
        public int TongXuatAll { get; set; }

        // ====== NHẬP/XUẤT TRONG KHOẢNG NGÀY (dùng filter ở Index) ======
        public int TongNhapTrongKhoang { get; set; }
        public int TongXuatTrongKhoang { get; set; }

        // (Tuỳ chọn) label hiển thị khoảng ngày trên view
        public string? KhoangThoiGianLabel { get; set; }
    }
}
