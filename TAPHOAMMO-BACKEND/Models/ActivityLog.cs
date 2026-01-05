using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    /// <summary>
    /// Model lưu nhật ký hoạt động của user và hệ thống
    /// </summary>
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID thực hiện hành động (nullable nếu là hoạt động hệ thống)
        /// </summary>
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        /// <summary>
        /// Mô tả hành động bằng tiếng Việt (ví dụ: "Nạp tiền", "Tạo tài khoản", "Đăng nhập")
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Mã thao tác bằng tiếng Anh (ví dụ: "deposit", "register", "login", "purchase")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Địa chỉ IP của user
        /// </summary>
        [StringLength(50)]
        public string? IPAddress { get; set; }

        /// <summary>
        /// Thời gian thực hiện hành động
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Mô tả chi tiết (optional)
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Metadata bổ sung dưới dạng JSON (optional)
        /// Ví dụ: {"amount": 100000, "transactionId": 123, "productId": 456}
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        /// <summary>
        /// User Agent (browser/device info)
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}

