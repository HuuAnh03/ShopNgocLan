using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Linq.Expressions;
using System.Security.Claims;
using X.PagedList;

namespace ShopNgocLan.Controllers
{
    public class ShopController : Controller
    {
        private readonly DBShopNLContext _context;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public ShopController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int? idDanhMuc, string? searchString, bool? hasPromotion)
        {
            ViewBag.Categories = await _context.DanhMucSanPhams.ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.SelectedCategoryId = idDanhMuc ?? 0;

          
            ViewBag.HasPromotion = hasPromotion ?? false;

            return View();
        }


        public async Task<IActionResult> _GetProductListPartial(
    int? page,
    int? idDanhMuc,
    int? listfilter,
    string? searchString,
    bool? isBestSeller,
    decimal? minPrice,
    decimal? maxPrice,
    bool? hasPromotion   // 👈 THÊM Ở ĐÂY
)
        {
            int pageSize = 9;
            int pageNumber = (page ?? 1);

            var productsQuery = _context.SanPhams
                                    .Include(p => p.DanhMuc)
                                    .Include(p => p.ChiTietSanPhams)
                                        .ThenInclude(ct => ct.MauSac)
                                    .Include(p => p.ChiTietSanPhams)
                                        .ThenInclude(ct => ct.Size)
                                    .Include(p => p.HinhAnhSanPhams)
                                    .AsQueryable();

            // LỌC 0: Trạng thái Active
            productsQuery = productsQuery.Where(p => p.IsActive == true);

            // 1. Lọc search
            if (!string.IsNullOrEmpty(searchString))
            {
                string normalizedSearch = searchString.Trim().ToLower();
                productsQuery = productsQuery.Where(p =>
                    (p.TenSanPham != null && p.TenSanPham.ToLower().Contains(normalizedSearch)) ||
                    (p.DanhMuc != null && p.DanhMuc.TenDanhMuc != null && p.DanhMuc.TenDanhMuc.ToLower().Contains(normalizedSearch))
                );
            }

            // 2. Lọc danh mục (như cũ) ...
            if (idDanhMuc.HasValue && idDanhMuc.Value != 0)
            {
                var danhMucCha = await _context.DanhMucSanPhams.FindAsync(idDanhMuc.Value);
                if (danhMucCha != null && !string.IsNullOrEmpty(danhMucCha.Path))
                {
                    string pathCha = danhMucCha.Path;
                    var allCategoryIds = await _context.DanhMucSanPhams
                        .Where(dm => dm.Path != null && dm.Path.StartsWith(pathCha))
                        .Select(dm => dm.Id)
                        .ToListAsync();
                    productsQuery = productsQuery.Where(p => allCategoryIds.Contains(p.DanhMucId));
                }
                else
                {
                    productsQuery = productsQuery.Where(p => p.DanhMucId == idDanhMuc.Value);
                }
            }

            // 2.2 Lọc khoảng giá (như cũ)...
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                decimal min = minPrice ?? 0;
                decimal max = maxPrice ?? decimal.MaxValue;

                productsQuery = productsQuery.Where(p =>
                    p.ChiTietSanPhams.Any(ct => ct.Gia >= min && ct.Gia <= max)
                );
            }

            // 2.3 Lọc bán chạy (như cũ)...
            if (isBestSeller.HasValue && isBestSeller.Value == true)
            {
                var topVariants = await _context.ChiTietHoaDons
                    .Where(ctdh => ctdh.HoaDon.TrangThai.MaTrangThai == "Delivered")
                    .GroupBy(ctdh => ctdh.ChiTietSanPhamId)
                    .Select(g => new
                    {
                        ChiTietSanPhamId = g.Key,
                        TotalSoldQuantity = g.Sum(x => x.SoLuong)
                    })
                    .OrderByDescending(x => x.TotalSoldQuantity)
                    .Take(100)
                    .ToListAsync();

                var topVariantIds = topVariants.Select(x => x.ChiTietSanPhamId).ToList();

                productsQuery = productsQuery.Where(p =>
                    p.ChiTietSanPhams.Any(ct => topVariantIds.Contains(ct.Id))
                );
            }

            // 2.4 🔥 LỌC SẢN PHẨM ĐANG KHUYẾN MÃI
            if (hasPromotion.HasValue && hasPromotion.Value)
            {
                // Sản phẩm nào có ít nhất 1 biến thể có GiaGoc > 0 (đang áp KM)
                productsQuery = productsQuery.Where(p =>
                    p.ChiTietSanPhams.Any(ct => ct.GiaGoc > 0)
                );
            }

            // 3. Sắp xếp (giữ nguyên)
            switch (listfilter)
            {
                case 1: // Giá tăng dần
                    productsQuery = productsQuery.OrderBy(sp =>
                        sp.ChiTietSanPhams.Any() ? sp.ChiTietSanPhams.Min(ct => ct.Gia) : 0
                    );
                    break;
                case 2: // Giá giảm dần
                    productsQuery = productsQuery.OrderByDescending(sp =>
                        sp.ChiTietSanPhams.Any() ? sp.ChiTietSanPhams.Min(ct => ct.Gia) : 0
                    );
                    break;
                case 3: // Tên A-Z
                    productsQuery = productsQuery.OrderBy(sp => sp.TenSanPham);
                    break;
                case 4: // Mới nhất
                    productsQuery = productsQuery.OrderByDescending(sp => sp.NgayTao);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(sp => sp.NgayTao);
                    break;
            }

            // 4. Map sang ProductListViewModel (đã có HasPromotion, OriginalPriceString như mình gửi ở tin trước)
            var viewModelQuery = productsQuery.Select(sp => new ProductListViewModel
            {
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                AnhDaiDienUrl = sp.HinhAnhSanPhams.FirstOrDefault(h => h.LaAnhDaiDien == true).UrlHinhAnh
        ?? sp.HinhAnhSanPhams.FirstOrDefault().UrlHinhAnh
        ?? "/images/placeholder.jpg",
                AnhPhuUrl = sp.HinhAnhSanPhams.FirstOrDefault(h => h.LaAnhDaiDien != true).UrlHinhAnh
        ?? sp.HinhAnhSanPhams.FirstOrDefault().UrlHinhAnh
        ?? "/images/placeholder.jpg",
                MoTa = sp.MoTa,

                PriceString = (!sp.ChiTietSanPhams.Any())
        ? "N/A"
        : (
            sp.ChiTietSanPhams.Min(ct => ct.Gia) == sp.ChiTietSanPhams.Max(ct => ct.Gia)
                ? sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " đ"
                : sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " - " +
                  sp.ChiTietSanPhams.Max(ct => ct.Gia).ToString("N0") + " đ"
          ),

                HasPromotion = sp.ChiTietSanPhams.Any(ct => ct.GiaGoc > 0),

                OriginalPriceString =
        (!sp.ChiTietSanPhams.Any(ct => ct.GiaGoc > 0))
            ? null
            : (
                sp.ChiTietSanPhams.Where(ct => ct.GiaGoc > 0).Min(ct => ct.GiaGoc)
                == sp.ChiTietSanPhams.Where(ct => ct.GiaGoc > 0).Max(ct => ct.GiaGoc)
                    ? sp.ChiTietSanPhams.Where(ct => ct.GiaGoc > 0).Min(ct => ct.GiaGoc).ToString("N0") + " đ"
                    : sp.ChiTietSanPhams.Where(ct => ct.GiaGoc > 0).Min(ct => ct.GiaGoc).ToString("N0") + " - " +
                      sp.ChiTietSanPhams.Where(ct => ct.GiaGoc > 0).Max(ct => ct.GiaGoc).ToString("N0") + " đ"
              ),

                // 👇 % giảm cao nhất
                BestDiscountPercent = sp.ChiTietSanPhams
        .Any(ct => ct.GiaGoc > 0 && ct.GiaGoc > ct.Gia)
            ? sp.ChiTietSanPhams
                .Where(ct => ct.GiaGoc > 0 && ct.GiaGoc > ct.Gia)
                .Select(ct => 100m * (1 - ct.Gia / ct.GiaGoc))
                .Max()
            : (decimal?)null,

                SizeString = (!sp.ChiTietSanPhams.Any())
        ? "N/A"
        : string.Join(", ",
            sp.ChiTietSanPhams
                .Select(ct => ct.Size.TenSize)
                .Where(s => s != null)
                .Distinct()
                .OrderBy(s => s)
          ),

                TotalStock = sp.ChiTietSanPhams.Sum(ct => ct.SoLuongTon),
                TenDanhMuc = sp.DanhMuc.TenDanhMuc ?? "N/A",
                NgayTaoFormatted = sp.NgayTao.HasValue ? sp.NgayTao.Value.ToString("dd/MM/yyyy") : "N/A",
                IsActive = sp.IsActive ?? false
            });



            // 5. Phân trang
            var productsList = new PagedList<ProductListViewModel>(viewModelQuery, pageNumber, pageSize);

            // 6. ViewBag để giữ filter cho phân trang
            ViewBag.CurrentDanhMucId = idDanhMuc;
            ViewBag.CurrentListFilter = listfilter;
            ViewBag.CurrentSearchString = searchString;
            ViewBag.IsBestSeller = isBestSeller;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.HasPromotion = hasPromotion; // 👈 THÊM

            return PartialView("_GetProductListPartial", productsList);
        }




        public async Task<IActionResult> Details(int id)
        {
            if (id == 0 || _context.SanPhams == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm và tất cả dữ liệu liên quan
            var sanPham = await _context.SanPhams
                .Include(p => p.ChiTietSanPhams).ThenInclude(ct => ct.MauSac)
                .Include(p => p.ChiTietSanPhams).ThenInclude(ct => ct.Size)
                .Include(p => p.HinhAnhSanPhams)
                .Include(p => p.DanhGiaSanPhams) // Bảng đánh giá
                .FirstOrDefaultAsync(p => p.Id == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // ===== 1. TÍNH TOÁN ĐIỂM VÀ SỐ LƯỢNG ĐÁNH GIÁ =====
            int reviewCount = 0;
            double averageRating = 5.0; // Mặc định 5 sao

            if (sanPham.DanhGiaSanPhams != null && sanPham.DanhGiaSanPhams.Any())
            {
                // Chỉ lấy những đánh giá đã được duyệt (IsPublished = true)
                var publishedReviews = sanPham.DanhGiaSanPhams
                    .Where(r => r.IsPublished == true)
                    .ToList();

                reviewCount = publishedReviews.Count;

                if (reviewCount > 0)
                {
                    // Có đánh giá được publish → tính trung bình
                    averageRating = publishedReviews.Average(r => (double)r.DiemDanhGia);
                }
                // Ngược lại: không có published review → giữ nguyên averageRating = 5.0
            }

            // ===== 2. LẤY DANH MỤC (DÙNG CHO BREADCRUMB / LINK DANH MỤC) =====
            var danhMucs = await _context.DanhMucSanPhams
                .OrderBy(c => c.TenDanhMuc)
                .ToListAsync();

            // ===== 3. LẤY SẢN PHẨM LIÊN QUAN =====
            var relatedProductEntities = await _context.SanPhams
                .Include(sp => sp.ChiTietSanPhams)
                .Include(sp => sp.HinhAnhSanPhams)
                .Where(sp =>
                    sp.DanhMucId == sanPham.DanhMucId &&       // cùng danh mục
                    sp.IsActive == true &&                      // đang bán
                    sp.Id != sanPham.Id)                        // loại trừ chính nó
                .OrderByDescending(sp => sp.NgayTao)
                .Take(12)
                .ToListAsync();

            // Map sang RelatedProductViewModel
            var relatedProducts = relatedProductEntities
                .Select(sp =>
                {
                    decimal giaMin = 0;
                    decimal giaMax = 0;

                    if (sp.ChiTietSanPhams != null && sp.ChiTietSanPhams.Any())
                    {
                        giaMin = sp.ChiTietSanPhams.Min(ct => ct.Gia);
                        giaMax = sp.ChiTietSanPhams.Max(ct => ct.Gia);
                    }

                    var mainImageUrl = sp.HinhAnhSanPhams?
                                           .FirstOrDefault(h => h.LaAnhDaiDien == true)?.UrlHinhAnh
                                       ?? sp.HinhAnhSanPhams?.FirstOrDefault()?.UrlHinhAnh
                                       ?? "/images/avatars/default.jpg";

                    return new RelatedProductViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        AnhDaiDienUrl = mainImageUrl,
                        GiaMin = giaMin,
                        GiaMax = giaMax
                    };
                })
                .ToList();

            // ===== 4. MAP SANG VIEWMODEL CHÍNH =====
            var viewModel = new ProductDetailsViewModel
            {
                Id = sanPham.Id,
                TenSanPham = sanPham.TenSanPham,
                MoTa = sanPham.MoTa,
                ThuongHieu = sanPham.ThuongHieu,
                ChatLieu = sanPham.ChatLieu,
                DanhMucId = sanPham.DanhMucId,
                IsActive = sanPham.IsActive ?? false,

                // Biến thể (variants)
                Variants = sanPham.ChiTietSanPhams.Select(ct => new ProductVariantViewModel
                {
                    Id = ct.Id,
                    MauSacId = ct.MauSacId,
                    SizeId = ct.SizeId,
                    Gia = ct.Gia,
                    SoLuongTon = ct.SoLuongTon,
                    MauSacName = ct.MauSac?.TenMau,
                    MaMauHex = ct.MauSac?.MaMauHex,
                    SizeName = ct.Size?.TenSize
                }).ToList(),

                // Hình ảnh
                ExistingImages = sanPham.HinhAnhSanPhams.Select(h => new ProductImageViewModel
                {
                    Id = h.Id,
                    UrlHinhAnh = h.UrlHinhAnh,
                    LaAnhDaiDien = h.LaAnhDaiDien
                }).ToList(),

                // Danh mục
                DanhMucList = danhMucs,

                // Thông tin đánh giá
                ReviewCount = reviewCount,
                DiemDanhGia = averageRating,

                // Sản phẩm liên quan
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }



    }
}
