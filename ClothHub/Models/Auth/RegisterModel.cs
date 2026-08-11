using System.ComponentModel.DataAnnotations;

namespace ClothHub.Models.Auth
{
    /// <summary>
    /// Dữ liệu Angular gửi lên khi đăng ký tài khoản.
    ///
    /// Đây là model nhận dữ liệu API,
    /// không khai báo DbSet nên EF Core không tạo bảng.
    /// </summary>
    public sealed class RegisterModel
    {
        [Required(
            ErrorMessage =
                "Vui lòng nhập họ và tên."
        )]
        [MinLength(
            2,
            ErrorMessage =
                "Họ và tên phải có ít nhất 2 ký tự."
        )]
        [MaxLength(
            100,
            ErrorMessage =
                "Họ và tên không được vượt quá 100 ký tự."
        )]
        public string FullName { get; set; } =
            string.Empty;

        [Required(
            ErrorMessage =
                "Vui lòng nhập email."
        )]
        [EmailAddress(
            ErrorMessage =
                "Email không đúng định dạng."
        )]
        [MaxLength(256)]
        public string Email { get; set; } =
            string.Empty;

        /*
         * Số điện thoại không bắt buộc.
         *
         * Hỗ trợ:
         * 0912345678
         * +84912345678
         */
        [RegularExpression(
            @"^(0|\+84)[0-9]{9}$",
            ErrorMessage =
                "Số điện thoại không đúng định dạng."
        )]
        public string? PhoneNumber { get; set; }

        [Required(
            ErrorMessage =
                "Vui lòng nhập mật khẩu."
        )]
        [MinLength(
            8,
            ErrorMessage =
                "Mật khẩu phải có ít nhất 8 ký tự."
        )]
        [MaxLength(100)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
            ErrorMessage =
                "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt."
        )]
        public string Password { get; set; } =
            string.Empty;
    }

    /// <summary>
    /// Kết quả trả về sau khi đăng ký thành công.
    /// </summary>
    public sealed class RegisterResultModel
    {
        public string Message { get; set; } =
            string.Empty;

        public AuthenticatedUserModel User
        {
            get;
            set;
        } = new();
    }
}
