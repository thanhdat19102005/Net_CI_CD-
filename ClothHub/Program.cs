using ClothHub.Config;
using ClothHub.Models;
using ClothHub.Repositories;

using ClothHub.Service.Auth;


using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Security.Claims;
using System.Text;

var builder =
    WebApplication.CreateBuilder(args);

const string AngularCorsPolicy =
    "AngularClient";

// ==================================================
// 1. CONTROLLERS
// ==================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// ==================================================
// 2. DATABASE
// ==================================================

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnectionString"
                )
        );
    }
);

// ==================================================
// 3. ASP.NET CORE IDENTITY
// ==================================================

builder.Services
    .AddIdentity<
        AppUserModel,
        IdentityRole
    >(
        options =>
        {
            /*
             * Quy tắc Password.
             */
            options.Password
                .RequireDigit = true;

            options.Password
                .RequiredLength = 6;

            options.Password
                .RequireLowercase = false;

            options.Password
                .RequireUppercase = false;

            options.Password
                .RequireNonAlphanumeric = false;

            /*
             * Không cho hai tài khoản dùng cùng Email.
             */
            options.User
                .RequireUniqueEmail = true;

            /*
             * Khóa tài khoản 10 phút sau 5 lần nhập sai.
             */
            options.Lockout
                .AllowedForNewUsers = true;

            options.Lockout
                .MaxFailedAccessAttempts = 5;

            options.Lockout
                .DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(10);
        }
    )
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

/*
 * Identity có Cookie Scheme riêng.
 *
 * Vì ClothHub là Web API, khi chưa đăng nhập
 * phải trả 401/403 thay vì redirect sang HTML.
 */
builder.Services.ConfigureApplicationCookie(
    options =>
    {
        options.Events.OnRedirectToLogin =
            context =>
            {
                context.Response.StatusCode =
                    StatusCodes
                        .Status401Unauthorized;

                return Task.CompletedTask;
            };

        options.Events.OnRedirectToAccessDenied =
            context =>
            {
                context.Response.StatusCode =
                    StatusCodes
                        .Status403Forbidden;

                return Task.CompletedTask;
            };
    }
);

// ==================================================
// 4. JWT OPTIONS
// ==================================================

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName
    )
);

var jwtOptions =
    builder.Configuration
        .GetSection(
            JwtOptions.SectionName
        )
        .Get<JwtOptions>()

    ?? throw new InvalidOperationException(
        "Không đọc được cấu hình Jwt."
    );

if (
    string.IsNullOrWhiteSpace(
        jwtOptions.Key
    )
)
{
    throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình."
    );
}

if (
    Encoding.UTF8.GetByteCount(
        jwtOptions.Key
    ) < 32
)
{
    throw new InvalidOperationException(
        "Jwt:Key phải có độ dài tối thiểu 32 byte."
    );
}

if (
    string.IsNullOrWhiteSpace(
        jwtOptions.Issuer
    ) ||
    string.IsNullOrWhiteSpace(
        jwtOptions.Audience
    )
)
{
    throw new InvalidOperationException(
        "Jwt:Issuer và Jwt:Audience chưa được cấu hình."
    );
}

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService
>();

// ==================================================
// 5. JWT AUTHENTICATION
// ==================================================

builder.Services
    .AddAuthentication(
        options =>
        {
            /*
             * Ép hệ thống dùng JwtBearer
             * làm Scheme xác thực mặc định.
             */
            options
                .DefaultAuthenticateScheme =
                    JwtBearerDefaults
                        .AuthenticationScheme;

            options
                .DefaultChallengeScheme =
                    JwtBearerDefaults
                        .AuthenticationScheme;

            options
                .DefaultForbidScheme =
                    JwtBearerDefaults
                        .AuthenticationScheme;
        }
    )
    .AddJwtBearer(
        options =>
        {
            options.SaveToken = false;

            options.RequireHttpsMetadata = true;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    /*
                     * Kiểm tra Issuer.
                     */
                    ValidateIssuer = true,

                    /*
                     * Kiểm tra Audience.
                     */
                    ValidateAudience = true,

                    /*
                     * Kiểm tra thời hạn JWT.
                     */
                    ValidateLifetime = true,

                    /*
                     * Kiểm tra chữ ký JWT.
                     */
                    ValidateIssuerSigningKey =
                        true,

                    ValidIssuer =
                        jwtOptions.Issuer,

                    ValidAudience =
                        jwtOptions.Audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtOptions.Key
                            )
                        ),

                    /*
                     * User.Identity.Name đọc ClaimTypes.Name.
                     */
                    NameClaimType =
                        ClaimTypes.Name,

                    /*
                     * [Authorize(Roles = "Admin")]
                     * đọc ClaimTypes.Role.
                     */
                    RoleClaimType =
                        ClaimTypes.Role,

                    /*
                     * JWT hết hạn là hết hiệu lực ngay.
                     */
                    ClockSkew =
                        TimeSpan.Zero
                };

            options.Events =
                new JwtBearerEvents
                {
                    OnMessageReceived =
                        context =>
                        {
                            /*
                             * Đọc JWT từ Cookie accessToken.
                             */
                            var cookieToken =
                                context.Request
                                    .Cookies[
                                        jwtOptions
                                            .CookieName
                                    ];

                            if (
                                !string
                                    .IsNullOrWhiteSpace(
                                        cookieToken
                                    )
                            )
                            {
                                context.Token =
                                    cookieToken;
                            }

                            return Task.CompletedTask;
                        }
                };
        }
    );

builder.Services.AddAuthorization();

// ==================================================
// 6. CORS CHO ANGULAR
// ==================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            AngularCorsPolicy,
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        );
    }
);

// ==================================================
// 7. SWAGGER
// ==================================================

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title =
                    "ClothHub API",

                Version =
                    "v1"
            }
        );

        /*
         * Bearer này chỉ để tiện kiểm thử Swagger.
         * Angular thực tế dùng JWT trong Cookie.
         */
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name =
                    "Authorization",

                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                In =
                    ParameterLocation.Header,

                Description =
                    "Nhập JWT nếu muốn kiểm thử bằng Authorization Header."
            }
        );

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference =
                            new OpenApiReference
                            {
                                Type =
                                    ReferenceType
                                        .SecurityScheme,

                                Id =
                                    "Bearer"
                            }
                    },

                    Array.Empty<string>()
                }
            }
        );
    }
);

// ==================================================
// 8. BUILD
// ==================================================

var app =
    builder.Build();

// ==================================================
// 9. DEVELOPMENT
// ==================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();

    /*
     * Tự cập nhật database và tạo tài khoản Admin mẫu.
     */
    using var scope =
        app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await dbContext
        .Database
        .MigrateAsync();

    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider,
        builder.Configuration
    );
}

// ==================================================
// 10. MIDDLEWARE PIPELINE
// ==================================================

app.UseHttpsRedirection();

app.UseRouting();

/*
 * Thứ tự rất quan trọng:
 *
 * CORS
 * -> Authentication
 * -> Authorization
 */
app.UseCors(
    AngularCorsPolicy
);

app.UseAuthentication();

app.UseAuthorization();


app.UseStaticFiles();

app.MapControllers();

app.Run();