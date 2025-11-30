namespace ShopNgocLan.Models
{
    public class DanhMucViewModel
    {
        public DanhMucSanPham DanhMucHienTai { get; set; }

        public List<DanhMucSanPham> DanhSachCon { get; set; }
        public List<ProductListViewModel> DanhSachSanPham { get; set; }

    }
}
