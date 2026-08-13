using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class CategoryModel
    {
        /// <summary>
        /// Mã loại sản phẩm do người dùng tự nhập.
        /// Ví dụ: AOTHUN, AOSOMI, QUANJEAN.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
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




