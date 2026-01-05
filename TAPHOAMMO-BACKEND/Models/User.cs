using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [StringLength(200)]
        public string? FullName { get; set; } // Họ và Tên

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "user"; // "user" or "seller"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Seller Information (chỉ áp dụng khi Role = "seller")
        [StringLength(20)]
        public string? CCCD { get; set; } // Số CCCD/Căn cước công dân

        [StringLength(20)]
        public string? Phone { get; set; } // Số điện thoại

        [StringLength(500)]
        public string? FacebookLink { get; set; } // Link Facebook

        [StringLength(100)]
        public string? ShopName { get; set; } // Tên gian hàng

        [StringLength(1000)]
        public string? ShopDescription { get; set; } // Mô tả gian hàng

        [StringLength(500)]
        public string? ShopAvatar { get; set; } // Avatar gian hàng

        [StringLength(100)]
        public string? Telegram { get; set; } // Telegram của seller

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0; // Đánh giá trung bình (0-5 sao)

        public int TotalRatings { get; set; } = 0; // Tổng số lượt đánh giá

        public int TotalSales { get; set; } = 0; // Số lượng đã bán

        public int TotalComplaints { get; set; } = 0; // Số khiếu nại

        public int TotalProducts { get; set; } = 0; // Tổng số sản phẩm

        public DateTime? JoinDate { get; set; } // Ngày tham gia làm seller

        public bool IsVerified { get; set; } = false; // Đã xác thực chưa

        // User Level & Stats
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPurchaseAmount { get; set; } = 0; // Tổng tiền đã mua

        public int TotalPurchases { get; set; } = 0; // Số sản phẩm đã mua

        // eKYC (Định danh điện tử)
        [StringLength(500)]
        public string? EKYCFrontImage { get; set; } // Ảnh căn cước mặt trước

        [StringLength(500)]
        public string? EKYCBackImage { get; set; } // Ảnh căn cước mặt sau

        [StringLength(500)]
        public string? EKYCPortraitImage { get; set; } // Ảnh chân dung

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        
        public UserBankInfo? BankInfo { get; set; } // Thông tin ngân hàng (tách riêng table)
        
        public UserAuthInfo? AuthInfo { get; set; } // Thông tin xác thực (tách riêng table)
    }
}

