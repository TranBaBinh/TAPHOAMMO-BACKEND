using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty; // Mã sản phẩm unique (ví dụ: "PRD-ABC123")

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        public int Stock { get; set; } = 0;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(500)]
        public string? ImageUrls { get; set; } // JSON array of image URLs

        [StringLength(50)]
        public string? Category { get; set; } // Loại dịch vụ MMO: "Telegram", "TikTok", "Facebook", etc.

        [StringLength(100)]
        public string? ServiceType { get; set; } // Loại dịch vụ: "Followers", "Likes", "Views", etc.

        [StringLength(50)]
        public string? ProductType { get; set; } // Loại sản phẩm: "Tài khoản", "Phần mềm", "Cái khác"

        [StringLength(50)]
        public string? EmailType { get; set; } // Loại email: "Gmail", "Hotmail", "Outlookmail", "Rumail", "Yahoomail", "Other"

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public User Seller { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false; // Sản phẩm nổi bật

        // Navigation properties
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
        
        public ProductStats? Stats { get; set; } // Thống kê sản phẩm (tách riêng table)
    }
}

