using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClothHub.Models
{
    public class AppUserModel : IdentityUser
    {
        /*
         * UserName đã được kế thừa từ IdentityUser.
         * Không khai báo lại để tránh trùng thuộc tính.
         */

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// 0 = Ngừng hoạt động hoặc bị khóa.
        /// 1 = Đang hoạt động.
        /// </summary>
        [Range(0, 1)]
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Hồ sơ nhân viên liên kết với tài khoản.
        /// Một tài khoản có tối đa một hồ sơ nhân viên.
        /// </summary>
        public EmployeeInfo? EmployeeInfo { get; set; }



        public string ? Address { get; set; }





    }
}
