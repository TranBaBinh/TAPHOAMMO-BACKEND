using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1-5 sao

        [StringLength(1000)]
        public string? Comment { get; set; } // Bình luận đánh giá

        public bool IsComplaint { get; set; } = false; // Có phải khiếu nại không

        [StringLength(500)]
        public string? ComplaintReason { get; set; } // Lý do khiếu nại

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

