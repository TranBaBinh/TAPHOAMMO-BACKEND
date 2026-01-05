using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class RegisterWithOtpDto
    {
        [Required(ErrorMessage = "Tên tài khoản là bắt buộc")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên tài khoản phải từ 3 đến 100 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập lại mật khẩu là bắt buộc")]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 số")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản")]
        public bool AgreeToTerms { get; set; }

        [Required(ErrorMessage = "Bạn phải đồng ý với chính sách sử dụng")]
        public bool AgreeToPolicy { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "user"; // "user" or "seller"
    }
}

