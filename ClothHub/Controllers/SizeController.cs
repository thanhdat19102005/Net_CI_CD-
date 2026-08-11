using ClothHub.Models;
using ClothHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/sizes")]

    /*
     * Chỉ tài khoản có Role Admin
     * mới gọi được API trong Controller này.
     *
     * Chưa đăng nhập:
     * → 401 Unauthorized
     *
     * Có đăng nhập nhưng không phải Admin:
     * → 403 Forbidden
     */
    [Authorize(Roles = "Admin")]
    public class SizeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SizeController(
            AppDbContext context
        )
        {
            _context = context;
        }

        // ==================================================
        // GET ALL SIZES
        // GET: /api/sizes
        // ==================================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken
        )
        {
            var sizes =
                await _context.Sizes
                    .AsNoTracking()
                    .Select(size => new
                    {
                        size.Id,

                        size.Name,

                        size.Description,

                        size.Status,

                        size.CreatedAt,

                        size.UpdatedAt,

                        ProductCount =
                            size.Products.Count()
                    })
                    .OrderByDescending(
                        size =>
                            size.CreatedAt
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            return Ok(sizes);
        }

        // ==================================================
        // CREATE SIZE
        // POST: /api/sizes
        // ==================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SizeModel request,
            CancellationToken cancellationToken
        )
        {
            /*
             * Chuẩn hóa mã kích thước.
             *
             * Ví dụ:
             * " size_m "
             *
             * →
             *
             * "SIZE_M"
             */

            var sizeId =
                request.Id
                    .Trim()
                    .ToUpperInvariant();

            var sizeName =
                request.Name.Trim();

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            // ==================================================
            // KIỂM TRA ID
            // ==================================================

            if (
                string.IsNullOrWhiteSpace(
                    sizeId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập mã kích thước."
                });
            }

            /*
             * SizeModel quy định:
             *
             * [StringLength(20)]
             *
             * nhưng kiểm tra thêm ở đây
             * để message rõ ràng hơn.
             */

            if (
                sizeId.Length > 20
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã kích thước không được vượt quá 20 ký tự."
                });
            }

            // ==================================================
            // KIỂM TRA NAME
            // ==================================================

            if (
                string.IsNullOrWhiteSpace(
                    sizeName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên kích thước."
                });
            }

            if (
                sizeName.Length > 50
            )
            {
                return BadRequest(new
                {
                    message =
                        "Tên kích thước không được vượt quá 50 ký tự."
                });
            }

            // ==================================================
            // KIỂM TRA DESCRIPTION
            // ==================================================

            if (
                description is not null &&
                description.Length > 255
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mô tả không được vượt quá 255 ký tự."
                });
            }

            // ==================================================
            // KIỂM TRA STATUS
            // ==================================================

            if (
                request.Status != 0 &&
                request.Status != 1
            )
            {
                return BadRequest(new
                {
                    message =
                        "Trạng thái chỉ được phép là 0 hoặc 1."
                });
            }

            // ==================================================
            // KIỂM TRA ID ĐÃ TỒN TẠI
            // ==================================================

            var sizeExists =
                await _context.Sizes
                    .AsNoTracking()
                    .AnyAsync(
                        size =>
                            size.Id ==
                            sizeId,

                        cancellationToken
                    );

            if (sizeExists)
            {
                return Conflict(new
                {
                    message =
                        $"Mã kích thước \"{sizeId}\" đã tồn tại."
                });
            }

            // ==================================================
            // TẠO ENTITY
            // ==================================================

            var size =
                new SizeModel
                {
                    Id =
                        sizeId,

                    Name =
                        sizeName,

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

            _context.Sizes.Add(
                size
            );

            try
            {
                await _context
                    .SaveChangesAsync(
                        cancellationToken
                    );
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        $"Không thể tạo kích thước. Mã \"{sizeId}\" có thể đã tồn tại."
                });
            }

            // ==================================================
            // RESPONSE
            // ==================================================

            var response =
                new
                {
                    size.Id,

                    size.Name,

                    size.Description,

                    size.Status,

                    size.CreatedAt,

                    size.UpdatedAt,

                    ProductCount = 0
                };

            /*
             * HTTP 201 Created
             */

            return Created(
                $"/api/sizes/{Uri.EscapeDataString(size.Id)}",
                response
            );
        }

        // ==================================================
        // UPDATE SIZE
        // PUT: /api/sizes/{id}
        // ==================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] SizeModel request,
            CancellationToken cancellationToken
        )
        {
            /*
             * ID nằm trên URL.
             *
             * Ví dụ:
             *
             * PUT /api/sizes/SIZE_M
             *
             * Không cho phép client
             * thay đổi Id của Size.
             */

            var sizeId =
                id
                    .Trim()
                    .ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    sizeId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã kích thước không hợp lệ."
                });
            }

            // ==================================================
            // CHUẨN HÓA DỮ LIỆU
            // ==================================================

            var sizeName =
                request.Name.Trim();

            var description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim();

            // ==================================================
            // VALIDATE NAME
            // ==================================================

            if (
                string.IsNullOrWhiteSpace(
                    sizeName
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Vui lòng nhập tên kích thước."
                });
            }

            if (
                sizeName.Length > 50
            )
            {
                return BadRequest(new
                {
                    message =
                        "Tên kích thước không được vượt quá 50 ký tự."
                });
            }

            // ==================================================
            // VALIDATE DESCRIPTION
            // ==================================================

            if (
                description is not null &&
                description.Length > 255
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mô tả không được vượt quá 255 ký tự."
                });
            }

            // ==================================================
            // VALIDATE STATUS
            // ==================================================

            if (
                request.Status != 0 &&
                request.Status != 1
            )
            {
                return BadRequest(new
                {
                    message =
                        "Trạng thái chỉ được phép là 0 hoặc 1."
                });
            }

            // ==================================================
            // TÌM SIZE
            // ==================================================

            var size =
                await _context.Sizes
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            sizeId,

                        cancellationToken
                    );

            if (size is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy kích thước có mã \"{sizeId}\"."
                });
            }

            // ==================================================
            // UPDATE
            // ==================================================

            size.Name =
                sizeName;

            size.Description =
                description;

            size.Status =
                request.Status;

            size.UpdatedAt =
                DateTime.UtcNow;

            try
            {
                await _context
                    .SaveChangesAsync(
                        cancellationToken
                    );
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    message =
                        "Không thể cập nhật kích thước."
                });
            }

            // ==================================================
            // ĐẾM SẢN PHẨM LIÊN QUAN
            // ==================================================

            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.SizeId ==
                            sizeId,

                        cancellationToken
                    );

            // ==================================================
            // RESPONSE
            // ==================================================

            return Ok(new
            {
                size.Id,

                size.Name,

                size.Description,

                size.Status,

                size.CreatedAt,

                size.UpdatedAt,

                ProductCount =
                    productCount
            });
        }

        // ==================================================
        // DELETE SIZE
        // DELETE: /api/sizes/{id}
        // ==================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken
        )
        {
            var sizeId =
                id
                    .Trim()
                    .ToUpperInvariant();

            if (
                string.IsNullOrWhiteSpace(
                    sizeId
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Mã kích thước không hợp lệ."
                });
            }

            // ==================================================
            // TÌM SIZE
            // ==================================================

            var size =
                await _context.Sizes
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            sizeId,

                        cancellationToken
                    );

            if (size is null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy kích thước có mã \"{sizeId}\"."
                });
            }

            // ==================================================
            // KIỂM TRA SẢN PHẨM ĐANG SỬ DỤNG SIZE
            // ==================================================

            var productCount =
                await _context.Products
                    .AsNoTracking()
                    .CountAsync(
                        product =>
                            product.SizeId ==
                            sizeId,

                        cancellationToken
                    );

            /*
             * Nếu đang có Product sử dụng Size này
             * thì không xóa.
             *
             * Tránh lỗi khóa ngoại và tránh
             * làm mất liên kết dữ liệu.
             */

            if (
                productCount > 0
            )
            {
                return Conflict(new
                {
                    message =
                        $"Không thể xóa kích thước \"{size.Name}\" vì đang có {productCount} sản phẩm sử dụng."
                });
            }

            // ==================================================
            // DELETE
            // ==================================================

            _context.Sizes.Remove(
                size
            );

            try
            {
                await _context
                    .SaveChangesAsync(
                        cancellationToken
                    );
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        $"Không thể xóa kích thước \"{size.Name}\" vì đang có dữ liệu liên quan."
                });
            }

            // ==================================================
            // RESPONSE
            // ==================================================

            return Ok(new
            {
                message =
                    $"Đã xóa kích thước \"{size.Name}\" thành công.",

                id =
                    size.Id
            });
        }
    }
}
