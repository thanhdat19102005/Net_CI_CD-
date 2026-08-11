using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class EmployeeInfo
    {
        /// <summary>
        /// Mã nhân viên do người dùng tự nhập.
        /// Ví dụ: NV001, NV002.
        /// </summary>
        [Key]
        [StringLength(20)]
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Khóa ngoại nối với AspNetUsers.Id.
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn ảnh đại diện được lưu trong database.
        /// </summary>
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// File ảnh nhận từ frontend.
        /// Không tạo cột trong database.
        /// </summary>
        [NotMapped]
        public IFormFile? FileAttachments { get; set; }

        /// <summary>
        /// Giới tính: Nam, Nữ hoặc Khác.
        /// </summary>
        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// 0 = Đã nghỉ việc.
        /// 1 = Còn làm việc.
        /// </summary>
        [Range(0, 1)]
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Tài khoản đăng nhập của nhân viên.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public AppUserModel? User { get; set; }
    }
}
