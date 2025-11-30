using ShopNgocLan.Models;
using ShopNgocLan.Services;

public class InventoryService : IInventoryService
{
    private readonly DBShopNLContext _context;

    public InventoryService(DBShopNLContext context)
    {
        _context = context;
    }

    public async Task RecordStockChangeAsync(int chiTietSanPhamId, int soLuongThayDoi,
                                             string loaiGiaoDich, string? ghiChu = null,
                                             int? nhanVienId = null, int? hoaDonId = null)
    {
        var log = new LichSuTonKho
        {
            ChiTietSanPhamId = chiTietSanPhamId,
            SoLuongThayDoi = soLuongThayDoi,
            LoaiGiaoDich = loaiGiaoDich,
            GhiChu = ghiChu,
            NhanVienId = nhanVienId,
            NgayTao = DateTime.Now,
            // Nếu m thêm HoaDonId, PhieuNhapId thì gán ở đây
            // HoaDonId = hoaDonId
        };

        _context.LichSuTonKhos.Add(log);
        await _context.SaveChangesAsync();
    }
}
