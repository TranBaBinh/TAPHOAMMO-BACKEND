using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PendingBalance { get; set; } = 0; // Số tiền đang chờ (chưa được chuyển cho seller)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Cột để lưu các trường đã được cập nhật (JSON format)
        [StringLength(2000)]
        public string? UpdatedFields { get; set; } // JSON: {"field1": "oldValue->newValue", "field2": "oldValue->newValue"}

        // Mã nạp tiền cố định cho mỗi user (dùng làm nội dung chuyển khoản)
        [StringLength(50)]
        public string? DepositCode { get; set; } // Format: NAP{userId} hoặc mã unique
    }
}

