namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class WalletBalanceDto
    {
        public decimal Balance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal AvailableBalance { get; set; } // Balance - PendingBalance
    }
}

