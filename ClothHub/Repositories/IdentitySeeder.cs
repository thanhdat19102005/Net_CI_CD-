using ClothHub.Models;
using Microsoft.AspNetCore.Identity;

namespace ClothHub.Repositories
{
    /// <summary>
    /// Tạo Role Admin và tài khoản Admin mẫu
    /// trong môi trường Development.
    /// </summary>
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration
        )
        {
            var userManager =
                serviceProvider
                    .GetRequiredService<
                        UserManager<AppUserModel>
                    >();

            var roleManager =
                serviceProvider
                    .GetRequiredService<
                        RoleManager<IdentityRole>
                    >();

            var role =
                configuration[
                    "SeedAdmin:Role"
                ] ?? "Admin";

            /*
             * Tạo Role nếu chưa tồn tại.
             */
            if (
                !await roleManager
                    .RoleExistsAsync(role)
            )
            {
                var createRoleResult =
                    await roleManager
                        .CreateAsync(
                            new IdentityRole(role)
                        );

                if (!createRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            "; ",
                            createRoleResult.Errors
                                .Select(
                                    error =>
                                        error.Description
                                )
                        )
                    );
                }
            }

            var email =
                configuration[
                    "SeedAdmin:Email"
                ] ?? "admin@clothhub.com";

            var fullName =
                configuration[
                    "SeedAdmin:FullName"
                ] ?? "Quản trị viên ClothHub";

            var password =
                configuration[
                    "SeedAdmin:Password"
                ] ?? "Admin@123";

            /*
             * Đăng nhập bằng Email nên tìm Admin bằng Email.
             */
            var user =
                await userManager
                    .FindByEmailAsync(email);

            if (user is null)
            {
                user =
                    new AppUserModel
                    {
                        /*
                         * Người dùng đăng nhập bằng Email.
                         *
                         * UserName vẫn phải tồn tại vì
                         * IdentityUser có trường UserName.
                         * Ta dùng chính Email làm UserName nội bộ.
                         */
                        UserName =
                            email,

                        Email =
                            email,

                        EmailConfirmed =
                            true,

                        FullName =
                            fullName,

                        LockoutEnabled =
                            true,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                var createUserResult =
                    await userManager
                        .CreateAsync(
                            user,
                            password
                        );

                if (!createUserResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            "; ",
                            createUserResult.Errors
                                .Select(
                                    error =>
                                        error.Description
                                )
                        )
                    );
                }
            }

            /*
             * Gán Role Admin nếu tài khoản chưa có.
             */
            if (
                !await userManager
                    .IsInRoleAsync(
                        user,
                        role
                    )
            )
            {
                var addRoleResult =
                    await userManager
                        .AddToRoleAsync(
                            user,
                            role
                        );

                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            "; ",
                            addRoleResult.Errors
                                .Select(
                                    error =>
                                        error.Description
                                )
                        )
                    );
                }
            }
        }
    }

}
