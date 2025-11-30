using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;

namespace ShopNgocLan.Repository
{
    public class DanhMucRepository : IDanhMucSpRepository
    {
        private readonly DBShopNLContext _context;

        public DanhMucRepository(DBShopNLContext context)
        {
            _context = context;
        }

        public  IEnumerable<DanhMucSanPham> GetAllAsync()
        {
            
            return  _context.DanhMucSanPhams;
        }

    }
}
