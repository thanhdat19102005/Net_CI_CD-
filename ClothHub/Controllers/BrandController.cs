using ClothHub.Models;
using ClothHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/brands")]
    [Authorize(Roles = "Admin")]
    public class BrandController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly IWebHostEnvironment
            _webHostEnvironment;

        public BrandController(
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _context =
                context;

            _webHostEnvironment =
                webHostEnvironment;
        }

        // ==================================================
        // GET ALL
        // GET: /api/brands
        // ==================================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken
        )
        {
            var brands =
                await _context.Brands
                    .AsNoTracking()
                    .Select(brand => new
                    {
                        brand.Id,

                        brand.Name,

                        brand.Description,

                        brand.Country,

                        brand.LogoUrl,

                        brand.Status,

                        brand.CreatedAt,

                        brand.UpdatedAt,

                        ProductCount =
                            brand.Products.Count()
                    })
                    .OrderByDescending(
                        brand =>
                            brand.CreatedAt
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            return Ok(
                brands
            );
        }

        // ==================================================
        // CREATE
        // POST: /api/brands
        // multipart/form-data
        // ==================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromForm] BrandModel request,
            CancellationToken cancellationToken
        )
        {
            var brandId =
                request.Id
                    .Trim()
                    .ToUpperInvariant();

            var brandName =
                request.Name
                    .Trim();

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            var country =
                string.IsNullOrWhiteSpace(
                    request.Country
                )
                    ? null
                    : request.Country.Trim();

            // ==============================================
            // VALIDATE
            // ==============================================

            if (
                string.IsNullOrWhiteSpace(
                    brandId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập mã thương hiệu."
                });
            }

            if (
                string.IsNullOrWhiteSpace(
                    brandName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên thương hiệu."
                });
            }

            var exists =
                await _context.Brands
                    .AsNoTracking()
                    .AnyAsync(
                        brand =>
                            brand.Id ==
                            brandId,

                        cancellationToken
                    );

            if (exists)
            {
                return Conflict(new
                {
                    message =
                        $"Mã thương hiệu \"{brandId}\" đã tồn tại."
                });
            }

            // ==============================================
            // UPLOAD LOGO
            // ==============================================

            string? logoUrl =
                null;

            if (
                request.FileAttachments
                != null
            )
            {
                logoUrl =
                    await SaveLogo(
                        request.FileAttachments,
                        cancellationToken
                    );
            }

            // ==============================================
            // CREATE ENTITY
            // ==============================================

            var brand =
                new BrandModel
                {
                    Id =
                        brandId,

                    Name =
                        brandName,

                    Description =
                        description,

                    Country =
                        country,

                    LogoUrl =
                        logoUrl,

                    Status =
                        request.Status,

                    CreatedAt =
                        DateTime.UtcNow,

                    UpdatedAt =
                        null,

                    Products =
                        new List<ProductModel>()
                };

            _context.Brands.Add(
                brand
            );

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken
                );
            }
            catch (DbUpdateException)
            {
                /*
                 * Nếu database lưu thất bại
                 * thì xóa logo vừa upload.
                 */
                DeleteLogoFile(
                    logoUrl
                );

                return Conflict(new
                {
                    message =
                        $"Không thể tạo thương hiệu \"{brandId}\"."
                });
            }

            // ==============================================
            // RESPONSE
            // ==============================================

            return Created(
                $"/api/brands/{Uri.EscapeDataString(brand.Id)}",
                new
                {
                    brand.Id,

                    brand.Name,

                    brand.Description,

                    brand.Country,

                    brand.LogoUrl,

                    brand.Status,

                    brand.CreatedAt,

                    brand.UpdatedAt,

                    ProductCount = 0
                }
            );
        }

        // ==================================================
        // UPDATE
        // PUT: /api/brands/{id}
        // multipart/form-data
        // ==================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            [FromForm] BrandModel request,
            CancellationToken cancellationToken
        )
        {
            var brandId =
                id
                    .Trim()
                    .ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    brandId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã thương hiệu không hợp lệ."
                });
            }

            var brandName =
                request.Name
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    brandName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên thương hiệu."
                });
            }

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            var country =
                string.IsNullOrWhiteSpace(
                    request.Country
                )
                    ? null
                    : request.Country.Trim();

            // ==============================================
            // TÌM BRAND
            // ==============================================

            var brand =
                await _context.Brands
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            brandId,

                        cancellationToken
                    );

            if (brand is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy thương hiệu \"{brandId}\"."
                });
            }

            // ==============================================
            // LƯU LOGO CŨ
            // ==============================================

            var oldLogoUrl =
                brand.LogoUrl;

            string? newLogoUrl =
                null;

            // ==============================================
            // NẾU USER UPLOAD LOGO MỚI
            // ==============================================

            if (
                request.FileAttachments
                != null
            )
            {
                newLogoUrl =
                    await SaveLogo(
                        request.FileAttachments,
                        cancellationToken
                    );

                brand.LogoUrl =
                    newLogoUrl;
            }

            // ==============================================
            // UPDATE DATA
            // ==============================================

            brand.Name =
                brandName;

            brand.Description =
                description;

            brand.Country =
                country;

            brand.Status =
                request.Status;

            brand.UpdatedAt =
                DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken
                );
            }
            catch (DbUpdateException)
            {
                /*
                 * Nếu DB update thất bại,
                 * xóa logo mới vừa upload.
                 */
                if (
                    newLogoUrl != null
                )
                {
                    DeleteLogoFile(
                        newLogoUrl
                    );
                }

                return BadRequest(new
                {
                    message =
                        "Không thể cập nhật thương hiệu."
                });
            }

            // ==============================================
            // DB THÀNH CÔNG
            // → XÓA LOGO CŨ
            // ==============================================

            if (
                newLogoUrl != null &&
                oldLogoUrl != null &&
                oldLogoUrl !=
                newLogoUrl
            )
            {
                DeleteLogoFile(
                    oldLogoUrl
                );
            }

            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.BrandId ==
                            brandId,

                        cancellationToken
                    );

            return Ok(new
            {
                brand.Id,

                brand.Name,

                brand.Description,

                brand.Country,

                brand.LogoUrl,

                brand.Status,

                brand.CreatedAt,

                brand.UpdatedAt,

                ProductCount =
                    productCount
            });
        }

        // ==================================================
        // DELETE
        // DELETE: /api/brands/{id}
        // ==================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken
        )
        {
            var brandId =
                id
                    .Trim()
                    .ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    brandId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã thương hiệu không hợp lệ."
                });
            }

            // ==============================================
            // TÌM BRAND
            // ==============================================

            var brand =
                await _context.Brands
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            brandId,

                        cancellationToken
                    );

            if (brand is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy thương hiệu \"{brandId}\"."
                });
            }

            // ==============================================
            // KIỂM TRA SẢN PHẨM ĐANG SỬ DỤNG BRAND
            // ==============================================

            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.BrandId ==
                            brandId,

                        cancellationToken
                    );

            if (
                productCount > 0
            )
            {
                return Conflict(new
                {
                    message =
                        $"Không thể xóa thương hiệu \"{brand.Name}\" vì đang có {productCount} sản phẩm sử dụng."
                });
            }

            // ==============================================
            // LƯU LOGO ĐỂ XÓA SAU
            // ==============================================

            var logoUrl =
                brand.LogoUrl;

            // ==============================================
            // XÓA DATABASE
            // ==============================================

            _context.Brands.Remove(
                brand
            );

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken
                );
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "Không thể xóa thương hiệu vì dữ liệu đang được sử dụng."
                });
            }

            // ==============================================
            // DB XÓA THÀNH CÔNG
            // → XÓA FILE VẬT LÝ
            // ==============================================

            DeleteLogoFile(
                logoUrl
            );

            return Ok(new
            {
                message =
                    "Xóa thương hiệu thành công.",

                id =
                    brand.Id,

                name =
                    brand.Name
            });
        }

        // ==================================================
        // HÀM LƯU LOGO
        // ==================================================

        private async Task<string> SaveLogo(
            IFormFile file,
            CancellationToken cancellationToken
        )
        {
            /*
             * wwwroot/brands
             */

            var webRootPath =
                _webHostEnvironment.WebRootPath;

            if (
                string.IsNullOrWhiteSpace(
                    webRootPath
                )
            )
            {
                webRootPath =
                    Path.Combine(
                        _webHostEnvironment.ContentRootPath,
                        "wwwroot"
                    );
            }

            var uploadDirectory =
                Path.Combine(
                    webRootPath,
                    "brands"
                );

            if (
                !Directory.Exists(
                    uploadDirectory
                )
            )
            {
                Directory.CreateDirectory(
                    uploadDirectory
                );
            }

            // ==============================================
            // LẤY ĐUÔI FILE
            // ==============================================

            var extension =
                Path.GetExtension(
                    file.FileName
                )
                .ToLowerInvariant();

            var allowedExtensions =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            if (
                !allowedExtensions.Contains(
                    extension
                )
            )
            {
                throw new InvalidOperationException(
                    "Logo chỉ hỗ trợ JPG, JPEG, PNG hoặc WEBP."
                );
            }

            // ==============================================
            // TẠO TÊN FILE KHÔNG TRÙNG
            // ==============================================

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadDirectory,
                    fileName
                );

            // ==============================================
            // LƯU FILE
            // ==============================================

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                );

            await file.CopyToAsync(
                stream,
                cancellationToken
            );

            /*
             * Giá trị lưu Database:
             *
             * /brands/abc.png
             */
            return
                $"/brands/{fileName}";
        }

        // ==================================================
        // HÀM XÓA LOGO
        // ==================================================

        private void DeleteLogoFile(
            string? logoUrl
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    logoUrl
                )
            )
            {
                return;
            }

            var webRootPath =
                _webHostEnvironment.WebRootPath;

            if (
                string.IsNullOrWhiteSpace(
                    webRootPath
                )
            )
            {
                webRootPath =
                    Path.Combine(
                        _webHostEnvironment.ContentRootPath,
                        "wwwroot"
                    );
            }

            /*
             * /brands/abc.png
             *
             * ↓
             *
             * brands/abc.png
             */

            var relativePath =
                logoUrl.TrimStart(
                    '/'
                );

            var filePath =
                Path.Combine(
                    webRootPath,
                    relativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    )
                );

            if (
                System.IO.File.Exists(
                    filePath
                )
            )
            {
                System.IO.File.Delete(
                    filePath
                );
            }
        }
    }
}
