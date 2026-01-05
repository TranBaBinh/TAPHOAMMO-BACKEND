using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class CreateVatInvoiceInfoDto
    {
        [Required(ErrorMessage = "Loại khách hàng là bắt buộc")]
        public int CustomerType { get; set; } // 1 = Cá nhân, 2 = Công ty

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự")]
        public string? TaxCode { get; set; } // Mã số thuế cá nhân (optional)

        [StringLength(20, ErrorMessage = "Số CCCD không được vượt quá 20 ký tự")]
        public string? CCCD { get; set; } // Số CCCD (optional - có thì đẩy lên)

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; } // Địa chỉ (có nút ghi thông tin)

        public bool IsDefault { get; set; } = false; // Đánh dấu làm mặc định
    }

    public class UpdateVatInvoiceInfoDto
    {
        [Required(ErrorMessage = "Loại khách hàng là bắt buộc")]
        public int CustomerType { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự")]
        public string? TaxCode { get; set; }

        [StringLength(20, ErrorMessage = "Số CCCD không được vượt quá 20 ký tự")]
        public string? CCCD { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class VatInvoiceInfoDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CustomerType { get; set; } // 1 = Cá nhân, 2 = Công ty
        public string CustomerTypeName { get; set; } = string.Empty; // "Cá nhân" hoặc "Công ty"
        public string FullName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? CCCD { get; set; }
        public string? Address { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

