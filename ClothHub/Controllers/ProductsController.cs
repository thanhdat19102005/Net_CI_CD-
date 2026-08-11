using ClothHub.Models;
using ClothHub.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==================================================
        // GET ALL
        // GET: /api/products
        // ==================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products =
                await _context.Products
                    .AsNoTracking()
                    .OrderByDescending(
                        product =>
                            product.CreatedAt
                    )
                    .Select(
                        product =>
                            new
                            {
                                product.Id,

                                product.Name,

                                product.Description,

                                // ==========================
                                // CATEGORY
                                // ==========================

                                product.CategoryId,

                                CategoryName =
                                    product.Category != null
                                        ? product.Category.Name
                                        : null,

                                // ==========================
                                // BRAND
                                // ==========================

                                product.BrandId,

                                BrandName =
                                    product.Brand != null
                                        ? product.Brand.Name
                                        : null,

                                // ==========================
                                // SIZE
                                // ==========================

                                product.SizeId,

                                SizeName =
                                    product.Size != null
                                        ? product.Size.Name
                                        : null,

                                // ==========================
                                // PRODUCT INFORMATION
                                // ==========================

                                product.Material,

                                product.Gender,

                                product.Origin,

                                product.ImportPrice,

                                product.Price,

                                product.Quantity,

                                product.MinimumQuantity,

                                product.ImageUrl,

                                product.Status,

                                product.CreatedAt,

                                product.UpdatedAt
                            }
                    )
                    .ToListAsync();

            return Ok(products);
        }


        // ==================================================
        // GET REFERENCE DATA
        // GET: /api/products/references
        //
        // Lấy TOÀN BỘ dữ liệu Category / Brand / Size
        // từ database để Frontend đổ vào dropdown.
        //
        // Không filter Status để đúng yêu cầu:
        // "load hết database".
        // ==================================================

        [HttpGet("references")]
        public async Task<IActionResult> GetReferences()
        {
            var categories =
                await _context.Categories
                    .AsNoTracking()
                    .OrderBy(
                        category =>
                            category.Name
                    )
                    .Select(
                        category =>
                            new
                            {
                                category.Id,
                                category.Name,
                                category.Status
                            }
                    )
                    .ToListAsync();

            var brands =
                await _context.Brands
                    .AsNoTracking()
                    .OrderBy(
                        brand =>
                            brand.Name
                    )
                    .Select(
                        brand =>
                            new
                            {
                                brand.Id,
                                brand.Name,
                                brand.Status
                            }
                    )
                    .ToListAsync();

            var sizes =
                await _context.Sizes
                    .AsNoTracking()
                    .OrderBy(
                        size =>
                            size.Name
                    )
                    .Select(
                        size =>
                            new
                            {
                                size.Id,
                                size.Name,
                                size.Status
                            }
                    )
                    .ToListAsync();

            return Ok(
                new
                {
                    categories,
                    brands,
                    sizes
                }
            );
        }

        // ==================================================
        // POST
        // POST: /api/products
        // ==================================================

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(
            [FromForm] ProductModel model)
        {
            // ==================================================
            // CHUẨN HÓA DỮ LIỆU
            // ==================================================

            model.Id =
                model.Id
                    .Trim()
                    .ToUpperInvariant();

            model.Name =
                model.Name.Trim();

            model.CategoryId =
                model.CategoryId
                    .Trim()
                    .ToUpperInvariant();

            model.SizeId =
                model.SizeId
                    .Trim()
                    .ToUpperInvariant();

            model.BrandId =
                string.IsNullOrWhiteSpace(
                    model.BrandId
                )
                    ? null
                    : model.BrandId
                        .Trim()
                        .ToUpperInvariant();

            model.Description =
                string.IsNullOrWhiteSpace(
                    model.Description
                )
                    ? null
                    : model.Description.Trim();

            model.Material =
                string.IsNullOrWhiteSpace(
                    model.Material
                )
                    ? null
                    : model.Material.Trim();

            model.Gender =
                string.IsNullOrWhiteSpace(
                    model.Gender
                )
                    ? null
                    : model.Gender.Trim();

            model.Origin =
                string.IsNullOrWhiteSpace(
                    model.Origin
                )
                    ? null
                    : model.Origin.Trim();

            // ==================================================
            // KIỂM TRA PRODUCT ID
            // ==================================================

            var productExists =
                await _context.Products
                    .AnyAsync(
                        product =>
                            product.Id ==
                            model.Id
                    );

            if (productExists)
            {
                return Conflict(
                    new
                    {
                        message =
                            $"Mã sản phẩm '{model.Id}' đã tồn tại."
                    }
                );
            }

            // ==================================================
            // KIỂM TRA CATEGORY
            // ==================================================

            var categoryExists =
                await _context.Categories
                    .AnyAsync(
                        category =>
                            category.Id ==
                            model.CategoryId
                    );

            if (!categoryExists)
            {
                return BadRequest(
                    new
                    {
                        message =
                            $"Danh mục '{model.CategoryId}' không tồn tại."
                    }
                );
            }

            // ==================================================
            // KIỂM TRA SIZE
            // ==================================================

            var sizeExists =
                await _context.Sizes
                    .AnyAsync(
                        size =>
                            size.Id ==
                            model.SizeId
                    );

            if (!sizeExists)
            {
                return BadRequest(
                    new
                    {
                        message =
                            $"Kích thước '{model.SizeId}' không tồn tại."
                    }
                );
            }

            // ==================================================
            // KIỂM TRA BRAND
            //
            // BrandId có thể NULL.
            // Nhưng nếu có giá trị thì Brand phải tồn tại.
            // ==================================================

            if (
                !string.IsNullOrWhiteSpace(
                    model.BrandId
                )
            )
            {
                var brandExists =
                    await _context.Brands
                        .AnyAsync(
                            brand =>
                                brand.Id ==
                                model.BrandId
                        );

                if (!brandExists)
                {
                    return BadRequest(
                        new
                        {
                            message =
                                $"Thương hiệu '{model.BrandId}' không tồn tại."
                        }
                    );
                }
            }

            // ==================================================
            // KIỂM TRA DỮ LIỆU SỐ
            // ==================================================

            if (model.ImportPrice < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Giá nhập không được nhỏ hơn 0."
                    }
                );
            }

            if (model.Price < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Giá bán không được nhỏ hơn 0."
                    }
                );
            }

            if (model.Quantity < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Số lượng không được nhỏ hơn 0."
                    }
                );
            }

            if (model.MinimumQuantity < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Số lượng tối thiểu không được nhỏ hơn 0."
                    }
                );
            }

            if (
                model.Status != 0 &&
                model.Status != 1
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Trạng thái chỉ được là 0 hoặc 1."
                    }
                );
            }

            // ==================================================
            // UPLOAD ẢNH
            // ==================================================

            if (
                model.FileAttachments != null &&
                model.FileAttachments.Length > 0
            )
            {
                var uploadResult =
                    await SaveImageAsync(
                        model.FileAttachments
                    );

                if (!uploadResult.Success)
                {
                    return BadRequest(
                        new
                        {
                            message =
                                uploadResult.ErrorMessage
                        }
                    );
                }

                model.ImageUrl =
                    uploadResult.ImageUrl;
            }

            // ==================================================
            // THÔNG TIN HỆ THỐNG
            // ==================================================

            model.CreatedAt =
                DateTime.UtcNow;

            model.UpdatedAt =
                null;

            /*
             * Không cho EF Core hiểu Navigation Properties
             * là entity mới cần được Insert.
             */
            model.Category = null;
            model.Brand = null;
            model.Size = null;

            // ==================================================
            // INSERT DATABASE
            // ==================================================

            _context.Products.Add(
                model
            );

            await _context.SaveChangesAsync();

            // ==================================================
            // LẤY LẠI SẢN PHẨM SAU KHI TẠO
            // ==================================================

            var createdProduct =
                await GetProductResponse(
                    model.Id
                );

            return Created(
                $"/api/products/{model.Id}",
                createdProduct
            );
        }

        // ==================================================
        // PUT
        // PUT: /api/products/{id}
        // ==================================================

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(
            string id,
            [FromForm] ProductModel model)
        {
            id =
                id
                    .Trim()
                    .ToUpperInvariant();

            var existingProduct =
                await _context.Products
                    .FirstOrDefaultAsync(
                        product =>
                            product.Id ==
                            id
                    );

            if (existingProduct == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            $"Không tìm thấy sản phẩm '{id}'."
                    }
                );
            }

            // ==================================================
            // KHÔNG CHO ĐỔI PRIMARY KEY
            // ==================================================

            if (
                !string.IsNullOrWhiteSpace(
                    model.Id
                ) &&
                !string.Equals(
                    model.Id.Trim(),
                    id,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Mã sản phẩm không thể thay đổi."
                    }
                );
            }

            // ==================================================
            // CHUẨN HÓA
            // ==================================================

            var categoryId =
                model.CategoryId
                    .Trim()
                    .ToUpperInvariant();

            var sizeId =
                model.SizeId
                    .Trim()
                    .ToUpperInvariant();

            var brandId =
                string.IsNullOrWhiteSpace(
                    model.BrandId
                )
                    ? null
                    : model.BrandId
                        .Trim()
                        .ToUpperInvariant();

            // ==================================================
            // KIỂM TRA CATEGORY
            // ==================================================

            var categoryExists =
                await _context.Categories
                    .AnyAsync(
                        category =>
                            category.Id ==
                            categoryId
                    );

            if (!categoryExists)
            {
                return BadRequest(
                    new
                    {
                        message =
                            $"Danh mục '{categoryId}' không tồn tại."
                    }
                );
            }

            // ==================================================
            // KIỂM TRA SIZE
            // ==================================================

            var sizeExists =
                await _context.Sizes
                    .AnyAsync(
                        size =>
                            size.Id ==
                            sizeId
                    );

            if (!sizeExists)
            {
                return BadRequest(
                    new
                    {
                        message =
                            $"Kích thước '{sizeId}' không tồn tại."
                    }
                );
            }

            // ==================================================
            // KIỂM TRA BRAND
            // ==================================================

            if (brandId != null)
            {
                var brandExists =
                    await _context.Brands
                        .AnyAsync(
                            brand =>
                                brand.Id ==
                                brandId
                        );

                if (!brandExists)
                {
                    return BadRequest(
                        new
                        {
                            message =
                                $"Thương hiệu '{brandId}' không tồn tại."
                        }
                    );
                }
            }

            // ==================================================
            // VALIDATE
            // ==================================================

            if (model.ImportPrice < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Giá nhập không được nhỏ hơn 0."
                    }
                );
            }

            if (model.Price < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Giá bán không được nhỏ hơn 0."
                    }
                );
            }

            if (model.Quantity < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Số lượng không được nhỏ hơn 0."
                    }
                );
            }

            if (model.MinimumQuantity < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Số lượng tối thiểu không được nhỏ hơn 0."
                    }
                );
            }

            if (
                model.Status != 0 &&
                model.Status != 1
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Trạng thái chỉ được là 0 hoặc 1."
                    }
                );
            }

            // ==================================================
            // CẬP NHẬT ẢNH
            // ==================================================

            if (
                model.FileAttachments != null &&
                model.FileAttachments.Length > 0
            )
            {
                var oldImageUrl =
                    existingProduct.ImageUrl;

                var uploadResult =
                    await SaveImageAsync(
                        model.FileAttachments
                    );

                if (!uploadResult.Success)
                {
                    return BadRequest(
                        new
                        {
                            message =
                                uploadResult.ErrorMessage
                        }
                    );
                }

                existingProduct.ImageUrl =
                    uploadResult.ImageUrl;

                DeleteImage(
                    oldImageUrl
                );
            }

            // ==================================================
            // UPDATE FIELD
            // ==================================================

            existingProduct.Name =
                model.Name.Trim();

            existingProduct.Description =
                string.IsNullOrWhiteSpace(
                    model.Description
                )
                    ? null
                    : model.Description.Trim();

            existingProduct.CategoryId =
                categoryId;

            existingProduct.BrandId =
                brandId;

            existingProduct.SizeId =
                sizeId;

            existingProduct.Material =
                string.IsNullOrWhiteSpace(
                    model.Material
                )
                    ? null
                    : model.Material.Trim();

            existingProduct.Gender =
                string.IsNullOrWhiteSpace(
                    model.Gender
                )
                    ? null
                    : model.Gender.Trim();

            existingProduct.Origin =
                string.IsNullOrWhiteSpace(
                    model.Origin
                )
                    ? null
                    : model.Origin.Trim();

            existingProduct.ImportPrice =
                model.ImportPrice;

            existingProduct.Price =
                model.Price;

            existingProduct.Quantity =
                model.Quantity;

            existingProduct.MinimumQuantity =
                model.MinimumQuantity;

            existingProduct.Status =
                model.Status;

            /*
             * CreatedAt giữ nguyên.
             * Chỉ cập nhật UpdatedAt.
             */
            existingProduct.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ==================================================
            // RESPONSE
            // ==================================================

            var updatedProduct =
                await GetProductResponse(
                    existingProduct.Id
                );

            return Ok(
                new
                {
                    message =
                        "Cập nhật sản phẩm thành công.",

                    product =
                        updatedProduct
                }
            );
        }

        // ==================================================
        // DELETE
        // DELETE: /api/products/{id}
        // ==================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id)
        {
            id =
                id
                    .Trim()
                    .ToUpperInvariant();

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            id
                    );

            if (product == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            $"Không tìm thấy sản phẩm '{id}'."
                    }
                );
            }

            var imageUrl =
                product.ImageUrl;

            _context.Products.Remove(
                product
            );

            await _context.SaveChangesAsync();

            // ==================================================
            // XÓA FILE ẢNH SAU KHI DB XÓA THÀNH CÔNG
            // ==================================================

            DeleteImage(
                imageUrl
            );

            return Ok(
                new
                {
                    message =
                        "Xóa sản phẩm thành công.",

                    id =
                        product.Id,

                    name =
                        product.Name
                }
            );
        }

        // ==================================================
        // RESPONSE PRODUCT
        // ==================================================

        private async Task<object?> GetProductResponse(
            string id)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(
                    product =>
                        product.Id ==
                        id
                )
                .Select(
                    product =>
                        new
                        {
                            product.Id,

                            product.Name,

                            product.Description,

                            // ==========================
                            // CATEGORY
                            // ==========================

                            product.CategoryId,

                            CategoryName =
                                product.Category != null
                                    ? product.Category.Name
                                    : null,

                            // ==========================
                            // BRAND
                            // ==========================

                            product.BrandId,

                            BrandName =
                                product.Brand != null
                                    ? product.Brand.Name
                                    : null,

                            // ==========================
                            // SIZE
                            // ==========================

                            product.SizeId,

                            SizeName =
                                product.Size != null
                                    ? product.Size.Name
                                    : null,

                            // ==========================
                            // PRODUCT
                            // ==========================

                            product.Material,

                            product.Gender,

                            product.Origin,

                            product.ImportPrice,

                            product.Price,

                            product.Quantity,

                            product.MinimumQuantity,

                            product.ImageUrl,

                            product.Status,

                            product.CreatedAt,

                            product.UpdatedAt
                        }
                )
                .FirstOrDefaultAsync();
        }

        // ==================================================
        // SAVE IMAGE
        // ==================================================

        private async Task<(
            bool Success,
            string? ImageUrl,
            string? ErrorMessage
        )> SaveImageAsync(
            IFormFile file)
        {
            // ==================================================
            // KIỂM TRA SIZE
            // Maximum 5 MB
            // ==================================================

            const long maxFileSize =
                5 * 1024 * 1024;

            if (
                file.Length <= 0
            )
            {
                return (
                    false,
                    null,
                    "File ảnh không hợp lệ."
                );
            }

            if (
                file.Length >
                maxFileSize
            )
            {
                return (
                    false,
                    null,
                    "Ảnh không được vượt quá 5MB."
                );
            }

            // ==================================================
            // KIỂM TRA EXTENSION
            // ==================================================

            var allowedExtensions =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            var extension =
                Path.GetExtension(
                    file.FileName
                )
                .ToLowerInvariant();

            if (
                !allowedExtensions.Contains(
                    extension
                )
            )
            {
                return (
                    false,
                    null,
                    "Chỉ hỗ trợ ảnh JPG, JPEG, PNG hoặc WEBP."
                );
            }

            // ==================================================
            // WWWROOT
            // ==================================================

            var webRootPath =
                _environment.WebRootPath;

            if (
                string.IsNullOrWhiteSpace(
                    webRootPath
                )
            )
            {
                webRootPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );
            }

            var productFolder =
                Path.Combine(
                    webRootPath,
                    "products"
                );

            if (
                !Directory.Exists(
                    productFolder
                )
            )
            {
                Directory.CreateDirectory(
                    productFolder
                );
            }

            // ==================================================
            // RANDOM FILE NAME
            // ==================================================

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    productFolder,
                    fileName
                );

            // ==================================================
            // SAVE
            // ==================================================

            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    )
            )
            {
                await file.CopyToAsync(
                    stream
                );
            }

            var imageUrl =
                $"/products/{fileName}";

            return (
                true,
                imageUrl,
                null
            );
        }

        // ==================================================
        // DELETE IMAGE
        // ==================================================

        private void DeleteImage(
            string? imageUrl)
        {
            if (
                string.IsNullOrWhiteSpace(
                    imageUrl
                )
            )
            {
                return;
            }

            /*
             * Chỉ xóa file thuộc /products/
             * để tránh xóa nhầm file ngoài thư mục.
             */
            if (
                !imageUrl.StartsWith(
                    "/products/",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return;
            }

            var webRootPath =
                _environment.WebRootPath;

            if (
                string.IsNullOrWhiteSpace(
                    webRootPath
                )
            )
            {
                webRootPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );
            }

            var relativePath =
                imageUrl
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    );

            var physicalPath =
                Path.Combine(
                    webRootPath,
                    relativePath
                );

            if (
                System.IO.File.Exists(
                    physicalPath
                )
            )
            {
                System.IO.File.Delete(
                    physicalPath
                );
            }
        }
    }
}
