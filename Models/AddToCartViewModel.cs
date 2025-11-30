namespace ShopNgocLan.Models
{
    public class AddToCartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        public decimal Subtotal { get; set; } // Tổng phụ
        public decimal ShippingFee { get; set; } // Phí ship
        public decimal Total { get; set; } // Tổng cộng
        

    }
}
