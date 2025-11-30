using System.Collections.Generic;

namespace ShopNgocLan.Models
{
    public class DashboardViewModel
    {
        // ----- TOP CARDS -----
        public int TotalOrders { get; set; }
        public int NewLeads { get; set; }
        public int TotalDeals { get; set; }
        public decimal Revenue { get; set; }
        public int PendingOrders { get; set; }
        public decimal Profit { get; set; }

        // ----- CONVERSION / CHART DATA -----
        public List<int> PerformanceData { get; set; } = new();
        public List<int> ConversionData { get; set; } = new();

        public List<ProductStat> TopSellingProducts { get; set; } = new();
        public List<ProductStat> SlowSellingProducts { get; set; } = new();
        // ----- BY COUNTRY -----
        public int SessionThisWeek { get; set; }
        public int SessionLastWeek { get; set; }

        // ----- TOP PAGES (demo) -----
        public List<TopPageItem> TopPages { get; set; } = new();

        // ----- RECENT ORDERS -----
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    }
    public class ProductStat
    {   
        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public int SanPhamId { get; set; }
        public string Image { get; set; }
    }

    public class TopPageItem
    {
        public string PagePath { get; set; }
        public int PageViews { get; set; }
        public string ExitRate { get; set; }
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime? NgayTao { get; set; }
        public string KhachHang { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DiaChi { get; set; }
        public string Payment { get; set; }
        public string TrangThai { get; set; }
        public string? Image { get; set; }
    }
}
