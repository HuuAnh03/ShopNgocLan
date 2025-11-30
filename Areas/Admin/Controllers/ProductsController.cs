using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using ShopNgocLan.Models.Inventory;
using X.PagedList;

namespace ShopNgocLan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class ProductsController : Controller
    {
        private readonly DBShopNLContext _context;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductsController(DBShopNLContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 6;
            int pageNumber = (page ?? 1);

            // Query including necessary relations
            var productsQuery = _context.SanPhams
                                        .Include(p => p.DanhMuc)
                                        .Include(p => p.HinhAnhSanPhams)
                                        .Include(p => p.ChiTietSanPhams)
                                            .ThenInclude(ct => ct.Size)
                                        .Include(p => p.ChiTietSanPhams)
                                            .ThenInclude(ct => ct.MauSac) // Include color if needed elsewhere, not strictly for this VM
                                        .OrderByDescending(p => p.NgayTao);

            // *** PROJECT INTO VIEWMODEL using .Select() ***
            var viewModelQuery = productsQuery.Select(sp => new ProductListViewModel
            {
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                // Logic to get representative image URL
                AnhDaiDienUrl = sp.HinhAnhSanPhams.FirstOrDefault(h => h.LaAnhDaiDien == true).UrlHinhAnh
                                ?? sp.HinhAnhSanPhams.FirstOrDefault().UrlHinhAnh
                                ?? "/images/placeholder.jpg", // Default image
                // Logic for price string (handle potential null or empty details)
                PriceString = (sp.ChiTietSanPhams == null || !sp.ChiTietSanPhams.Any()) ? "N/A" :
                              (sp.ChiTietSanPhams.Min(ct => ct.Gia) == sp.ChiTietSanPhams.Max(ct => ct.Gia) ?
                               sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " đ" :
                               sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " - " + sp.ChiTietSanPhams.Max(ct => ct.Gia).ToString("N0") + " đ"),
                // Logic for size string
                SizeString = (sp.ChiTietSanPhams == null || !sp.ChiTietSanPhams.Any()) ? "N/A" :
                             string.Join(", ", sp.ChiTietSanPhams
                                                .Select(ct => ct.Size.TenSize)
                                                .Where(s => s != null)
                                                .Distinct()
                                                .OrderBy(s => s)), // Simple sort for sizes
                // Calculate total stock
                TotalStock = sp.ChiTietSanPhams.Sum(ct => ct.SoLuongTon),
                TenDanhMuc = sp.DanhMuc.TenDanhMuc ?? "N/A",
                NgayTaoFormatted = sp.NgayTao.HasValue ? sp.NgayTao.Value.ToString("dd/MM/yyyy") : "N/A",
                IsActive = sp.IsActive ?? false // Default to false if null
            });

            PagedList<ProductListViewModel> productsList = new PagedList<ProductListViewModel>(viewModelQuery, pageNumber, pageSize);
            ViewBag.HasLowStock = await _context.ChiTietSanPhams
    .AnyAsync(ct => ct.SoLuongTon <= 5);
            ViewBag.LowStockProducts = await _context.SanPhams
                .Where(sp => sp.ChiTietSanPhams.Sum(ct => ct.SoLuongTon) <= 5)
                .Select(sp => new ProductListViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    AnhDaiDienUrl = sp.HinhAnhSanPhams
                        .Where(a => a.LaAnhDaiDien == true)
                        .Select(a => a.UrlHinhAnh)
                        .FirstOrDefault(),
                    TotalStock = sp.ChiTietSanPhams.Sum(ct => ct.SoLuongTon),
                    TenDanhMuc = sp.DanhMuc.TenDanhMuc
                })
                .ToListAsync();

            return View(productsList);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductCreateViewModel
            {
                // Load data for dropdowns
                DanhMucList = new SelectList(await _context.DanhMucSanPhams.OrderBy(c => c.TenDanhMuc).ToListAsync(), "Id", "TenDanhMuc"),
                MauSacList = new SelectList(await _context.MauSacs.OrderBy(m => m.TenMau).ToListAsync(), "Id", "TenMau"),
                SizeList = new SelectList(await _context.Sizes.OrderBy(s => s.TenSize).ToListAsync(), "Id", "TenSize")
            };
            return View(viewModel);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel)
        {
            // Basic check if variants were added
            if (viewModel.Variants == null || !viewModel.Variants.Any())
            {
                ModelState.AddModelError("Variants", "Vui lòng thêm ít nhất một phiên bản (màu sắc, size, giá, số lượng).");
            }
            if (viewModel.ImageFiles == null || !viewModel.ImageFiles.Any())
            {
                ModelState.AddModelError("ImageFiles", "Vui lòng tải lên ít nhất một hình ảnh.");
            }

            // Custom validation for unique variant combinations if needed (Size + Color must be unique per product)
            if (viewModel.Variants != null && viewModel.Variants.Any())
            {
                var duplicateVariants = viewModel.Variants
                    .GroupBy(v => new { v.MauSacId, v.SizeId })
                    .Where(g => g.Count() > 1)
                    .Select(g => $"Màu/Size ({g.Key.MauSacId}/{g.Key.SizeId})") // Improve this to show names if possible
                    .ToList();

                if (duplicateVariants.Any())
                {
                    ModelState.AddModelError("Variants", "Không được có các phiên bản trùng lặp (cùng Màu sắc và Size). Trùng lặp: " + string.Join(", ", duplicateVariants));
                }
            }


            if (ModelState.IsValid)
            {
                // 1. Create the main SanPham object
                var sanPham = new SanPham
                {
                    TenSanPham = viewModel.TenSanPham,
                    MoTa = viewModel.MoTa,
                    ThuongHieu = viewModel.ThuongHieu,
                    ChatLieu = viewModel.ChatLieu,
                    DanhMucId = viewModel.DanhMucId,
                    IsActive = viewModel.IsActive,
                    NgayTao = DateTime.Now // Set creation date
                };

                _context.SanPhams.Add(sanPham);
                await _context.SaveChangesAsync(); // Save to get the SanPham ID

                // 2. Create ChiTietSanPham (Variants)
                foreach (var variantVM in viewModel.Variants)
                {
                    var chiTiet = new ChiTietSanPham
                    {
                        SanPhamId = sanPham.Id,
                        Gia = variantVM.Gia,
                        GiaNhap = variantVM.GiaNhap,
                        SoLuongTon = variantVM.SoLuongTon,
                        MauSacId = variantVM.MauSacId,
                        SizeId = variantVM.SizeId
                    };
                    _context.ChiTietSanPhams.Add(chiTiet);
                }

                // 3. Handle Image Uploads
                if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) // Create folder if it doesn't exist
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    for (int i = 0; i < viewModel.ImageFiles.Count; i++)
                    {
                        var file = viewModel.ImageFiles[i];
                        if (file != null && file.Length > 0)
                        {
                            // Create a unique filename
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // Save the file
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            // Create HinhAnhSanPham record
                            var hinhAnh = new HinhAnhSanPham
                            {
                                SanPhamId = sanPham.Id,
                                UrlHinhAnh = "/images/products/" + uniqueFileName, // Store relative path
                                LaAnhDaiDien = (i == viewModel.MainImageIndex) // Mark as main image if index matches
                                // You might want to associate image with color (MauSacId) here if applicable
                            };
                            _context.HinhAnhSanPhams.Add(hinhAnh);
                        }
                    }
                    // Ensure at least one image is marked as main if MainImageIndex wasn't set or invalid
                    if (viewModel.MainImageIndex == null || viewModel.MainImageIndex < 0 || viewModel.MainImageIndex >= viewModel.ImageFiles.Count)
                    {
                        var firstImageRecord = _context.ChangeTracker.Entries<HinhAnhSanPham>()
                           .Where(e => e.State == EntityState.Added && e.Entity.SanPhamId == sanPham.Id)
                           .Select(e => e.Entity)
                           .FirstOrDefault();
                        if (firstImageRecord != null)
                        {
                            firstImageRecord.LaAnhDaiDien = true;
                        }
                    }
                }

                await _context.SaveChangesAsync(); // Save Variants and Images

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is Invalid, reload dropdowns and return view
            viewModel.DanhMucList = new SelectList(await _context.DanhMucSanPhams.OrderBy(c => c.TenDanhMuc).ToListAsync(), "Id", "TenDanhMuc", viewModel.DanhMucId);
            viewModel.MauSacList = new SelectList(await _context.MauSacs.OrderBy(m => m.TenMau).ToListAsync(), "Id", "TenMau");
            viewModel.SizeList = new SelectList(await _context.Sizes.OrderBy(s => s.TenSize).ToListAsync(), "Id", "TenSize");
            return View(viewModel);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.SanPhams == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm và các dữ liệu liên quan
            var sanPham = await _context.SanPhams
                .Include(p => p.ChiTietSanPhams)
                    .ThenInclude(ct => ct.MauSac)
                .Include(p => p.ChiTietSanPhams)
                    .ThenInclude(ct => ct.Size)
                .Include(p => p.HinhAnhSanPhams)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Map từ Model (SanPham) sang ViewModel (ProductEditViewModel)
            var viewModel = new ProductEditViewModel
            {
                Id = sanPham.Id,
                TenSanPham = sanPham.TenSanPham,
                MoTa = sanPham.MoTa,
                ThuongHieu = sanPham.ThuongHieu,
                ChatLieu = sanPham.ChatLieu,
                DanhMucId = sanPham.DanhMucId,
                IsActive = sanPham.IsActive ?? false,

                // Map các phiên bản đã có
                Variants = sanPham.ChiTietSanPhams.Select(ct => new ProductVariantViewModel
                {
                    Id = ct.Id,
                    MauSacId = ct.MauSacId,
                    SizeId = ct.SizeId,
                    Gia = ct.Gia,
                    GiaNhap = ct.GiaNhap ?? 0,          
                    SoLuongTon = ct.SoLuongTon,
                    MauSacName = ct.MauSac?.TenMau,
                    SizeName = ct.Size?.TenSize
                }).ToList(),

                // Map các hình ảnh đã có
                ExistingImages = sanPham.HinhAnhSanPhams.Select(h => new ProductImageViewModel
                {
                    Id = h.Id,
                    UrlHinhAnh = h.UrlHinhAnh,
                    LaAnhDaiDien = h.LaAnhDaiDien
                }).ToList(),
                ExistingImageCount = sanPham.HinhAnhSanPhams.Count, // Gán số lượng ảnh cũ
                // Lấy ID ảnh đại diện hiện tại
                MainImageId = sanPham.HinhAnhSanPhams.FirstOrDefault(h => h.LaAnhDaiDien==true)?.Id,

                // Load dropdown lists
                DanhMucList = new SelectList(await _context.DanhMucSanPhams.OrderBy(c => c.TenDanhMuc).ToListAsync(), "Id", "TenDanhMuc", sanPham.DanhMucId),
                MauSacList = new SelectList(await _context.MauSacs.OrderBy(m => m.TenMau).ToListAsync(), "Id", "TenMau"),
                SizeList = new SelectList(await _context.Sizes.OrderBy(s => s.TenSize).ToListAsync(), "Id", "TenSize")
            };

            return View(viewModel);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            // *** VALIDATION (Tương tự Create) ***
            var allVariants = viewModel.Variants ?? new List<ProductVariantViewModel>();
            if (!allVariants.Any())
            {
                ModelState.AddModelError("Variants", "Vui lòng thêm ít nhất một phiên bản.");
            }

            // Kiểm tra ảnh: Phải có ít nhất 1 ảnh (cũ hoặc mới)
            int existingImageCount = viewModel.ExistingImageCount; // <-- SỬA THÀNH DÒNG NÀY
            int imagesToDeleteCount = viewModel.ImagesToDelete?.Count ?? 0;
            int newImageCount = viewModel.ImageFiles?.Count(f => f != null && f.Length > 0) ?? 0;
            if (existingImageCount - imagesToDeleteCount + newImageCount == 0)
            {
                ModelState.AddModelError("ImageFiles", "Sản phẩm phải có ít nhất một hình ảnh.");
            }

            // Kiểm tra phiên bản trùng lặp
            if (allVariants.Any())
            {
                var duplicateVariants = allVariants
                    .GroupBy(v => new { v.MauSacId, v.SizeId })
                    .Where(g => g.Count() > 1)
                    .Select(g => $"MàuId {g.Key.MauSacId} & SizeId {g.Key.SizeId}")
                    .ToList();

                if (duplicateVariants.Any())
                {
                    ModelState.AddModelError("Variants", "Không được có các phiên bản trùng lặp (cùng Màu sắc và Size). Trùng lặp: " + string.Join(", ", duplicateVariants));
                }
            }

            if (ModelState.IsValid)
            {
                // Sử dụng Transaction để đảm bảo toàn vẹn dữ liệu
                await using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Lấy sản phẩm gốc từ DB (bao gồm cả các quan hệ)
                        var sanPhamToUpdate = await _context.SanPhams
                            .Include(p => p.ChiTietSanPhams)
                            .Include(p => p.HinhAnhSanPhams)
                            .FirstOrDefaultAsync(p => p.Id == viewModel.Id);

                        if (sanPhamToUpdate == null)
                        {
                            return NotFound();
                        }

                        // 2. Cập nhật thông tin cơ bản của SanPham
                        sanPhamToUpdate.TenSanPham = viewModel.TenSanPham;
                        sanPhamToUpdate.MoTa = viewModel.MoTa;
                        sanPhamToUpdate.ThuongHieu = viewModel.ThuongHieu;
                        sanPhamToUpdate.ChatLieu = viewModel.ChatLieu;
                        sanPhamToUpdate.DanhMucId = viewModel.DanhMucId;
                        sanPhamToUpdate.IsActive = viewModel.IsActive;
                        sanPhamToUpdate.NgayTao = DateTime.Now; 

                        // 3. Đồng bộ hóa ChiTietSanPham (Phiên bản)
                        var submittedVariantIds = allVariants.Where(v => v.Id.HasValue && v.Id > 0).Select(v => v.Id.Value).ToList();

                        // Xóa các phiên bản không còn trong danh sách gửi lên
                        var variantsToDelete = sanPhamToUpdate.ChiTietSanPhams
                            .Where(e => !submittedVariantIds.Contains(e.Id)).ToList();
                        _context.ChiTietSanPhams.RemoveRange(variantsToDelete);

                        // Cập nhật hoặc Thêm mới
                        foreach (var variantVM in allVariants)
                        {
                            if (variantVM.Id.HasValue && variantVM.Id > 0) // Cập nhật
                            {
                                var variantToUpdate = sanPhamToUpdate.ChiTietSanPhams.FirstOrDefault(e => e.Id == variantVM.Id);
                                if (variantToUpdate != null)
                                {
                                    variantToUpdate.Gia = variantVM.Gia;
                                    variantToUpdate.GiaNhap = variantVM.GiaNhap;
                                    variantToUpdate.SoLuongTon = variantVM.SoLuongTon;
                                    variantToUpdate.MauSacId = variantVM.MauSacId;
                                    variantToUpdate.SizeId = variantVM.SizeId;
                                }
                            }
                            else // Thêm mới
                            {
                                var newVariant = new ChiTietSanPham
                                {
                                    SanPhamId = sanPhamToUpdate.Id,
                                    Gia = variantVM.Gia,
                                    GiaNhap = variantVM.GiaNhap,
                                    SoLuongTon = variantVM.SoLuongTon,
                                    MauSacId = variantVM.MauSacId,
                                    SizeId = variantVM.SizeId
                                };
                                sanPhamToUpdate.ChiTietSanPhams.Add(newVariant); // Thêm vào navigation property
                            }
                        }

                        // 4. Xóa các hình ảnh được đánh dấu
                        if (viewModel.ImagesToDelete != null && viewModel.ImagesToDelete.Any())
                        {
                            foreach (int imageIdToDelete in viewModel.ImagesToDelete)
                            {
                                var imageToDelete = sanPhamToUpdate.HinhAnhSanPhams.FirstOrDefault(h => h.Id == imageIdToDelete);
                                if (imageToDelete != null)
                                {
                                    // Xóa file vật lý
                                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToDelete.UrlHinhAnh.TrimStart('/'));
                                    if (System.IO.File.Exists(oldFilePath))
                                    {
                                        System.IO.File.Delete(oldFilePath);
                                    }
                                    _context.HinhAnhSanPhams.Remove(imageToDelete); // EF sẽ theo dõi việc xóa
                                }
                            }
                        }

                        // 5. Thêm hình ảnh mới (Logic giống Create)
                        var newImageRecords = new List<HinhAnhSanPham>();
                        if (viewModel.ImageFiles != null && viewModel.ImageFiles.Any())
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            for (int i = 0; i < viewModel.ImageFiles.Count; i++)
                            {
                                var file = viewModel.ImageFiles[i];
                                if (file != null && file.Length > 0)
                                {
                                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                    await using (var fileStream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(fileStream);
                                    }

                                    var hinhAnh = new HinhAnhSanPham
                                    {
                                        UrlHinhAnh = "/images/products/" + uniqueFileName,
                                        LaAnhDaiDien = (i == viewModel.MainImageIndex) // Đánh dấu nếu là ảnh đại diện MỚI
                                    };
                                    newImageRecords.Add(hinhAnh);
                                    sanPhamToUpdate.HinhAnhSanPhams.Add(hinhAnh); // Thêm vào navigation property
                                }
                            }
                        }

                        // 6. Xử lý ảnh đại diện
                        // Bỏ chọn tất cả ảnh đại diện hiện tại
                        foreach (var img in sanPhamToUpdate.HinhAnhSanPhams)
                        {
                            img.LaAnhDaiDien = false;
                        }

                        HinhAnhSanPham? newMainImage = null;
                        if (viewModel.MainImageIndex.HasValue) // Ưu tiên 1: Ảnh MỚI được chọn
                        {
                            int newIndex = viewModel.MainImageIndex.Value;
                            if (newIndex >= 0 && newIndex < newImageRecords.Count)
                            {
                                newImageRecords[newIndex].LaAnhDaiDien = true;
                                newMainImage = newImageRecords[newIndex];
                            }
                        }
                        else if (viewModel.MainImageId.HasValue) // Ưu tiên 2: Ảnh CŨ được chọn
                        {
                            var mainImg = sanPhamToUpdate.HinhAnhSanPhams.FirstOrDefault(h => h.Id == viewModel.MainImageId);
                            if (mainImg != null)
                            {
                                mainImg.LaAnhDaiDien = true;
                                newMainImage = mainImg;
                            }
                        }

                        // Mặc định: Lấy ảnh đầu tiên (còn lại) nếu chưa có cái nào được chọn
                        if (newMainImage == null && sanPhamToUpdate.HinhAnhSanPhams.Any())
                        {
                            var firstImage = sanPhamToUpdate.HinhAnhSanPhams.FirstOrDefault();
                            if (firstImage != null)
                            {
                                firstImage.LaAnhDaiDien = true;
                            }
                        }

                        await _context.SaveChangesAsync(); // Lưu tất cả thay đổi
                        await transaction.CommitAsync(); // Hoàn tất transaction

                        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        // Ghi log lỗi (ex)
                        ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.");
                    }
                }
            }

            // Nếu ModelState không hợp lệ, tải lại dropdowns và hiển thị lại view
            viewModel.DanhMucList = new SelectList(await _context.DanhMucSanPhams.OrderBy(c => c.TenDanhMuc).ToListAsync(), "Id", "TenDanhMuc", viewModel.DanhMucId);
            viewModel.MauSacList = new SelectList(await _context.MauSacs.OrderBy(m => m.TenMau).ToListAsync(), "Id", "TenMau");
            viewModel.SizeList = new SelectList(await _context.Sizes.OrderBy(s => s.TenSize).ToListAsync(), "Id", "TenSize");

            // Tải lại ExistingImages nếu không submit (thường không cần thiết nếu view binding đúng)
            if (viewModel.ExistingImages == null || !viewModel.ExistingImages.Any())
            {
                var sanPham = await _context.SanPhams
                   .Include(p => p.HinhAnhSanPhams)
                   .AsNoTracking()
                   .FirstOrDefaultAsync(p => p.Id == id);
                if (sanPham != null)
                {
                    viewModel.ExistingImages = sanPham.HinhAnhSanPhams.Select(h => new ProductImageViewModel
                    {
                        Id = h.Id,
                        UrlHinhAnh = h.UrlHinhAnh,
                        LaAnhDaiDien = h.LaAnhDaiDien
                    }).ToList();
                }
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sp = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.HinhAnhSanPhams)
                .Include(p => p.ChiTietSanPhams)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (sp == null)
            {
                return NotFound();
            }

            // Map sang viewmodel để hiển thị
            var vm = new ProductListViewModel
            {
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                AnhDaiDienUrl = sp.HinhAnhSanPhams
                                    .Where(h => h.LaAnhDaiDien == true)
                                    .Select(h => h.UrlHinhAnh)
                                    .FirstOrDefault()
                                ?? sp.HinhAnhSanPhams.Select(h => h.UrlHinhAnh).FirstOrDefault()
                                ?? "/images/placeholder.jpg",
                PriceString = (sp.ChiTietSanPhams == null || !sp.ChiTietSanPhams.Any()) ? "N/A" :
                              (sp.ChiTietSanPhams.Min(ct => ct.Gia) == sp.ChiTietSanPhams.Max(ct => ct.Gia)
                                  ? sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " đ"
                                  : sp.ChiTietSanPhams.Min(ct => ct.Gia).ToString("N0") + " - " +
                                    sp.ChiTietSanPhams.Max(ct => ct.Gia).ToString("N0") + " đ"),
                TotalStock = sp.ChiTietSanPhams.Sum(ct => ct.SoLuongTon),
                TenDanhMuc = sp.DanhMuc?.TenDanhMuc ?? "N/A",
                NgayTaoFormatted = sp.NgayTao.HasValue ? sp.NgayTao.Value.ToString("dd/MM/yyyy") : "N/A",
                IsActive = sp.IsActive ?? false
            };

            return View(vm);  // -> sẽ tìm tới Areas/Admin/Views/Products/Delete.cshtml
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null)
                return NotFound();

            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        

    }
}
