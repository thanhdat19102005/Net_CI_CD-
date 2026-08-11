using Azure;
using ClothHub.Config;
using ClothHub.Models;
using ClothHub.Service.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ClothHub.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtOptions _jwtOptions;

        public SettingsController(
            UserManager<AppUserModel> userManager,
            IJwtTokenService jwtTokenService,
            IOptions<JwtOptions> jwtOptions
        )
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _jwtOptions = jwtOptions.Value;
        }

        // ==================================================
        // GET: api/settings/me
        //
        // JWT nằm trong HttpOnly Cookie.
        // JwtBearer đã đọc Cookie và xác thực JWT trước khi
        // Controller được thực thi.
        //
        // Ta lấy UserId từ JWT Claim -> FindByIdAsync().
        //
        // UserId ổn định hơn Email:
        //
        // UserId = abc123
        //      ↓
        // đổi Email
        //      ↓
        // UserId vẫn abc123
        //      ↓
        // JWT vẫn tìm đúng User.
        // ==================================================

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userResult =
                await GetCurrentUserFromJwtUserIdAsync();

            if (!userResult.Success)
            {
                return userResult.ErrorResult!;
            }

            var user =
                userResult.User!;

            var roles =
                await _userManager
                    .GetRolesAsync(user);

            return Ok(new
            {
                id =
                    user.Id,

                email =
                    user.Email ?? string.Empty,

                userName =
                    user.UserName ?? string.Empty,

                phoneNumber =
                    user.PhoneNumber ?? string.Empty,

                roles,

                createdAt =
                    user.CreatedAt
            });
        }

        // ==================================================
        // PUT: api/settings/profile
        //
        // Cập nhật:
        // - Email
        // - Tên đăng nhập
        // - Số điện thoại
        //
        // User được tìm bằng UserId trong JWT.
        //
        // Sau khi đổi Email/UserName vẫn tạo lại JWT để
        // các Claim Email/UserName trong token được cập nhật.
        //
        // Tuy nhiên việc tìm User KHÔNG còn phụ thuộc Email.
        // ==================================================

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(
                    ModelState
                );
            }

            var userResult =
                await GetCurrentUserFromJwtUserIdAsync();

            if (!userResult.Success)
            {
                return userResult.ErrorResult!;
            }

            var user =
                userResult.User!;

            var newEmail =
                request.Email.Trim();

            var newUserName =
                request.UserName.Trim();

            var newPhoneNumber =
                request.PhoneNumber.Trim();

            // ==============================================
            // KIỂM TRA EMAIL ĐÃ TỒN TẠI
            // ==============================================

            var userWithSameEmail =
                await _userManager
                    .FindByEmailAsync(
                        newEmail
                    );

            if (
                userWithSameEmail != null &&
                userWithSameEmail.Id != user.Id
            )
            {
                return Conflict(new
                {
                    message =
                        "Email này đã được sử dụng bởi tài khoản khác."
                });
            }

            // ==============================================
            // KIỂM TRA TÊN ĐĂNG NHẬP ĐÃ TỒN TẠI
            // ==============================================

            var userWithSameUserName =
                await _userManager
                    .FindByNameAsync(
                        newUserName
                    );

            if (
                userWithSameUserName != null &&
                userWithSameUserName.Id != user.Id
            )
            {
                return Conflict(new
                {
                    message =
                        "Tên đăng nhập này đã được sử dụng bởi tài khoản khác."
                });
            }

            // ==============================================
            // CẬP NHẬT EMAIL
            // ==============================================

            if (
                !string.Equals(
                    user.Email,
                    newEmail,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var emailResult =
                    await _userManager
                        .SetEmailAsync(
                            user,
                            newEmail
                        );

                if (!emailResult.Succeeded)
                {
                    return BadRequest(
                        CreateIdentityErrorResponse(
                            "Không thể cập nhật email.",
                            emailResult
                        )
                    );
                }
            }

            // ==============================================
            // CẬP NHẬT TÊN ĐĂNG NHẬP
            // ==============================================

            if (
                !string.Equals(
                    user.UserName,
                    newUserName,
                    StringComparison.Ordinal
                )
            )
            {
                var userNameResult =
                    await _userManager
                        .SetUserNameAsync(
                            user,
                            newUserName
                        );

                if (!userNameResult.Succeeded)
                {
                    return BadRequest(
                        CreateIdentityErrorResponse(
                            "Không thể cập nhật tên đăng nhập.",
                            userNameResult
                        )
                    );
                }
            }

            // ==============================================
            // CẬP NHẬT SỐ ĐIỆN THOẠI
            // ==============================================

            if (
                !string.Equals(
                    user.PhoneNumber,
                    newPhoneNumber,
                    StringComparison.Ordinal
                )
            )
            {
                var phoneResult =
                    await _userManager
                        .SetPhoneNumberAsync(
                            user,
                            newPhoneNumber
                        );

                if (!phoneResult.Succeeded)
                {
                    return BadRequest(
                        CreateIdentityErrorResponse(
                            "Không thể cập nhật số điện thoại.",
                            phoneResult
                        )
                    );
                }
            }

            // ==============================================
            // LẤY LẠI USER SAU KHI CẬP NHẬT
            // ==============================================

            var updatedUser =
                await _userManager
                    .FindByIdAsync(
                        user.Id
                    );

            if (updatedUser == null)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy tài khoản sau khi cập nhật."
                });
            }

            // ==============================================
            // TẠO LẠI JWT COOKIE
            //
            // Không còn cần refresh token để "sửa Email dùng
            // tìm User", vì bây giờ ta tìm bằng UserId.
            //
            // Nhưng vẫn nên refresh để Email/UserName Claim
            // trong JWT luôn đồng bộ với dữ liệu mới.
            // ==============================================

            await RefreshJwtCookieAsync(
                updatedUser
            );

            var roles =
                await _userManager
                    .GetRolesAsync(
                        updatedUser
                    );

            return Ok(new
            {
                message =
                    "Cập nhật thông tin tài khoản thành công.",

                user = new
                {
                    id =
                        updatedUser.Id,

                    email =
                        updatedUser.Email ?? string.Empty,

                    userName =
                        updatedUser.UserName ?? string.Empty,

                    phoneNumber =
                        updatedUser.PhoneNumber ?? string.Empty,

                    roles,

                    createdAt =
                        updatedUser.CreatedAt
                }
            });
        }

        // ==================================================
        // PUT: api/settings/password
        //
        // Người dùng phải nhập:
        // - Mật khẩu hiện tại
        // - Mật khẩu mới
        // - Xác nhận mật khẩu mới
        //
        // User được tìm bằng UserId trong JWT.
        // ==================================================

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(
                    ModelState
                );
            }

            if (
                request.NewPassword !=
                request.ConfirmNewPassword
            )
            {
                return BadRequest(new
                {
                    message =
                        "Xác nhận mật khẩu mới không khớp."
                });
            }

            var userResult =
                await GetCurrentUserFromJwtUserIdAsync();

            if (!userResult.Success)
            {
                return userResult.ErrorResult!;
            }

            var user =
                userResult.User!;

            var result =
                await _userManager
                    .ChangePasswordAsync(
                        user,
                        request.CurrentPassword,
                        request.NewPassword
                    );

            if (!result.Succeeded)
            {
                var currentPasswordWrong =
                    result.Errors.Any(
                        error =>
                            error.Code ==
                            "PasswordMismatch"
                    );

                if (currentPasswordWrong)
                {
                    return BadRequest(new
                    {
                        message =
                            "Mật khẩu hiện tại không chính xác."
                    });
                }

                return BadRequest(
                    CreateIdentityErrorResponse(
                        "Không thể thay đổi mật khẩu.",
                        result
                    )
                );
            }

            return Ok(new
            {
                message =
                    "Đổi mật khẩu thành công."
            });
        }

        // ==================================================
        // HELPER:
        // LẤY USERID TỪ JWT CLAIM -> TÌM USER
        // ==================================================

        private async Task<CurrentUserResult>
            GetCurrentUserFromJwtUserIdAsync()
        {
            /*
             * ClaimTypes.NameIdentifier là nơi thông dụng
             * để lưu UserId trong JWT của ASP.NET Core.
             *
             * Fallback "sub" và "userId" giúp Controller
             * tương thích nếu JwtTokenService dùng tên Claim
             * khác.
             */

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
                userId =
                    User.FindFirstValue(
                        "sub"
                    );
            }

            if (
                string.IsNullOrWhiteSpace(
                    userId
                )
            )
            {
                userId =
                    User.FindFirstValue(
                        "userId"
                    );
            }

            if (
                string.IsNullOrWhiteSpace(
                    userId
                )
            )
            {
                return new CurrentUserResult
                {
                    Success = false,

                    ErrorResult =
                        Unauthorized(new
                        {
                            message =
                                "JWT không chứa UserId người dùng."
                        })
                };
            }

            var user =
                await _userManager
                    .FindByIdAsync(
                        userId
                    );

            if (user == null)
            {
                return new CurrentUserResult
                {
                    Success = false,

                    ErrorResult =
                        NotFound(new
                        {
                            message =
                                "Không tìm thấy tài khoản tương ứng với UserId trong JWT."
                        })
                };
            }

            return new CurrentUserResult
            {
                Success = true,
                User = user
            };
        }

        // ==================================================
        // HELPER:
        // TẠO LẠI JWT + GHI ĐÈ COOKIE
        // ==================================================

        private async Task RefreshJwtCookieAsync(
            AppUserModel user
        )
        {
            var roles =
                await _userManager
                    .GetRolesAsync(
                        user
                    );

            var expiresAtUtc =
                DateTime.UtcNow
                    .AddMinutes(
                        _jwtOptions
                            .AccessTokenMinutes
                    );

            var accessToken =
                _jwtTokenService
                    .CreateAccessToken(
                        user,
                        roles,
                        expiresAtUtc
                    );

            Response.Cookies.Append(
                _jwtOptions.CookieName,
                accessToken,
                new CookieOptions
                {
                    HttpOnly = true,

                    Secure = true,

                    SameSite =
                        SameSiteMode.None,

                    Path = "/",

                    Expires =
                        expiresAtUtc
                }
            );
        }

        // ==================================================
        // HELPER:
        // FORMAT LỖI IDENTITY
        // ==================================================

        private static object
            CreateIdentityErrorResponse(
                string message,
                IdentityResult result
            )
        {
            return new
            {
                message,

                errors =
                    result.Errors
                        .Select(
                            error => new
                            {
                                code =
                                    error.Code,

                                description =
                                    error.Description
                            }
                        )
                        .ToArray()
            };
        }

        // ==================================================
        // INTERNAL RESULT
        // ==================================================

        private sealed class CurrentUserResult
        {
            public bool Success
            {
                get;
                init;
            }

            public AppUserModel? User
            {
                get;
                init;
            }

            public IActionResult? ErrorResult
            {
                get;
                init;
            }
        }
    }

    // ==================================================
    // DTO: CẬP NHẬT THÔNG TIN CÁ NHÂN
    // ==================================================

    public class UpdateProfileRequest
    {
        [Required(
            ErrorMessage =
                "Email là bắt buộc."
        )]
        [EmailAddress(
            ErrorMessage =
                "Email không đúng định dạng."
        )]
        [StringLength(
            256,
            ErrorMessage =
                "Email tối đa 256 ký tự."
        )]
        public string Email
        {
            get;
            set;
        } = string.Empty;

        [Required(
            ErrorMessage =
                "Tên đăng nhập là bắt buộc."
        )]
        [StringLength(
            100,
            MinimumLength = 3,
            ErrorMessage =
                "Tên đăng nhập phải từ 3 đến 100 ký tự."
        )]
        public string UserName
        {
            get;
            set;
        } = string.Empty;

        [Required(
            ErrorMessage =
                "Số điện thoại là bắt buộc."
        )]
        [Phone(
            ErrorMessage =
                "Số điện thoại không hợp lệ."
        )]
        [StringLength(
            20,
            ErrorMessage =
                "Số điện thoại tối đa 20 ký tự."
        )]
        public string PhoneNumber
        {
            get;
            set;
        } = string.Empty;
    }

    // ==================================================
    // DTO: ĐỔI MẬT KHẨU
    // ==================================================

    public class ChangePasswordRequest
    {
        [Required(
            ErrorMessage =
                "Mật khẩu hiện tại là bắt buộc."
        )]
        public string CurrentPassword
        {
            get;
            set;
        } = string.Empty;

        [Required(
            ErrorMessage =
                "Mật khẩu mới là bắt buộc."
        )]
        [MinLength(
            8,
            ErrorMessage =
                "Mật khẩu mới phải có ít nhất 8 ký tự."
        )]
        public string NewPassword
        {
            get;
            set;
        } = string.Empty;

        [Required(
            ErrorMessage =
                "Xác nhận mật khẩu mới là bắt buộc."
        )]
        public string ConfirmNewPassword
        {
            get;
            set;
        } = string.Empty;
    }
}
