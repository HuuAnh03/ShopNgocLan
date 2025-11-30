namespace ShopNgocLan.Models
{
    public class ProductListViewModel
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; } = string.Empty; // Default to empty string
        public string? AnhDaiDienUrl { get; set; } // URL for the main image
        public string? AnhPhuUrl { get; set; } // URL for the main image
        public string? MoTa { get; set; }
        public string PriceString { get; set; } = "N/A"; // Formatted price range (e.g., "80,000 - 120,000 đ")
        public string SizeString { get; set; } = "N/A"; // Comma-separated sizes (e.g., "S, M, L")
        public int TotalStock { get; set; } // Total stock across all variants
        public string TenDanhMuc { get; set; } = "N/A"; // Category name
        public string? NgayTaoFormatted { get; set; } // Formatted creation date (e.g., "19/10/2025")
        public bool IsActive { get; set; } // Product status
        public bool HasPromotion { get; set; }
        public string OriginalPriceString { get; set; }  // giá gốc (để gạch ngang)
        public decimal? BestDiscountPercent { get; set; }

    }
}
