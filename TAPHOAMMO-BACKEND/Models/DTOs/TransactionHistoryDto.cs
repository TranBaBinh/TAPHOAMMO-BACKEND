namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class TransactionHistoryDto
    {
        public List<WalletTransactionDto> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

