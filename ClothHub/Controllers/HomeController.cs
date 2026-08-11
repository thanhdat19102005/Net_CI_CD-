using Azure.Core;
using ClothHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothHub.Controllers
{
    /// <summary>
    /// API public dành cho trang Home.
    ///
    /// Chức năng:
    /// - Load danh mục đang hoạt động.
    /// - Load sản phẩm đang hoạt động.
    /// - Lọc sản phẩm theo Category.
    /// - Hỗ trợ tìm kiếm, Brand và khoảng giá.
    ///
    /// ImageUrl trả về dạng đường dẫn tương đối:
    /// products/4324674d9aca4350b5020cb70fff7d94.png
    ///
    /// Frontend tự nối domain API.
    /// </summary>
    [ApiController]
    [Route("api/home")]
    [AllowAnonymous]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HomeController(
            AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 1. GET CATEGORIES
        //
        // GET:
        // /api/home/categories
        //
        // Load danh mục để hiển thị sidebar bên trái.
        //
        // "all" không phải dữ liệu thật trong database.
        // Backend tự thêm "Tất cả sản phẩm" vào đầu danh sách.
        // =========================================================

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            // -----------------------------------------------------
            // Tổng số sản phẩm đang hoạt động
            // và thuộc Category đang hoạt động.
            // -----------------------------------------------------

            var totalProducts =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.Status == 1 &&
                            product.Category != null &&
                            product.Category.Status == 1
                    );

            // -----------------------------------------------------
            // Load Category đang hoạt động.
            //
            // category.Products.Count(...) tương ứng việc
            // liên kết Category -> Product theo CategoryId.
            // -----------------------------------------------------

            var categories =
                await _context.Categories
                    .AsNoTracking()
                    .Where(
                        category =>
                            category.Status == 1
                    )
                    .OrderBy(
                        category =>
                            category.Name
                    )
                    .Select(
                        category =>
                            new HomeCategoryResponse
                            {
                                Id =
                                    category.Id,

                                Name =
                                    category.Name,

                                Description =
                                    category.Description,

                                ProductCount =
                                    category.Products.Count(
                                        product =>
                                            product.Status == 1
                                    )
                            }
                    )
                    .ToListAsync();

            // -----------------------------------------------------
            // Thêm item "Tất cả sản phẩm"
            // để frontend dùng giống UI.
            // -----------------------------------------------------

            var result =
                new List<HomeCategoryResponse>
                {
                    new HomeCategoryResponse
                    {
                        Id = "all",

                        Name =
                            "Tất cả sản phẩm",

                        Description =
                            null,

                        ProductCount =
                            totalProducts
                    }
                };

            result.AddRange(
                categories
            );

            return Ok(
                result
            );
        }

        // =========================================================
        // 2. GET PRODUCTS
        //
        // GET:
        // /api/home/products
        //
        // Tất cả:
        // /api/home/products
        //
        // hoặc:
        // /api/home/products?categoryId=all
        //
        // Theo Category:
        // /api/home/products?categoryId=AOTHUN
        //
        // Search:
        // /api/home/products?search=polo
        //
        // Brand:
        // /api/home/products?brandId=ADIDAS
        //
        // Giá:
        // /api/home/products?maxPrice=500000
        //
        // Có thể kết hợp:
        // /api/home/products
        // ?categoryId=AOTHUN
        // &brandId=ADIDAS
        // &search=basic
        // &maxPrice=500000
        // =========================================================

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? brandId = null,
            [FromQuery] decimal? maxPrice = null)
        {
            return await LoadProducts(
                categoryId,
                search,
                brandId,
                maxPrice
            );
        }

        // =========================================================
        // 3. GET PRODUCTS BY CATEGORY
        //
        // GET:
        // /api/home/categories/{categoryId}/products
        //
        // Ví dụ:
        // /api/home/categories/AOTHUN/products
        //
        // Khi click "Tất cả sản phẩm":
        // /api/home/categories/all/products
        // =========================================================

        [HttpGet(
            "categories/{categoryId}/products"
        )]
        public async Task<IActionResult>
            GetProductsByCategory(
                string categoryId)
        {
            categoryId =
                categoryId.Trim();

            // -----------------------------------------------------
            // Nếu khác "all" thì kiểm tra Category
            // có tồn tại và đang hoạt động hay không.
            // -----------------------------------------------------

            if (!categoryId.Equals(
                    "all",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                var categoryExists =
                    await _context.Categories
                        .AsNoTracking()
                        .AnyAsync(
                            category =>
                                category.Id ==
                                categoryId &&
                                category.Status == 1
                        );

                if (!categoryExists)
                {
                    return NotFound(
                        new
                        {
                            message =
                                $"Không tìm thấy danh mục '{categoryId}' hoặc danh mục đã ngừng hoạt động."
                        }
                    );
                }
            }

            return await LoadProducts(
                categoryId,
                null,
                null,
                null
            );
        }

        // =========================================================
        // PRIVATE
        // LOAD PRODUCT
        // =========================================================

        private async Task<IActionResult> LoadProducts(
            string? categoryId,
            string? search,
            string? brandId,
            decimal? maxPrice)
        {
            // -----------------------------------------------------
            // QUERY GỐC
            //
            // Chỉ load:
            // Product.Status = 1
            // Category.Status = 1
            //
            // Không cần Include() vì chúng ta Select trực tiếp
            // Category.Name / Brand.Name / Size.Name.
            //
            // EF Core tự sinh JOIN khi cần.
            // -----------------------------------------------------

            var query =
                _context.Products
                    .AsNoTracking()
                    .Where(
                        product =>
                            product.Status == 1 &&
                            product.Category != null &&
                            product.Category.Status == 1
                    );

            // =====================================================
            // FILTER CATEGORY
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    categoryId
                ) &&
                !categoryId.Equals(
                    "all",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                categoryId =
                    categoryId.Trim();

                query =
                    query.Where(
                        product =>
                            product.CategoryId ==
                            categoryId
                    );
            }

            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    search
                ))
            {
                search =
                    search.Trim();

                query =
                    query.Where(
                        product =>
                            product.Name.Contains(
                                search
                            ) ||

                            (
                                product.Description != null &&
                                product.Description.Contains(
                                    search
                                )
                            ) ||

                            (
                                product.Category != null &&
                                product.Category.Name.Contains(
                                    search
                                )
                            ) ||

                            (
                                product.Brand != null &&
                                product.Brand.Name.Contains(
                                    search
                                )
                            )
                    );
            }

            // =====================================================
            // FILTER BRAND
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    brandId
                ))
            {
                brandId =
                    brandId.Trim();

                query =
                    query.Where(
                        product =>
                            product.BrandId ==
                            brandId
                    );
            }

            // =====================================================
            // FILTER MAX PRICE
            // =====================================================

            if (maxPrice.HasValue)
            {
                query =
                    query.Where(
                        product =>
                            product.Price <=
                            maxPrice.Value
                    );
            }

            // =====================================================
            // SELECT PRODUCT
            //
            // ImportPrice không trả về frontend khách hàng.
            //
            // ImageUrl được lấy nguyên từ database trước,
            // sau đó NormalizeImageUrl() chỉ bỏ dấu "/" đầu.
            // =====================================================

            var rows =
                await query
                    .OrderByDescending(
                        product =>
                            product.CreatedAt
                    )
                    .Select(
                        product =>
                            new HomeProductQueryRow
                            {
                                Id =
                                    product.Id,

                                Name =
                                    product.Name,

                                Description =
                                    product.Description,

                                // ================================
                                // CATEGORY
                                // ================================

                                CategoryId =
                                    product.CategoryId,

                                CategoryName =
                                    product.Category != null
                                        ? product.Category.Name
                                        : string.Empty,

                                // ================================
                                // BRAND
                                // ================================

                                BrandId =
                                    product.BrandId,

                                BrandName =
                                    product.Brand != null
                                        ? product.Brand.Name
                                        : null,

                                // ================================
                                // SIZE
                                // ================================

                                SizeId =
                                    product.SizeId,

                                SizeName =
                                    product.Size != null
                                        ? product.Size.Name
                                        : null,

                                // ================================
                                // PRODUCT INFORMATION
                                // ================================

                                Material =
                                    product.Material,

                                Gender =
                                    product.Gender,

                                Origin =
                                    product.Origin,

                                Price =
                                    product.Price,

                                Quantity =
                                    product.Quantity,

                                MinimumQuantity =
                                    product.MinimumQuantity,

                                ImageUrl =
                                    product.ImageUrl
                            }
                    )
                    .ToListAsync();

            // =====================================================
            // RESPONSE
            // =====================================================

            var products =
                rows.Select(
                    product =>
                        new HomeProductResponse
                        {
                            Id =
                                product.Id,

                            Name =
                                product.Name,

                            Description =
                                product.Description,

                            // ================================
                            // CATEGORY
                            // ================================

                            CategoryId =
                                product.CategoryId,

                            CategoryName =
                                product.CategoryName,

                            // ================================
                            // BRAND
                            // ================================

                            BrandId =
                                product.BrandId,

                            BrandName =
                                product.BrandName,

                            // ================================
                            // SIZE
                            // ================================

                            SizeId =
                                product.SizeId,

                            SizeName =
                                product.SizeName,

                            // ================================
                            // PRODUCT INFORMATION
                            // ================================

                            Material =
                                product.Material,

                            Gender =
                                product.Gender,

                            Origin =
                                product.Origin,

                            Price =
                                product.Price,

                            Quantity =
                                product.Quantity,

                            MinimumQuantity =
                                product.MinimumQuantity,

                            // ================================
                            // IMAGE
                            //
                            // Database:
                            // /products/abc.png
                            //
                            // API:
                            // products/abc.png
                            // ================================

                            ImageUrl =
                                NormalizeImageUrl(
                                    product.ImageUrl
                                )
                        }
                )
                .ToList();

            return Ok(
                products
            );
        }

        // =========================================================
        // NORMALIZE IMAGE URL
        //
        // Database:
        // /products/4324674d9aca4350b5020cb70fff7d94.png
        //
        // API response:
        // products/4324674d9aca4350b5020cb70fff7d94.png
        //
        // Frontend tự nối domain:
        //
        // apiBaseUrl + '/' + product.imageUrl
        //
        // Ví dụ:
        // https://localhost:7010/
        // +
        // products/abc.png
        //
        // =>
        // https://localhost:7010/products/abc.png
        // =========================================================

        private static string? NormalizeImageUrl(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    imageUrl
                ))
            {
                return null;
            }

            return imageUrl
                .Trim()
                .TrimStart('/');
        }

        // =========================================================
        // INTERNAL QUERY ROW
        //
        // Dùng làm object trung gian sau khi query EF Core.
        // =========================================================

        private sealed class HomeProductQueryRow
        {
            public string Id { get; set; }
                = string.Empty;

            public string Name { get; set; }
                = string.Empty;

            public string? Description { get; set; }

            // =====================================================
            // CATEGORY
            // =====================================================

            public string CategoryId { get; set; }
                = string.Empty;

            public string CategoryName { get; set; }
                = string.Empty;

            // =====================================================
            // BRAND
            // =====================================================

            public string? BrandId { get; set; }

            public string? BrandName { get; set; }

            // =====================================================
            // SIZE
            // =====================================================

            public string SizeId { get; set; }
                = string.Empty;

            public string? SizeName { get; set; }

            // =====================================================
            // PRODUCT
            // =====================================================

            public string? Material { get; set; }

            public string? Gender { get; set; }

            public string? Origin { get; set; }

            public decimal Price { get; set; }

            public int Quantity { get; set; }

            public int MinimumQuantity { get; set; }

            public string? ImageUrl { get; set; }
        }
    }

    // =============================================================
    // RESPONSE CATEGORY
    // =============================================================

    public sealed class HomeCategoryResponse
    {
        public string Id { get; set; }
            = string.Empty;

        public string Name { get; set; }
            = string.Empty;

        public string? Description { get; set; }

        public int ProductCount { get; set; }
    }

    // =============================================================
    // RESPONSE PRODUCT
    // =============================================================

    public sealed class HomeProductResponse
    {
        public string Id { get; set; }
            = string.Empty;

        public string Name { get; set; }
            = string.Empty;

        public string? Description { get; set; }

        // =========================================================
        // CATEGORY
        // =========================================================

        public string CategoryId { get; set; }
            = string.Empty;

        public string CategoryName { get; set; }
            = string.Empty;

        // =========================================================
        // BRAND
        // =========================================================

        public string? BrandId { get; set; }

        public string? BrandName { get; set; }

        // =========================================================
        // SIZE
        // =========================================================

        public string SizeId { get; set; }
            = string.Empty;

        public string? SizeName { get; set; }

        // =========================================================
        // PRODUCT
        // =========================================================

        public string? Material { get; set; }

        public string? Gender { get; set; }

        public string? Origin { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public int MinimumQuantity { get; set; }

        public string? ImageUrl { get; set; }
    }
}
