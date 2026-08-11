namespace ClothHub.Config
{
    /// <summary>
    /// Cấu hình lấy từ section "Jwt"
    /// trong appsettings.json.
    /// </summary>
    public sealed class JwtOptions
    {
        public const string SectionName =
            "Jwt";

        public string Key { get; set; } =
            string.Empty;

        public string Issuer { get; set; } =
            string.Empty;

        public string Audience { get; set; } =
            string.Empty;

        public string CookieName { get; set; } =
            "accessToken";

        /// <summary>
        /// Thời hạn JWT khi không chọn ghi nhớ đăng nhập.
        /// </summary>
        public int AccessTokenMinutes { get; set; } =
            120;

        /// <summary>
        /// Thời hạn JWT khi chọn ghi nhớ đăng nhập.
        /// </summary>
        public int RememberMeDays { get; set; } =
            7;
    }
}
