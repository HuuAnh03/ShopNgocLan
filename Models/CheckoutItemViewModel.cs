namespace ShopNgocLan.Models
{

    public class CheckoutItemViewModel
    {
        public int? ChiTietGioHangId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }

        // Các thông tin này sẽ được lấy từ CSDL (để bảo mật)
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string ColorName { get; set; } 
        public string SizeName { get; set; }  
    }
}