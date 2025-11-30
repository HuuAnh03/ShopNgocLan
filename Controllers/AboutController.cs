using Microsoft.AspNetCore.Mvc;

namespace ShopNgocLan.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
