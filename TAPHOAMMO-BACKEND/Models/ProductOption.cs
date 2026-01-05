using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    public class ProductOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // Ví dụ: "1 ngày", "7 ngày", "30 ngày"

        [StringLength(500)]
        public string? Description { get; set; } // Mô tả option (tùy chọn)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Giá của option này

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } // Giá khuyến mãi (tùy chọn)

        public int Stock { get; set; } = 0; // Số lượng tồn kho

        public int SortOrder { get; set; } = 0; // Thứ tự hiển thị (sắp xếp)

        public bool IsActive { get; set; } = true; // Có hiển thị không

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

