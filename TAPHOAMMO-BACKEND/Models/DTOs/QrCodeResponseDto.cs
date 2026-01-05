namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class QrCodeResponseDto
    {
        public string QrCodeBase64 { get; set; } = string.Empty; // Base64 image
        public string QrCodeUrl { get; set; } = string.Empty; // URL để scan
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string TransferContent { get; set; } = string.Empty; // Nội dung chuyển khoản unique
        public string TransactionId { get; set; } = string.Empty; // Mã giao dịch để tracking
    }
}



