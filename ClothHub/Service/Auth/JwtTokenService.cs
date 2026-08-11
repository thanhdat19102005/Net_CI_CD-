using ClothHub.Config;
using ClothHub.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClothHub.Service.Auth
{
    public sealed class JwtTokenService
        : IJwtTokenService
    {
        private readonly JwtOptions
            _jwtOptions;

        public JwtTokenService(
            IOptions<JwtOptions> jwtOptions
        )
        {
            _jwtOptions =
                jwtOptions.Value;
        }

        public string CreateAccessToken(
            AppUserModel user,
            IEnumerable<string> roles,
            DateTime expiresAtUtc
        )
        {
            /*
             * Các trường muốn lưu trong JWT
             * đều được đưa vào Claim.
             */
            var claims =
                new List<Claim>
                {
                    /*
                     * ID riêng của mỗi JWT.
                     */
                    new(
                        JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid().ToString()
                    ),

                    /*
                     * Id người dùng trong AspNetUsers.
                     */
                    new(
                        ClaimTypes.NameIdentifier,
                        user.Id
                    ),

                    /*
                     * User.Identity.Name sẽ đọc Claim này.
                     */
                    new(
                        ClaimTypes.Name,
                        string.IsNullOrWhiteSpace(
                            user.FullName
                        )
                            ? user.Email ??
                              string.Empty
                            : user.FullName
                    ),

                    /*
                     * Email đăng nhập.
                     */
                    new(
                        ClaimTypes.Email,
                        user.Email ??
                        string.Empty
                    ),

                    /*
                     * UserName nội bộ của Identity.
                     */
                    new(
                        "userName",
                        user.UserName ??
                        string.Empty
                    )
                };

            /*
             * Một người dùng có thể có nhiều Role.
             */
            foreach (
                var role in roles.Distinct()
            )
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role
                    )
                );
            }

            /*
             * Secret Key dùng để ký JWT.
             */
            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _jwtOptions.Key
                    )
                );

            var signingCredentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256
                );

            var token =
                new JwtSecurityToken(
                    issuer:
                        _jwtOptions.Issuer,

                    audience:
                        _jwtOptions.Audience,

                    claims:
                        claims,

                    notBefore:
                        DateTime.UtcNow,

                    expires:
                        expiresAtUtc,

                    signingCredentials:
                        signingCredentials
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
