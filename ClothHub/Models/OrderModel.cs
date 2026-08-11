using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothHub.Models
{
    public class OrderModel
    {
        // ==================================================
        // PRIMARY KEY
        // ==================================================

        [Key]
        public int Id { get; set; }

        // ==================================================
        // ORDER INFORMATION
        // ==================================================

        [Required]
        [StringLength(30)]
        public string OrderCode { get; set; } = string.Empty;

        // ==================================================
        // USER
        // ==================================================

        /*
         * User đã đăng nhập và tạo đơn hàng.
         *
         * Thông tin như:
         * - FullName
         * - PhoneNumber
         * - Address
         *
         * sẽ lấy thông qua AppUserModel.
         */
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public AppUserModel? User { get; set; }

        // ==================================================
        // NOTE
        // ==================================================

        [StringLength(1000)]
        public string? Note { get; set; }

        // ==================================================
        // ORDER STATUS
        // ==================================================

        /*
         * 0 = Chờ xác nhận
         * 1 = Đã xác nhận
         * 2 = Đang giao hàng
         * 3 = Đã giao hàng
         * 4 = Đã hủy
         */
        [Range(0, 4)]
        public int Status { get; set; } = 0;

        // ==================================================
        // PAYMENT STATUS
        // ==================================================

        /*
         * Hệ thống chỉ thanh toán bằng tiền mặt.
         *
         * 0 = Chưa thanh toán
         * 1 = Đã thanh toán
         */
        [Range(0, 1)]
        public int PaymentStatus { get; set; } = 0;

        // ==================================================
        // MONEY
        // ==================================================

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        // ==================================================
        // TIME
        // ==================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /*
         * Thời điểm cửa hàng đã nhận tiền mặt.
         */
        public DateTime? PaidAt { get; set; }

        // ==================================================
        // NAVIGATION
        // ==================================================

        public ICollection<OrderDetailModel> OrderDetails { get; set; }
            = new List<OrderDetailModel>();
    }

}
