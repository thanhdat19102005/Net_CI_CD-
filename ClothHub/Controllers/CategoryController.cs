using ClothHub.Models;
using ClothHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/categories")]

    /*
     * Chỉ tài khoản có Role Admin
     * mới gọi được API trong Controller này.
     *
     * Chưa đăng nhập  → 401 Unauthorized
     * Có đăng nhập nhưng không phải Admin → 403 Forbidden
     */
    [Authorize(Roles = "Admin")]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(
            AppDbContext context
        )
        {
            _context = context;
        }

        // ==================================================
        // GET ALL CATEGORIES
        // GET: /api/categories
        // Chỉ Admin được truy cập
        // ==================================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken
        )
        {
            var categories =
                await _context.Categories
                    .AsNoTracking()
                    .Select(category => new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.Status,
                        category.CreatedAt,
                        category.UpdatedAt,

                        ProductCount =
                            category.Products.Count()
                    })
                    .OrderByDescending(
                        category =>
                            category.CreatedAt
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            return Ok(categories);
        }




        // ==================================================
        // CREATE CATEGORY
        // POST: /api/categories
        // ==================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CategoryModel request,
            CancellationToken cancellationToken
        )
        {
            /*
             * [ApiController] tự kiểm tra:
             *
             * Id:
             * - Bắt buộc
             * - Tối đa 20 ký tự
             *
             * Name:
             * - Bắt buộc
             * - Tối đa 100 ký tự
             *
             * Description:
             * - Tối đa 500 ký tự
             *
             * Status:
             * - Chỉ nhận 0 hoặc 1
             */

            var categoryId =
                request.Id
                    .Trim()
                    .ToUpperInvariant();

            var categoryName =
                request.Name.Trim();

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            /*
             * Kiểm tra lại sau khi Trim để tránh
             * người dùng chỉ nhập khoảng trắng.
             */

            if (
                string.IsNullOrWhiteSpace(
                    categoryId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập mã danh mục."
                });
            }

            if (
                string.IsNullOrWhiteSpace(
                    categoryName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên danh mục."
                });
            }

            /*
             * Kiểm tra mã danh mục đã tồn tại.
             */

            var categoryExists =
                await _context.Categories
                    .AsNoTracking()
                    .AnyAsync(
                        category =>
                            category.Id ==
                            categoryId,

                        cancellationToken
                    );

            if (categoryExists)
            {
                return Conflict(new
                {
                    message =
                        $"Mã danh mục \"{categoryId}\" đã tồn tại."
                });
            }

            /*
             * Tạo một Entity mới.
             *
             * Không lưu trực tiếp request để tránh
             * client tự gửi CreatedAt, UpdatedAt
             * hoặc Products.
             */

            var category =
                new CategoryModel
                {
                    Id =
                        categoryId,

                    Name =
                        categoryName,

                    Description =
                        description,

                    Status =
                        request.Status,

                    CreatedAt =
                        DateTime.UtcNow,

                    UpdatedAt =
                        null,

                    Products =
                        new List<ProductModel>()
                };

            _context.Categories.Add(
                category
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
                        $"Không thể tạo danh mục. Mã \"{categoryId}\" có thể đã tồn tại."
                });
            }

            /*
             * Danh mục mới chưa có sản phẩm,
             * vì vậy ProductCount bằng 0.
             */

            var response =
                new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.Status,
                    category.CreatedAt,
                    category.UpdatedAt,
                    ProductCount = 0
                };

            /*
             * Trả HTTP 201 Created.
             */

            return Created(
                $"/api/categories/{Uri.EscapeDataString(category.Id)}",
                response
            );
        }





        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
    string id,
    [FromBody] CategoryModel request,
    CancellationToken cancellationToken
)
        {
            var categoryId =
                id.Trim().ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    categoryId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã danh mục không hợp lệ."
                });
            }

            var categoryName =
                request.Name.Trim();

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            if (
                string.IsNullOrWhiteSpace(
                    categoryName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên danh mục."
                });
            }

            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == categoryId,
                        cancellationToken
                    );

            if (category is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy danh mục có mã \"{categoryId}\"."
                });
            }

            category.Name =
                categoryName;

            category.Description =
                description;

            category.Status =
                request.Status;

            category.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync(
                cancellationToken
            );

            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.CategoryId ==
                            categoryId,
                        cancellationToken
                    );

            return Ok(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.Status,
                category.CreatedAt,
                category.UpdatedAt,
                ProductCount =
                    productCount
            });
        }




        // ==================================================
        // DELETE CATEGORY
        // DELETE: /api/categories/{id}
        // Chỉ Admin được truy cập
        // ==================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken
        )
        {
            var categoryId =
                id.Trim().ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    categoryId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã danh mục không hợp lệ."
                });
            }

            /*
             * Tìm danh mục cần xóa.
             */
            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == categoryId,

                        cancellationToken
                    );

            if (category is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy danh mục có mã \"{categoryId}\"."
                });
            }

            /*
             * Kiểm tra danh mục có đang được
             * sản phẩm nào sử dụng hay không.
             *
             * Không cho xóa để tránh lỗi khóa ngoại
             * và mất liên kết dữ liệu.
             */
            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.CategoryId ==
                            categoryId,

                        cancellationToken
                    );

            if (productCount > 0)
            {
                return Conflict(new
                {
                    message =
                        $"Không thể xóa danh mục \"{category.Name}\" vì đang có {productCount} sản phẩm sử dụng."
                });
            }

            /*
             * Đánh dấu Entity để xóa.
             */
            _context.Categories.Remove(
                category
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
                        $"Không thể xóa danh mục \"{category.Name}\" vì dữ liệu đang được sử dụng."
                });
            }

            return Ok(new
            {
                message =
                    $"Đã xóa danh mục \"{category.Name}\" thành công.",

                deletedCategory = new
                {
                    category.Id,
                    category.Name
                }
            });
        }

















    }
}
