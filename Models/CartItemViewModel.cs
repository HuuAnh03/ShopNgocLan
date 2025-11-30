namespace ShopNgocLan.Models
{
    public class CartItemViewModel
    {
        public int Id { get; set; } // Đây có thể là ID của Biến thể (Variant)
        public int ProductId { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? AnhDaiDienUrl { get; set; }

        // Size và Màu đã chọn
        public string Size { get; set; } = string.Empty;
        public string Mau { get; set; } = string.Empty;

        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia; // Tự động tính
    }
}
