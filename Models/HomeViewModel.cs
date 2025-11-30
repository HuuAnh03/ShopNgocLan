namespace ShopNgocLan.Models
{
    public class HomeViewModel
    {
        // Danh sách sản phẩm dùng cho các tab (Nam / Nữ / Phụ kiện)
        public List<SanPham> SanPhams { get; set; } = new List<SanPham>();

        // Danh sách tất cả danh mục (dùng để lọc theo cây)
        public List<DanhMucSanPham> AllCategories { get; set; } = new List<DanhMucSanPham>();

        // (Để sẵn cho tương lai, nếu muốn dùng)
        // public List<SanPham> NewProducts { get; set; } = new List<SanPham>();
        // public List<SanPham> BestSellerProducts { get; set; } = new List<SanPham>();
    }
}
