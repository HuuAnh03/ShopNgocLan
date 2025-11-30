using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShopNgocLan.Repository;

namespace ShopNgocLan.ViewComponents
{
    public class AddToCartViewComponent :ViewComponent
    {
        private readonly IAddToCartRepository _gioHang;
        public AddToCartViewComponent(IAddToCartRepository addToCartRepository)
        {
            _gioHang = addToCartRepository;
        }
        public IViewComponentResult Invoke()
        { int iduser  = HttpContext.Session.GetInt32("UserId") ?? -1;
            var listsp = _gioHang.GetAllAsync(iduser);
                return View(listsp);
        }
    }
}
