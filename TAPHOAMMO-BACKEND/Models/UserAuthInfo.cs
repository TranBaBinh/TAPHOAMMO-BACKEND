using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    /// <summary>
    /// Thông tin xác thực của user - tách riêng để dễ quản lý
    /// Bao gồm: Google OAuth, Password, RefreshToken, 2FA
    /// </summary>
    public class UserAuthInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign key to User

        // Password authentication (cho đăng nhập thường với OTP)
        public string? PasswordHash { get; set; } // Null nếu chỉ đăng nhập bằng Google

        // Google OAuth
        public string? GoogleId { get; set; } // For Google OAuth

        // Refresh Token
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Security
        public bool TwoFactorEnabled { get; set; } = false; // Bảo mật 2 lớp

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

