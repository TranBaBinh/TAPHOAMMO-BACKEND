using System.ComponentModel.DataAnnotations;

namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    /// <summary>
    /// DTO để cập nhật thông tin ngân hàng
    /// </summary>
    public class UpdateBankInfoDto
    {
        [StringLength(100, ErrorMessage = "Tên ngân hàng không được vượt quá 100 ký tự")]
        public string? BankName { get; set; } // Tên ngân hàng

        [StringLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
        public string? BankAccountNumber { get; set; } // Số tài khoản

        [StringLength(100, ErrorMessage = "Tên chủ tài khoản không được vượt quá 100 ký tự")]
        public string? BankAccountHolder { get; set; } // Tên chủ tài khoản

        [StringLength(200, ErrorMessage = "Chi nhánh không được vượt quá 200 ký tự")]
        public string? BankBranch { get; set; } // Chi nhánh
    }

    /// <summary>
    /// DTO để trả về thông tin ngân hàng
    /// </summary>
    public class BankInfoDto
    {
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankBranch { get; set; }
    }
}

