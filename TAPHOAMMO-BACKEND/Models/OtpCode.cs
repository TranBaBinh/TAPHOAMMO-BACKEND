using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models
{
    public class OtpCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string Code { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5); // Mã hết hạn sau 5 phút

        public bool IsUsed { get; set; } = false;

        public string Purpose { get; set; } = "login"; // "login" or "register"
    }
}

