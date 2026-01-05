using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class DepositRequestDto
    {
        [Required]
        [Range(0.01, 999999999, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ProofImageUrl { get; set; } // Ảnh chứng từ chuyển khoản
    }
}

