using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class WithdrawRequestDto
    {
        [Required]
        [Range(0.01, 999999999, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string AccountName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BankName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

