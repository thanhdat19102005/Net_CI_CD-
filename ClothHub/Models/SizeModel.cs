using System.ComponentModel.DataAnnotations;

namespace ClothHub.Models
{
    public class SizeModel
    {
        /// <summary>
        /// Mã kích thước do người dùng tự nhập.
        /// Ví dụ: SIZE_S, SIZE_M, SIZE_L.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

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
