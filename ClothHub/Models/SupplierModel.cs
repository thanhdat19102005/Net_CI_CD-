using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class SupplierModel
    {
        /// <summary>
        /// Mã nhà cung cấp do người dùng tự nhập.
        /// Ví dụ: NCC001, NCC002.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }

        [StringLength(150)]
        public string? ContactPerson { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Đường dẫn ảnh hoặc logo nhà cung cấp.
        /// </summary>
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// File ảnh nhận từ frontend.
        /// Không tạo cột trong database.
        /// </summary>
        [NotMapped]
        public IFormFile? FileAttachments { get; set; }

        /// <summary>
        /// 0 = Ngừng hợp tác.
        /// 1 = Đang hợp tác.
        /// </summary>
        [Range(0, 1)]
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ProductModel> Products { get; set; }
            = new List<ProductModel>();
    }
}
