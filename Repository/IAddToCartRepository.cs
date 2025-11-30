using ShopNgocLan.Models;

namespace ShopNgocLan.Repository
{
    public interface IAddToCartRepository
    {
        AddToCartViewModel GetAllAsync(int? userId);
    }
}
