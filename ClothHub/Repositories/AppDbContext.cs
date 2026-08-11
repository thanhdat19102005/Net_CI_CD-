using ClothHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ClothHub.Repositories
{
    public class AppDbContext
      : IdentityDbContext
    {
        public AppDbContext(
            DbContextOptions options)
            : base(options)
        {
        }

        // ==========================
        // Các bảng nghiệp vụ
        // ==========================

        public DbSet<CategoryModel> Categories
            => Set<CategoryModel>();

        public DbSet<BrandModel> Brands
            => Set<BrandModel>();

        public DbSet<SizeModel> Sizes
            => Set<SizeModel>();

        public DbSet<SupplierModel> Suppliers
            => Set<SupplierModel>();

        public DbSet<ProductModel> Products
            => Set<ProductModel>();

        public DbSet<EmployeeInfo> EmployeeInfos
            => Set<EmployeeInfo>();


        // ==================================================
        // [BỔ SUNG 1]
        // Thêm bảng Orders và OrderDetails vào DbContext
        // ==================================================

        public DbSet<OrderModel> Orders
            => Set<OrderModel>();

        public DbSet<OrderDetailModel> OrderDetails
            => Set<OrderDetailModel>();


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            /*
             * Bắt buộc gọi base để Identity cấu hình:
             *
             * AspNetUsers
             * AspNetRoles
             * AspNetUserRoles
             * AspNetUserClaims
             * AspNetRoleClaims
             * AspNetUserLogins
             * AspNetUserTokens
             */
            base.OnModelCreating(modelBuilder);


            // ==========================
            // Tên bảng nghiệp vụ
            // ==========================

            modelBuilder.Entity<CategoryModel>()
                .ToTable("Categories");

            modelBuilder.Entity<BrandModel>()
                .ToTable("Brands");

            modelBuilder.Entity<SizeModel>()
                .ToTable("Sizes");

            modelBuilder.Entity<SupplierModel>()
                .ToTable("Suppliers");

            modelBuilder.Entity<ProductModel>()
                .ToTable("Products");

            modelBuilder.Entity<EmployeeInfo>()
                .ToTable("EmployeeInfos");


            // ==================================================
            // [BỔ SUNG 2]
            // Đặt tên bảng Order và OrderDetail
            // ==================================================

            modelBuilder.Entity<OrderModel>()
                .ToTable("Orders");

            modelBuilder.Entity<OrderDetailModel>()
                .ToTable("OrderDetails");


            // ==========================
            // Category - Product
            // Một loại có nhiều sản phẩm
            // ==========================

            modelBuilder.Entity<ProductModel>()
                .HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================
            // Brand - Product
            // Một thương hiệu có nhiều sản phẩm
            // ==========================

            modelBuilder.Entity<ProductModel>()
                .HasOne(product => product.Brand)
                .WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId)
                .OnDelete(DeleteBehavior.SetNull);


            // ==========================
            // Size - Product
            // Một kích thước có nhiều sản phẩm
            // ==========================

            modelBuilder.Entity<ProductModel>()
                .HasOne(product => product.Size)
                .WithMany(size => size.Products)
                .HasForeignKey(product => product.SizeId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================
            // AppUser - EmployeeInfo
            // Quan hệ một - một
            // ==========================

            modelBuilder.Entity<EmployeeInfo>()
                .HasOne(employee => employee.User)
                .WithOne(user => user.EmployeeInfo)
                .HasForeignKey<EmployeeInfo>(
                    employee => employee.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             * Mỗi tài khoản chỉ được liên kết
             * với một hồ sơ nhân viên.
             */
            modelBuilder.Entity<EmployeeInfo>()
                .HasIndex(employee => employee.UserId)
                .IsUnique();


            // ==================================================
            // [BỔ SUNG 3]
            // AppUser - Order
            //
            // Một User có thể có nhiều Order.
            //
            // Restrict:
            // Nếu User đang có Order thì không cho
            // xóa vật lý User đó.
            // ==================================================

            modelBuilder.Entity<OrderModel>()
                .HasOne(order => order.User)
                .WithMany()
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==================================================
            // [BỔ SUNG 4]
            // Order - OrderDetail
            //
            // Một Order có nhiều OrderDetail.
            //
            // Dùng SetNull:
            //
            // Xóa Order
            //      ↓
            // OrderDetail vẫn tồn tại
            //      ↓
            // OrderDetail.OrderId = null
            //
            // Vì vậy OrderDetailModel phải khai báo:
            //
            // public int? OrderId { get; set; }
            // ==================================================

            modelBuilder.Entity<OrderDetailModel>()
                .HasOne(detail => detail.Order)
                .WithMany(order => order.OrderDetails)
                .HasForeignKey(detail => detail.OrderId)
                .OnDelete(DeleteBehavior.SetNull);


            // ==================================================
            // [BỔ SUNG 5]
            // Product - OrderDetail
            //
            // Một Product có thể xuất hiện
            // trong nhiều OrderDetail.
            //
            // Restrict:
            // Nếu Product đã được sử dụng trong OrderDetail
            // thì không cho xóa vật lý Product đó.
            // ==================================================

            modelBuilder.Entity<OrderDetailModel>()
                .HasOne(detail => detail.Product)
                .WithMany()
                .HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================
            // Không cho phép trùng tên
            // ==========================

            modelBuilder.Entity<CategoryModel>()
                .HasIndex(category => category.Name)
                .IsUnique();

            modelBuilder.Entity<BrandModel>()
                .HasIndex(brand => brand.Name)
                .IsUnique();

            modelBuilder.Entity<SizeModel>()
                .HasIndex(size => size.Name)
                .IsUnique();


            // ==================================================
            // [BỔ SUNG 6]
            // OrderCode không được phép trùng nhau
            // ==================================================

            modelBuilder.Entity<OrderModel>()
                .HasIndex(order => order.OrderCode)
                .IsUnique();


            // ==========================
            // Giá trị trạng thái mặc định
            // Không cấu hình Status cho AppUserModel
            // ==========================

            modelBuilder.Entity<CategoryModel>()
                .Property(category => category.Status)
                .HasDefaultValue(1);

            modelBuilder.Entity<BrandModel>()
                .Property(brand => brand.Status)
                .HasDefaultValue(1);

            modelBuilder.Entity<SizeModel>()
                .Property(size => size.Status)
                .HasDefaultValue(1);

            modelBuilder.Entity<SupplierModel>()
                .Property(supplier => supplier.Status)
                .HasDefaultValue(1);

            modelBuilder.Entity<ProductModel>()
                .Property(product => product.Status)
                .HasDefaultValue(1);

            modelBuilder.Entity<EmployeeInfo>()
                .Property(employee => employee.Status)
                .HasDefaultValue(1);


            // ==================================================
            // [BỔ SUNG 7]
            // Giá trị mặc định cho Order
            // ==================================================

            /*
             * Status:
             *
             * 0 = Chờ xác nhận
             * 1 = Đã xác nhận
             * 2 = Đang giao hàng
             * 3 = Đã giao hàng
             * 4 = Đã hủy
             */
            modelBuilder.Entity<OrderModel>()
                .Property(order => order.Status)
                .HasDefaultValue(0);


            /*
             * PaymentStatus:
             *
             * 0 = Chưa thanh toán
             * 1 = Đã thanh toán
             */
            modelBuilder.Entity<OrderModel>()
                .Property(order => order.PaymentStatus)
                .HasDefaultValue(0);


            // ==========================
            // Kiểu dữ liệu tiền Product
            // ==========================

            modelBuilder.Entity<ProductModel>()
                .Property(product => product.ImportPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductModel>()
                .Property(product => product.Price)
                .HasPrecision(18, 2);


            // ==================================================
            // [BỔ SUNG 8]
            // Kiểu dữ liệu tiền của Order
            // ==================================================

            modelBuilder.Entity<OrderModel>()
                .Property(order => order.SubTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderModel>()
                .Property(order => order.ShippingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderModel>()
                .Property(order => order.Discount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderModel>()
                .Property(order => order.TotalAmount)
                .HasPrecision(18, 2);


            // ==================================================
            // [BỔ SUNG 9]
            // Kiểu dữ liệu tiền của OrderDetail
            // ==================================================

            modelBuilder.Entity<OrderDetailModel>()
                .Property(detail => detail.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetailModel>()
                .Property(detail => detail.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}
