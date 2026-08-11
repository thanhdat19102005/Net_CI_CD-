using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class ProductModel
    {
        /// <summary>
        /// Mã sản phẩm do người dùng tự nhập.
        /// Ví dụ: SP001, SP002.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // ==========================
        // Khóa ngoại
        // ==========================

        [Required]
        [StringLength(20)]
        public string CategoryId { get; set; } = string.Empty;

        [StringLength(20)]
        public string? BrandId { get; set; }

        [Required]
        [StringLength(20)]
        public string SizeId { get; set; } = string.Empty;

        

        // ==========================
        // Thông tin sản phẩm
        // ==========================

        [StringLength(100)]
        public string? Material { get; set; }

        /// <summary>
        /// Nam, Nữ hoặc Unisex.
        /// </summary>
        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(100)]
        public string? Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal ImportPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        /// <summary>
        /// Số lượng sản phẩm hiện có.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>
        /// Mức số lượng tối thiểu để cảnh báo sắp hết hàng.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int MinimumQuantity { get; set; }

        /// <summary>
        /// Đường dẫn ảnh được lưu trong database.
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
        /// 0 = Ngừng hoạt động.
        /// 1 = Đang hoạt động.
        /// </summary>
        [Range(0, 1)]
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ==========================
        // Navigation properties
        // ==========================

        [ForeignKey(nameof(CategoryId))]
        public CategoryModel? Category { get; set; }

        [ForeignKey(nameof(BrandId))]
        public BrandModel? Brand { get; set; }

        [ForeignKey(nameof(SizeId))]
        public SizeModel? Size { get; set; }

      
    }
}

