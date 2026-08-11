using Azure;
using ClothHub.Config;
using ClothHub.Models;
using ClothHub.Models.Auth;
using ClothHub.Service.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController
          : ControllerBase
    {
        private readonly UserManager<AppUserModel>
            _userManager;

        private readonly IJwtTokenService
            _jwtTokenService;

        private readonly JwtOptions
            _jwtOptions;

        public AuthController(
            UserManager<AppUserModel> userManager,
            IJwtTokenService jwtTokenService,
            IOptions<JwtOptions> jwtOptions
        )
        {
            _userManager =
                userManager;

            _jwtTokenService =
                jwtTokenService;

            _jwtOptions =
                jwtOptions.Value;
        }

        // ==================================================
        // LOGIN BẰNG EMAIL
        // POST: /api/auth/login
        // ==================================================

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<
            ActionResult<LoginResultModel>
        > Login(
            [FromBody] LoginModel model
        )
        {
            var email =
                model.Email.Trim();

            /*
             * Chỉ tìm tài khoản bằng Email.
             *
             * FindByEmailAsync sẽ chuẩn hóa Email
             * theo cấu hình của Identity.
             */
            var user =
                await _userManager
                    .FindByEmailAsync(email);

            /*
             * Không thông báo riêng Email hay Password sai.
             * Điều này tránh làm lộ Email nào đã tồn tại.
             */
            if (user is null)
            {
                return Unauthorized(
                    new
                    {
                        message =
                            "Email hoặc mật khẩu không chính xác."
                    }
                );
            }

            /*
             * Không cho đăng nhập nếu tài khoản
             * đang bị Identity khóa.
             */
            if (
                await _userManager
                    .IsLockedOutAsync(user)
            )
            {
                return StatusCode(
                    StatusCodes.Status423Locked,
                    new
                    {
                        message =
                            "Tài khoản đang tạm thời bị khóa."
                    }
                );
            }

            /*
             * Identity tự so sánh Password người dùng nhập
             * với PasswordHash trong AspNetUsers.
             */
            var passwordIsValid =
                await _userManager
                    .CheckPasswordAsync(
                        user,
                        model.Password
                    );

            if (!passwordIsValid)
            {
                /*
                 * Tăng số lần nhập sai.
                 * Đủ số lần cho phép thì Identity khóa tài khoản.
                 */
                await _userManager
                    .AccessFailedAsync(user);

                return Unauthorized(
                    new
                    {
                        message =
                            "Email hoặc mật khẩu không chính xác."
                    }
                );
            }

            /*
             * Đăng nhập đúng thì đặt số lần sai về 0.
             */
            await _userManager
                .ResetAccessFailedCountAsync(user);

            /*
             * Lấy toàn bộ Role của tài khoản.
             */
            var roles =
                await _userManager
                    .GetRolesAsync(user);

            /*
             * Chọn thời hạn JWT.
             */
            var expiresAtUtc =
                model.RememberMe

                    ? DateTime.UtcNow.AddDays(
                        _jwtOptions.RememberMeDays
                    )

                    : DateTime.UtcNow.AddMinutes(
                        _jwtOptions.AccessTokenMinutes
                    );

            /*
             * Tạo JWT chứa Claim và ký bằng Secret Key.
             */
            var jwt =
                _jwtTokenService
                    .CreateAccessToken(
                        user,
                        roles,
                        expiresAtUtc
                    );

            /*
             * Server trả Set-Cookie về Browser.
             *
             * JavaScript không thể đọc JWT
             * vì Cookie có HttpOnly = true.
             */
            Response.Cookies.Append(
                _jwtOptions.CookieName,
                jwt,
                CreateCookieOptions(
                    model.RememberMe,
                    expiresAtUtc
                )
            );

            /*
             * Không trả JWT trong Response Body.
             */
            return Ok(
                new LoginResultModel
                {
                    Message =
                        "Đăng nhập thành công.",

                    ExpiresAtUtc =
                        expiresAtUtc,

                    User =
                        CreateAuthenticatedUser(
                            user,
                            roles
                        )
                }
            );
        }

        // ==================================================
        // PROFILE
        // GET: /api/auth/profile
        //
        // Dùng để:
        // - Kiểm tra đã đăng nhập hay chưa.
        // - Lấy dữ liệu Claim trong JWT.
        // ==================================================

        [Authorize]
        [HttpGet("profile")]
        public ActionResult<
            AuthenticatedUserModel
        > Profile()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    userId
                )
            )
            {
                return Unauthorized(
                    new
                    {
                        message =
                            "Phiên đăng nhập không hợp lệ."
                    }
                );
            }

            var roles =
                User.FindAll(
                        ClaimTypes.Role
                    )
                    .Select(
                        claim =>
                            claim.Value
                    )
                    .Distinct()
                    .ToArray();

            return Ok(
                new AuthenticatedUserModel
                {
                    Id =
                        userId,

                    UserName =
                        User.FindFirstValue(
                            "userName"
                        ) ?? string.Empty,

                    FullName =
                        User.FindFirstValue(
                            ClaimTypes.Name
                        ) ?? string.Empty,

                    Email =
                        User.FindFirstValue(
                            ClaimTypes.Email
                        ) ?? string.Empty,

                    Roles =
                        roles
                }
            );
        }

        // ==================================================
        // API CHỈ DÀNH CHO ADMIN
        // GET: /api/auth/admin-only
        // ==================================================

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(
                new
                {
                    message =
                        "Bạn đang đăng nhập bằng quyền Admin."
                }
            );
        }

        // ==================================================
        // LOGOUT
        // POST: /api/auth/logout
        // ==================================================

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            /*
             * Xóa Cookie JWT.
             *
             * Path, SameSite và Secure nên khớp
             * với Cookie lúc được tạo.
             */
            Response.Cookies.Delete(
                _jwtOptions.CookieName,
                new CookieOptions
                {
                    HttpOnly = true,

                    Secure = true,

                    SameSite =
                        SameSiteMode.None,

                    Path = "/"
                }
            );

            return Ok(
                new
                {
                    message =
                        "Đăng xuất thành công."
                }
            );
        }

        // ==================================================
        // COOKIE OPTIONS
        // ==================================================

        private static CookieOptions
            CreateCookieOptions(
                bool rememberMe,
                DateTime expiresAtUtc
            )
        {
            var cookieOptions =
                new CookieOptions
                {
                    /*
                     * JavaScript không thể đọc Cookie.
                     */
                    HttpOnly = true,

                    /*
                     * Cookie chỉ được truyền qua HTTPS.
                     */
                    Secure = true,

                    /*
                     * Angular và Backend khác origin.
                     */
                    SameSite =
                        SameSiteMode.None,

                    /*
                     * Cookie được gửi cho toàn bộ API.
                     */
                    Path = "/",

                    IsEssential = true
                };

            /*
             * Không chọn RememberMe:
             * Cookie là Session Cookie.
             *
             * Chọn RememberMe:
             * Cookie vẫn tồn tại sau khi đóng Browser.
             */
            if (rememberMe)
            {
                cookieOptions.Expires =
                    new DateTimeOffset(
                        expiresAtUtc
                    );

                cookieOptions.MaxAge =
                    expiresAtUtc -
                    DateTime.UtcNow;
            }

            return cookieOptions;
        }

        // ==================================================
        // TẠO MODEL TRẢ VỀ ANGULAR
        // ==================================================

        private static AuthenticatedUserModel
            CreateAuthenticatedUser(
                AppUserModel user,
                IEnumerable<string> roles
            )
        {
            return new AuthenticatedUserModel
            {
                Id =
                    user.Id,

                UserName =
                    user.UserName ??
                    string.Empty,

                FullName =
                    user.FullName,

                Email =
                    user.Email ??
                    string.Empty,

                Roles =
                    roles
                        .Distinct()
                        .ToArray()
            };
        }









        // ==================================================
        // REGISTER TÀI KHOẢN KHÁCH HÀNG
        // POST: /api/auth/register
        // ==================================================

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<
            ActionResult<RegisterResultModel>
        > Register(
            [FromBody] RegisterModel model
        )
        {
            var email =
                model.Email.Trim();

            /*
             * Kiểm tra Email đã tồn tại hay chưa.
             */
            var existingUser =
                await _userManager
                    .FindByEmailAsync(email);

            if (existingUser is not null)
            {
                return Conflict(
                    new
                    {
                        message =
                            "Email này đã được sử dụng."
                    }
                );
            }

            /*
             * Người dùng đăng nhập bằng Email.
             *
             * UserName vẫn là trường bắt buộc của Identity,
             * nên dùng chính Email làm UserName nội bộ.
             */
            var user =
                new AppUserModel
                {
                    UserName =
                        email,

                    Email =
                        email,

                    PhoneNumber =
                        string.IsNullOrWhiteSpace(
                            model.PhoneNumber
                        )
                            ? null
                            : model.PhoneNumber.Trim(),

                    FullName =
                        model.FullName.Trim(),

                    /*
                     * Chưa tích hợp xác nhận Email nên
                     * tạm thời cho phép tài khoản hoạt động.
                     */
                    EmailConfirmed =
                        true,

                    LockoutEnabled =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            /*
             * Identity tự băm Password và lưu PasswordHash.
             * Không bao giờ tự lưu Password dạng chữ thường.
             */
            var createResult =
                await _userManager
                    .CreateAsync(
                        user,
                        model.Password
                    );

            if (!createResult.Succeeded)
            {
                var errors =
                    createResult.Errors
                        .Select(
                            error =>
                                error.Description
                        )
                        .ToArray();

                return BadRequest(
                    new
                    {
                        message =
                            "Đăng ký tài khoản thất bại.",

                        errors
                    }
                );
            }

            /*
             * KHÔNG gọi AddToRoleAsync().
             *
             * Đây là tài khoản khách hàng thông thường,
             * nên AspNetUserRoles không có bản ghi.
             *
             * Khi đăng nhập:
             * roles = []
             * → Angular chuyển đến /user.
             */
            return StatusCode(
                StatusCodes.Status201Created,
                new RegisterResultModel
                {
                    Message =
                        "Đăng ký tài khoản thành công.",

                    User =
                        new AuthenticatedUserModel
                        {
                            Id =
                                user.Id,

                            UserName =
                                user.UserName ??
                                string.Empty,

                            FullName =
                                user.FullName,

                            Email =
                                user.Email ??
                                string.Empty,

                            Roles =
                                Array.Empty<string>()
                        }
                }
            );
        }















































    }
}
