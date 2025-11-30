using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShopNgocLan.Models;

namespace ShopNgocLan.Repository
{
    public class AddToCartRepository : IAddToCartRepository
    {
        private readonly DBShopNLContext _context;

        public AddToCartRepository(DBShopNLContext context)
        {
            _context = context;
        }

        public  AddToCartViewModel GetAllAsync(int? userId)
        {
            var viewModel = new AddToCartViewModel();

            var cartItems =  _context.ChiTietGioHangs
                .Where(ctgh => ctgh.GioHang.UserId == userId)
                .Select(item => new CartItemViewModel {
                    Id = item.ChiTietSanPhamId, // Dùng ID của biến thể
                    ProductId = item.ChiTietSanPham.SanPhamId,
                    TenSanPham = item.ChiTietSanPham.SanPham.TenSanPham,
                    Size = item.ChiTietSanPham.Size.TenSize,
                    Mau = item.ChiTietSanPham.MauSac.TenMau,
                    SoLuong = item.SoLuong,
                    DonGia = item.ChiTietSanPham.Gia,
                    AnhDaiDienUrl = _context.HinhAnhSanPhams
                        .Where(img => img.SanPhamId == item.ChiTietSanPham.SanPhamId && img.LaAnhDaiDien == true)
                        .Select(img => img.UrlHinhAnh)
                        .FirstOrDefault()
                }).ToList();
                 // Thực thi truy vấn

            // 5. GÁN KẾT QUẢ:
            if (cartItems != null)
            {
                viewModel.Items = cartItems;
                viewModel.Subtotal = cartItems.Sum(i => i.ThanhTien); // Tính tổng phụ
                viewModel.ShippingFee = 0; // Tạm gán phí ship
                viewModel.Total = viewModel.Subtotal + viewModel.ShippingFee; // Tính tổng cộng
            }

            // 6. TRẢ VỀ:
            return viewModel;
        }
    }
}
