namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = null!;
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CCCD { get; set; }
        public string? Phone { get; set; }
        public string? FacebookLink { get; set; }
        public string? ShopName { get; set; }
        public bool IsVerified { get; set; }
    }
}

