using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class Toggle2FADto
    {
        [Required(ErrorMessage = "Trạng thái 2FA là bắt buộc")]
        public bool Enable { get; set; } // true để bật, false để tắt
    }
}

