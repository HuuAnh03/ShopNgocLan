using ShopNgocLan.Models;

namespace ShopNgocLan.Repository
{
    public interface IDanhMucSpRepository
    {
        IEnumerable<DanhMucSanPham> GetAllAsync();
    }
}
