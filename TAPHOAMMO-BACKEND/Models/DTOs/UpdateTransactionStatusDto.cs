using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class UpdateTransactionStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty; // Completed, Failed, Cancelled

        [StringLength(500)]
        public string? Note { get; set; }
    }
}

