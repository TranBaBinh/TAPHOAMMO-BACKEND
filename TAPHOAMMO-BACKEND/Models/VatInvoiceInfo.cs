using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public enum CustomerType
    {
        Individual = 1,  // Cá nhân
        Company = 2     // Công ty
    }

    public class VatInvoiceInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public CustomerType CustomerType { get; set; } = CustomerType.Individual;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty; // Họ và tên

        [StringLength(50)]
        public string? TaxCode { get; set; } // Mã số thuế cá nhân (có thể null nếu chưa có)

        [StringLength(20)]
        public string? CCCD { get; set; } // Số CCCD (optional - có thì đẩy lên)

        [StringLength(500)]
        public string? Address { get; set; } // Địa chỉ (có nút ghi thông tin)

        public bool IsDefault { get; set; } = false; // Đánh dấu thông tin mặc định

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

