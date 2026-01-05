using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    /// <summary>
    /// Thông tin ngân hàng của user - tách riêng để dễ quản lý
    /// </summary>
    public class UserBankInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign key to User

        [StringLength(100)]
        public string? BankName { get; set; } // Tên ngân hàng (VD: Vietcombank, Techcombank, etc.)

        [StringLength(50)]
        public string? BankAccountNumber { get; set; } // Số tài khoản ngân hàng

        [StringLength(100)]
        public string? BankAccountHolder { get; set; } // Tên chủ tài khoản

        [StringLength(200)]
        public string? BankBranch { get; set; } // Chi nhánh ngân hàng

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

