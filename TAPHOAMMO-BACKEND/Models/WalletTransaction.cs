using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public enum TransactionType
    {
        Deposit = 1,           // Nạp tiền
        Withdrawal = 2,        // Rút tiền
        Purchase = 3,          // Mua hàng
        TransferToSeller = 4,  // Admin chuyển tiền cho seller (sau 3 ngày)
        Refund = 5             // Hoàn tiền (nếu có report)
    }

    public enum TransactionStatus
    {
        Pending = 1,      // Đang chờ
        Completed = 2,    // Hoàn thành
        Failed = 3,       // Thất bại
        Cancelled = 4,    // Đã hủy
        Processing = 5   // Đang xử lý
    }

    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Thông tin tài khoản admin (cho nạp tiền)
        [StringLength(50)]
        public string? AdminAccountNumber { get; set; } // 9945180596

        [StringLength(200)]
        public string? AdminAccountName { get; set; } // Võ Minh Anh

        [StringLength(50)]
        public string? AdminBankName { get; set; } // VCB

        // Thông tin seller (cho giao dịch mua hàng và chuyển tiền)
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public User? Seller { get; set; }

        // Thông tin sản phẩm (cho giao dịch mua hàng)
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // Giao dịch liên quan (ví dụ: Purchase -> TransferToSeller)
        public int? RelatedTransactionId { get; set; }

        [ForeignKey("RelatedTransactionId")]
        public WalletTransaction? RelatedTransaction { get; set; }

        // Thời hạn 3 ngày để report (cho giao dịch mua hàng)
        public DateTime? ReportDeadline { get; set; } // CreatedAt + 3 days

        // Thông tin rút tiền (cho seller)
        [StringLength(50)]
        public string? WithdrawalAccountNumber { get; set; }

        [StringLength(200)]
        public string? WithdrawalAccountName { get; set; }

        [StringLength(50)]
        public string? WithdrawalBankName { get; set; }

        // Ảnh chứng từ (cho nạp tiền)
        [StringLength(500)]
        public string? ProofImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Cột để lưu các trường đã được cập nhật (JSON format)
        [StringLength(2000)]
        public string? UpdatedFields { get; set; } // JSON: {"field1": "oldValue->newValue", "field2": "oldValue->newValue"}
    }
}

