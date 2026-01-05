using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class BecomeSellerDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD là bắt buộc")]
        [StringLength(20, MinimumLength = 9, ErrorMessage = "Số CCCD phải từ 9 đến 20 ký tự")]
        public string CCCD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Số điện thoại phải từ 10 đến 20 ký tự")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ được chứa số")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Link Facebook là bắt buộc")]
        [StringLength(500, ErrorMessage = "Link Facebook không được quá 500 ký tự")]
        [Url(ErrorMessage = "Link Facebook phải là URL hợp lệ")]
        public string FacebookLink { get; set; } = string.Empty;
    }
}

