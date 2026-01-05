namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Level & Stats
        public int Level { get; set; } // Level của user (tính từ tổng giao dịch)
        public decimal TotalPurchaseAmount { get; set; } // Tổng tiền đã mua
        public int TotalPurchases { get; set; } // Số sản phẩm đã mua
        
        // Seller stats (nếu là seller)
        public int? TotalShops { get; set; } // Số gian hàng (số products)
        public int? TotalSales { get; set; } // Số lượng đã bán
        public decimal? TotalSaleAmount { get; set; } // Tổng tiền đã bán
        
        // Verification & Security
        public bool IsVerified { get; set; } // Đã xác thực chưa
        public bool TwoFactorEnabled { get; set; } // Bảo mật 2 lớp
        
        // eKYC
        public string? EKYCFrontImage { get; set; }
        public string? EKYCBackImage { get; set; }
        public string? EKYCPortraitImage { get; set; }
        
        // Contact info
        public string? Phone { get; set; }
        
        // Shop info (nếu là seller)
        public string? ShopName { get; set; } // Tên gian hàng

        // Bank Information
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankBranch { get; set; }
    }
}

