namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty; // Deposit, Withdrawal, Purchase, TransferToSeller
        public string Status { get; set; } = string.Empty; // Pending, Completed, Failed, Cancelled
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? AdminAccountNumber { get; set; }
        public string? AdminAccountName { get; set; }
        public string? AdminBankName { get; set; }
        public int? SellerId { get; set; }
        public string? SellerName { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? RelatedTransactionId { get; set; }
        public DateTime? ReportDeadline { get; set; }
        public string? WithdrawalAccountNumber { get; set; }
        public string? WithdrawalAccountName { get; set; }
        public string? WithdrawalBankName { get; set; }
        public string? ProofImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

