using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class UpdateProductOptionDto
    {
        [Required(ErrorMessage = "Tên tùy chọn là bắt buộc")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // e.g., "1 ngày", "7 ngày", "1 tháng"

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int Stock { get; set; } = 0;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}

