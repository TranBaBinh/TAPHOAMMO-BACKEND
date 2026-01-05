using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class CreatePurchaseDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0.01, 999999999, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

