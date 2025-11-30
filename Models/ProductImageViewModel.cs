namespace ShopNgocLan.Models
{
    // Dùng để hiển thị các ảnh ĐÃ CÓ trên trang Edit
    public class ProductImageViewModel
    {
        public int Id { get; set; }
        public string UrlHinhAnh { get; set; } = string.Empty;
        public bool? LaAnhDaiDien { get; set; }
    }
}