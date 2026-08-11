using System.ComponentModel.DataAnnotations;

namespace ClothHub.Models.Auth
{
    /// <summary>
    /// Dữ liệu Angular gửi lên khi đăng nhập.
    ///
    /// Đây không phải Entity database.
    /// Không khai báo DbSet cho class này.
    /// </summary>
    public sealed class LoginModel
    {
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

        [Required(
            ErrorMessage =
                "Vui lòng nhập mật khẩu."
        )]
        [MinLength(
            6,
            ErrorMessage =
                "Mật khẩu phải có ít nhất 6 ký tự."
        )]
        [MaxLength(100)]
        public string Password { get; set; } =
            string.Empty;

        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Thông tin tài khoản trả về cho Angular.
    ///
    /// JWT không nằm trong model này vì JWT
    /// được lưu trong HttpOnly Cookie.
    /// </summary>
    public sealed class AuthenticatedUserModel
    {
        public string Id { get; set; } =
            string.Empty;

        public string UserName { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public IReadOnlyCollection<string> Roles
        {
            get;
            set;
        } = Array.Empty<string>();
    }

    /// <summary>
    /// Kết quả trả về sau khi đăng nhập thành công.
    /// </summary>
    public sealed class LoginResultModel
    {
        public string Message { get; set; } =
            string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public AuthenticatedUserModel User
        {
            get;
            set;
        } = new();
    }
}
