using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class BrandModel
    {
        /// <summary>
        /// Mã thương hiệu do người dùng tự nhập.
        /// Ví dụ: NIKE, ADIDAS, LOCAL.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        /// <summary>
        /// Đường dẫn logo được lưu trong database.
        /// </summary>
        [StringLength(500)]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// File logo nhận từ frontend.
        /// Không tạo cột trong database.
        /// </summary>
        [NotMapped]
        public IFormFile? FileAttachments { get; set; }

        /// <summary>
        /// 0 = Ngừng hoạt động.
        /// 1 = Đang hoạt động.
        /// </summary>
        [Range(0, 1)]
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ProductModel> Products { get; set; }
            = new List<ProductModel>();
    }
}
