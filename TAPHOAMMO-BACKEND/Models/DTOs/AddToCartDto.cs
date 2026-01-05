using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class AddToCartDto
    {
        [Required(ErrorMessage = "Product ID là bắt buộc")]
        public int ProductId { get; set; }

        public int? ProductOptionId { get; set; } // Optional: Nếu chọn option cụ thể

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;
    }
}

