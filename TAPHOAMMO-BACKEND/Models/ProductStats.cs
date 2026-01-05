using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAPHOAMMO_BACKEND.Models
{
    /// <summary>
    /// Thống kê sản phẩm - tách riêng để dễ quản lý và không ảnh hưởng đến thông tin cơ bản
    /// </summary>
    public class ProductStats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; } // Foreign key to Product

        // Đánh giá và thống kê sản phẩm
        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0; // Đánh giá trung bình (0-5 sao)

        public int TotalRatings { get; set; } = 0; // Tổng số lượt đánh giá

        public int TotalSales { get; set; } = 0; // Số lượng đã bán

        public int TotalComplaints { get; set; } = 0; // Số khiếu nại

        public int TotalViews { get; set; } = 0; // Số lượt xem

        public int TotalFavorites { get; set; } = 0; // Số lượt yêu thích

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}

