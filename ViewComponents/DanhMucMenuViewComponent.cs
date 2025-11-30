using ShopNgocLan.Models;
using Microsoft.AspNetCore.Mvc;
using ShopNgocLan.Repository;
namespace ShopNgocLan.ViewComponents

{
    public class DanhMucMenuViewComponent : ViewComponent
    {
        private readonly IDanhMucSpRepository _danhMuc;
        public DanhMucMenuViewComponent(IDanhMucSpRepository danhMucSpRepository)
        {
            _danhMuc = danhMucSpRepository;
        }
        public IViewComponentResult Invoke()
        {
            var danhmucSP = _danhMuc.GetAllAsync().OrderBy(x => x.TenDanhMuc);
            return View(danhmucSP);
        }
    }
}
