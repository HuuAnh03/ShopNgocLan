namespace ShopNgocLan.Services
{
    public interface IInventoryService
    {
        Task RecordStockChangeAsync(int chiTietSanPhamId, int soLuongThayDoi,
                                string loaiGiaoDich, string? ghiChu = null,
                                int? nhanVienId = null, int? hoaDonId = null);
    }
}
