using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class UpdateUserProfileDto
    {
        [StringLength(100, ErrorMessage = "Tên gian hàng không được vượt quá 100 ký tự")]
        public string? ShopName { get; set; } // Tên gian hàng (tên của web)

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
        public string? EKYCFrontImage { get; set; } // Ảnh căn cước mặt trước

        [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
        public string? EKYCBackImage { get; set; } // Ảnh căn cước mặt sau

        [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự")]
        public string? EKYCPortraitImage { get; set; } // Ảnh chân dung
    }
}

