using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using BCrypt.Net;

namespace TAPHOAMMO_BACKEND.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                return; // Data already seeded
            }

            // Create sample users
            var users = new List<User>
            {
                new User
                {
                    Username = "user1",
                    Email = "user1@example.com",
                    Role = "user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "seller1",
                    Email = "seller1@example.com",
                    Role = "seller",
                    ShopName = "MMO Services Pro",
                    ShopDescription = "Chuyên cung cấp các dịch vụ MMO chất lượng cao, uy tín, giá tốt",
                    ShopAvatar = "https://via.placeholder.com/200x200?text=MMO+Pro",
                    Telegram = "@mmoservicespro",
                    Rating = 4.8m,
                    TotalRatings = 1250,
                    TotalSales = 8500,
                    TotalComplaints = 12,
                    TotalProducts = 25,
                    JoinDate = DateTime.UtcNow.AddMonths(-18),
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "seller2",
                    Email = "seller2@example.com",
                    Role = "seller",
                    ShopName = "Fast MMO Shop",
                    ShopDescription = "Dịch vụ MMO nhanh chóng, giá rẻ, hỗ trợ 24/7",
                    ShopAvatar = "https://via.placeholder.com/200x200?text=Fast+MMO",
                    Telegram = "@fastmmoshop",
                    Rating = 4.5m,
                    TotalRatings = 890,
                    TotalSales = 5200,
                    TotalComplaints = 8,
                    TotalProducts = 18,
                    JoinDate = DateTime.UtcNow.AddMonths(-12),
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "quanghung",
                    Email = "quanghung@example.com",
                    Role = "seller",
                    ShopName = "Quang Hung MMO",
                    ShopDescription = "Dịch vụ MMO đa dạng, chất lượng, giá cả hợp lý",
                    ShopAvatar = "https://via.placeholder.com/200x200?text=Quang+Hung",
                    Telegram = "@quanghung",
                    Rating = 4.7m,
                    TotalRatings = 2100,
                    TotalSales = 136276,
                    TotalComplaints = 15,
                    TotalProducts = 35,
                    JoinDate = DateTime.UtcNow.AddMonths(-24),
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "seller3",
                    Email = "seller3@example.com",
                    Role = "seller",
                    ShopName = "Premium MMO Store",
                    ShopDescription = "Dịch vụ MMO cao cấp, uy tín số 1",
                    ShopAvatar = "https://via.placeholder.com/200x200?text=Premium",
                    Telegram = "@premiummmo",
                    Rating = 4.9m,
                    TotalRatings = 3200,
                    TotalSales = 25000,
                    TotalComplaints = 5,
                    TotalProducts = 42,
                    JoinDate = DateTime.UtcNow.AddMonths(-30),
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "seller4",
                    Email = "seller4@example.com",
                    Role = "seller",
                    ShopName = "Budget MMO",
                    ShopDescription = "Dịch vụ MMO giá rẻ nhất thị trường",
                    ShopAvatar = "https://via.placeholder.com/200x200?text=Budget",
                    Telegram = "@budgetmmo",
                    Rating = 4.3m,
                    TotalRatings = 650,
                    TotalSales = 3800,
                    TotalComplaints = 10,
                    TotalProducts = 15,
                    JoinDate = DateTime.UtcNow.AddMonths(-8),
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Tạo UserAuthInfo cho tất cả users
            var authInfos = new List<UserAuthInfo>();
            foreach (var user in users)
            {
                authInfos.Add(new UserAuthInfo
                {
                    UserId = user.Id,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            _context.UserAuthInfos.AddRange(authInfos);
            await _context.SaveChangesAsync();

            // Get seller IDs
            var seller1 = await _context.Users.FirstAsync(u => u.Email == "seller1@example.com");
            var seller2 = await _context.Users.FirstAsync(u => u.Email == "seller2@example.com");
            var quanghung = await _context.Users.FirstAsync(u => u.Email == "quanghung@example.com");
            var seller3 = await _context.Users.FirstAsync(u => u.Email == "seller3@example.com");
            var seller4 = await _context.Users.FirstAsync(u => u.Email == "seller4@example.com");

            var now = DateTime.UtcNow;
            var random = new Random();

            // Create sample MMO service products
            var products = new List<Product>();

            // ========== EMAIL CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Gmail VN NEW REG Tay Thủ Công Siêu Trâu, Khỏe",
                    Description = "Gmail Việt Nam mới đăng ký, tay thủ công, siêu trâu, khỏe mạnh, không trùng",
                    Price = 15000,
                    DiscountPrice = 1000,
                    Stock = 1155,
                    Category = "Email",
                    ServiceType = "New Registration",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Gmail+New+Reg",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Gmail+1\"]",
                    SellerId = quanghung.Id,
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = now.AddDays(-5),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Gmail US Premium - Real Accounts",
                    Description = "Gmail Mỹ premium, tài khoản real, chất lượng cao",
                    Price = 25000,
                    DiscountPrice = 20000,
                    Stock = 800,
                    Category = "Email",
                    ServiceType = "Premium Accounts",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Gmail+Premium",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Gmail+2\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-10),
                    UpdatedAt = now.AddDays(-10),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Gmail Bulk - Số lượng lớn",
                    Description = "Gmail số lượng lớn, giá rẻ, phù hợp cho marketing",
                    Price = 5000,
                    DiscountPrice = 3000,
                    Stock = 5000,
                    Category = "Email",
                    ServiceType = "Bulk Accounts",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Gmail+Bulk",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Gmail+3\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-15),
                    UpdatedAt = now.AddDays(-15),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "Outlook Email - Microsoft Accounts",
                    Description = "Tài khoản Outlook Microsoft, mới đăng ký, bảo hành",
                    Price = 12000,
                    DiscountPrice = 10000,
                    Stock = 2000,
                    Category = "Email",
                    ServiceType = "New Registration",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Outlook",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Outlook+1\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-8),
                    UpdatedAt = now.AddDays(-8),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== FACEBOOK CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Tăng tương tác Facebook giá rẻ chất lượng",
                    Description = "Dịch vụ tăng tương tác Facebook (likes, comments, shares) giá rẻ, chất lượng cao",
                    Price = 350,
                    DiscountPrice = 20,
                    Stock = 1155,
                    Category = "Facebook",
                    ServiceType = "Likes",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Facebook+Likes",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=FB+1\"]",
                    SellerId = quanghung.Id,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Facebook Followers - Real Accounts",
                    Description = "Tăng followers Facebook real accounts, không drop, bảo hành 30 ngày",
                    Price = 80000,
                    DiscountPrice = 70000,
                    Stock = 9999,
                    Category = "Facebook",
                    ServiceType = "Followers",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=FB+Followers",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=FB+2\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-12),
                    UpdatedAt = now.AddDays(-12),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Facebook Views - Video Views",
                    Description = "Tăng views video Facebook nhanh chóng, chất lượng",
                    Price = 50000,
                    DiscountPrice = 40000,
                    Stock = 9999,
                    Category = "Facebook",
                    ServiceType = "Views",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=FB+Views",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=FB+3\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-7),
                    UpdatedAt = now.AddDays(-7),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "Facebook Comments - Real Comments",
                    Description = "Dịch vụ tăng comments Facebook real, có nội dung",
                    Price = 60000,
                    DiscountPrice = 50000,
                    Stock = 5000,
                    Category = "Facebook",
                    ServiceType = "Comments",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=FB+Comments",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=FB+4\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-20),
                    UpdatedAt = now.AddDays(-20),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== TIKTOK CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Dịch vụ Tik Tok Tăng Follow Chuyên Cho Tik Tok Bật Kiếm Tiền Tik Tok Beta",
                    Description = "Dịch vụ TikTok tăng follow chuyên nghiệp, phù hợp cho TikTok bật kiếm tiền, TikTok Beta",
                    Price = 15000,
                    DiscountPrice = 1000,
                    Stock = 1155,
                    Category = "TikTok",
                    ServiceType = "Followers",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=TikTok+Followers",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=TikTok+1\"]",
                    SellerId = quanghung.Id,
                    CreatedAt = now.AddDays(-4),
                    UpdatedAt = now.AddDays(-4),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "TikTok Views - 10K Views",
                    Description = "Tăng views TikTok nhanh chóng, chất lượng, hỗ trợ 24/7",
                    Price = 30000,
                    DiscountPrice = 25000,
                    Stock = 9999,
                    Category = "TikTok",
                    ServiceType = "Views",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=TikTok+Views",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=TikTok+2\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-11),
                    UpdatedAt = now.AddDays(-11),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "TikTok Likes - Real Likes",
                    Description = "Tăng likes TikTok real, chất lượng cao, không drop",
                    Price = 40000,
                    DiscountPrice = 35000,
                    Stock = 9999,
                    Category = "TikTok",
                    ServiceType = "Likes",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=TikTok+Likes",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=TikTok+3\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-6),
                    UpdatedAt = now.AddDays(-6),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "TikTok Shares - Tăng chia sẻ",
                    Description = "Dịch vụ tăng shares TikTok, tăng độ lan tỏa",
                    Price = 50000,
                    DiscountPrice = 45000,
                    Stock = 5000,
                    Category = "TikTok",
                    ServiceType = "Shares",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=TikTok+Shares",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=TikTok+4\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-18),
                    UpdatedAt = now.AddDays(-18),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== TELEGRAM CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Telegram Followers - 1000 Followers",
                    Description = "Dịch vụ tăng followers Telegram chất lượng cao, real accounts, không drop",
                    Price = 50000,
                    DiscountPrice = 45000,
                    Stock = 9999,
                    Category = "Telegram",
                    ServiceType = "Followers",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Telegram+Followers",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Telegram+1\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "nâng cấp telegram premium (1 tháng - 3 tháng - 6 tháng - 12 tháng)",
                    Description = "Nâng cấp Telegram Premium với nhiều gói: 1 tháng, 3 tháng, 6 tháng, 12 tháng",
                    Price = 900000,
                    DiscountPrice = 140000,
                    Stock = 1155,
                    Category = "Telegram",
                    ServiceType = "Premium",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Telegram+Premium",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Telegram+2\"]",
                    SellerId = quanghung.Id,
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Telegram Members - Group Members",
                    Description = "Tăng members cho group Telegram, real accounts",
                    Price = 60000,
                    DiscountPrice = 50000,
                    Stock = 9999,
                    Category = "Telegram",
                    ServiceType = "Members",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Telegram+Members",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Telegram+3\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-9),
                    UpdatedAt = now.AddDays(-9),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "Telegram Views - Channel Views",
                    Description = "Tăng views cho channel Telegram, chất lượng cao",
                    Price = 40000,
                    DiscountPrice = 35000,
                    Stock = 5000,
                    Category = "Telegram",
                    ServiceType = "Views",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Telegram+Views",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Telegram+4\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-16),
                    UpdatedAt = now.AddDays(-16),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== GOOGLE CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Google Account - Premium Accounts",
                    Description = "Tài khoản Google premium, đầy đủ tính năng, bảo hành",
                    Price = 80000,
                    DiscountPrice = 70000,
                    Stock = 2000,
                    Category = "Google",
                    ServiceType = "Accounts",
                    ProductType = "Tài khoản",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Google+Accounts",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Google+1\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-13),
                    UpdatedAt = now.AddDays(-13),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Google Reviews - Real Reviews",
                    Description = "Dịch vụ tăng reviews Google Maps, real accounts",
                    Price = 100000,
                    DiscountPrice = 90000,
                    Stock = 3000,
                    Category = "Google",
                    ServiceType = "Reviews",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Google+Reviews",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Google+2\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-14),
                    UpdatedAt = now.AddDays(-14),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "Google Play Reviews",
                    Description = "Tăng reviews ứng dụng trên Google Play Store",
                    Price = 120000,
                    DiscountPrice = 100000,
                    Stock = 1500,
                    Category = "Google",
                    ServiceType = "Play Reviews",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Google+Play",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Google+3\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-19),
                    UpdatedAt = now.AddDays(-19),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== SHOPEE CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "Shopee Followers - Tăng Followers Shop",
                    Description = "Tăng followers cho shop Shopee, tăng độ uy tín",
                    Price = 70000,
                    DiscountPrice = 60000,
                    Stock = 5000,
                    Category = "Shopee",
                    ServiceType = "Followers",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Shopee+Followers",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Shopee+1\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-17),
                    UpdatedAt = now.AddDays(-17),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Shopee Reviews - Đánh giá sản phẩm",
                    Description = "Dịch vụ tăng reviews sản phẩm Shopee, real buyers",
                    Price = 90000,
                    DiscountPrice = 80000,
                    Stock = 3000,
                    Category = "Shopee",
                    ServiceType = "Reviews",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Shopee+Reviews",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Shopee+2\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-21),
                    UpdatedAt = now.AddDays(-21),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "Shopee Likes - Tăng lượt thích",
                    Description = "Tăng lượt thích sản phẩm Shopee, tăng độ nổi bật",
                    Price = 50000,
                    DiscountPrice = 40000,
                    Stock = 4000,
                    Category = "Shopee",
                    ServiceType = "Likes",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=Shopee+Likes",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=Shopee+3\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-22),
                    UpdatedAt = now.AddDays(-22),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            // ========== YOUTUBE CATEGORY ==========
            products.AddRange(new[]
            {
                new Product
                {
                    Name = "YouTube Subscribers - 1000 Subscribers",
                    Description = "Tăng subscribers YouTube real, không bot, bảo hành lâu dài",
                    Price = 150000,
                    DiscountPrice = 130000,
                    Stock = 9999,
                    Category = "YouTube",
                    ServiceType = "Subscribers",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=YouTube+Subs",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=YouTube+1\",\"https://via.placeholder.com/400x300?text=YouTube+2\"]",
                    SellerId = seller1.Id,
                    CreatedAt = now.AddDays(-23),
                    UpdatedAt = now.AddDays(-23),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "YouTube Views - Video Views",
                    Description = "Tăng views video YouTube nhanh chóng, chất lượng cao",
                    Price = 80000,
                    DiscountPrice = 70000,
                    Stock = 9999,
                    Category = "YouTube",
                    ServiceType = "Views",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=YouTube+Views",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=YouTube+3\"]",
                    SellerId = seller2.Id,
                    CreatedAt = now.AddDays(-24),
                    UpdatedAt = now.AddDays(-24),
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "YouTube Likes - Video Likes",
                    Description = "Tăng likes video YouTube, real accounts",
                    Price = 100000,
                    DiscountPrice = 90000,
                    Stock = 5000,
                    Category = "YouTube",
                    ServiceType = "Likes",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=YouTube+Likes",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=YouTube+4\"]",
                    SellerId = seller3.Id,
                    CreatedAt = now.AddDays(-25),
                    UpdatedAt = now.AddDays(-25),
                    IsActive = true,
                    IsFeatured = false
                },
                new Product
                {
                    Name = "YouTube Comments - Real Comments",
                    Description = "Dịch vụ tăng comments YouTube, có nội dung, real accounts",
                    Price = 120000,
                    DiscountPrice = 110000,
                    Stock = 3000,
                    Category = "YouTube",
                    ServiceType = "Comments",
                    ProductType = "Dịch vụ khác",
                    ImageUrl = "https://via.placeholder.com/400x300?text=YouTube+Comments",
                    ImageUrls = "[\"https://via.placeholder.com/400x300?text=YouTube+5\"]",
                    SellerId = seller4.Id,
                    CreatedAt = now.AddDays(-26),
                    UpdatedAt = now.AddDays(-26),
                    IsActive = true,
                    IsFeatured = false
                }
            });

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Tạo ProductStats cho tất cả products
            var productStatsList = new List<ProductStats>();
            var productIndex = 0;
            
            // Stats cho từng product theo thứ tự trong list
            var statsData = new[]
            {
                // Email products
                new { Rating = 4.5m, TotalRatings = 3420, TotalSales = 136276, TotalComplaints = 0, TotalViews = 125000, TotalFavorites = 8900 },
                new { Rating = 4.7m, TotalRatings = 1250, TotalSales = 8500, TotalComplaints = 2, TotalViews = 45000, TotalFavorites = 3200 },
                new { Rating = 4.2m, TotalRatings = 890, TotalSales = 5200, TotalComplaints = 5, TotalViews = 28000, TotalFavorites = 1500 },
                new { Rating = 4.6m, TotalRatings = 650, TotalSales = 3800, TotalComplaints = 1, TotalViews = 18000, TotalFavorites = 980 },
                // Facebook products
                new { Rating = 4.5m, TotalRatings = 3420, TotalSales = 136276, TotalComplaints = 0, TotalViews = 125000, TotalFavorites = 8900 },
                new { Rating = 4.8m, TotalRatings = 456, TotalSales = 3200, TotalComplaints = 5, TotalViews = 18900, TotalFavorites = 1200 },
                new { Rating = 4.6m, TotalRatings = 278, TotalSales = 1890, TotalComplaints = 2, TotalViews = 9800, TotalFavorites = 650 },
                new { Rating = 4.4m, TotalRatings = 234, TotalSales = 1450, TotalComplaints = 3, TotalViews = 11200, TotalFavorites = 780 },
                // TikTok products
                new { Rating = 4.5m, TotalRatings = 3420, TotalSales = 136276, TotalComplaints = 0, TotalViews = 125000, TotalFavorites = 8900 },
                new { Rating = 4.6m, TotalRatings = 278, TotalSales = 1890, TotalComplaints = 2, TotalViews = 9800, TotalFavorites = 650 },
                new { Rating = 4.7m, TotalRatings = 456, TotalSales = 3200, TotalComplaints = 5, TotalViews = 18900, TotalFavorites = 1200 },
                new { Rating = 4.3m, TotalRatings = 189, TotalSales = 980, TotalComplaints = 2, TotalViews = 6700, TotalFavorites = 420 },
                // Telegram products
                new { Rating = 4.7m, TotalRatings = 342, TotalSales = 2150, TotalComplaints = 3, TotalViews = 12500, TotalFavorites = 890 },
                new { Rating = 4.5m, TotalRatings = 3420, TotalSales = 136276, TotalComplaints = 0, TotalViews = 125000, TotalFavorites = 8900 },
                new { Rating = 4.6m, TotalRatings = 278, TotalSales = 1890, TotalComplaints = 2, TotalViews = 9800, TotalFavorites = 650 },
                new { Rating = 4.4m, TotalRatings = 234, TotalSales = 1450, TotalComplaints = 3, TotalViews = 11200, TotalFavorites = 780 },
                // Google products
                new { Rating = 4.8m, TotalRatings = 456, TotalSales = 3200, TotalComplaints = 5, TotalViews = 18900, TotalFavorites = 1200 },
                new { Rating = 4.7m, TotalRatings = 342, TotalSales = 2150, TotalComplaints = 3, TotalViews = 12500, TotalFavorites = 890 },
                new { Rating = 4.6m, TotalRatings = 278, TotalSales = 1890, TotalComplaints = 2, TotalViews = 9800, TotalFavorites = 650 },
                // Shopee products
                new { Rating = 4.5m, TotalRatings = 234, TotalSales = 1450, TotalComplaints = 3, TotalViews = 11200, TotalFavorites = 780 },
                new { Rating = 4.7m, TotalRatings = 456, TotalSales = 3200, TotalComplaints = 5, TotalViews = 18900, TotalFavorites = 1200 },
                new { Rating = 4.4m, TotalRatings = 189, TotalSales = 980, TotalComplaints = 2, TotalViews = 6700, TotalFavorites = 420 },
                // YouTube products
                new { Rating = 4.5m, TotalRatings = 189, TotalSales = 980, TotalComplaints = 2, TotalViews = 6700, TotalFavorites = 420 },
                new { Rating = 4.6m, TotalRatings = 278, TotalSales = 1890, TotalComplaints = 2, TotalViews = 9800, TotalFavorites = 650 },
                new { Rating = 4.7m, TotalRatings = 342, TotalSales = 2150, TotalComplaints = 3, TotalViews = 12500, TotalFavorites = 890 },
                new { Rating = 4.4m, TotalRatings = 234, TotalSales = 1450, TotalComplaints = 3, TotalViews = 11200, TotalFavorites = 780 }
            };

            foreach (var product in products)
            {
                if (productIndex < statsData.Length)
                {
                    var stats = statsData[productIndex];
                    productStatsList.Add(new ProductStats
                    {
                        ProductId = product.Id,
                        Rating = stats.Rating,
                        TotalRatings = stats.TotalRatings,
                        TotalSales = stats.TotalSales,
                        TotalComplaints = stats.TotalComplaints,
                        TotalViews = stats.TotalViews,
                        TotalFavorites = stats.TotalFavorites,
                        CreatedAt = product.CreatedAt,
                        UpdatedAt = product.UpdatedAt
                    });
                    productIndex++;
                }
            }

            _context.ProductStats.AddRange(productStatsList);
            await _context.SaveChangesAsync();
        }

        public async Task ResetAndSeedAsync()
        {
            // Xóa tất cả dữ liệu cũ
            _context.Products.RemoveRange(_context.Products);
            _context.Users.RemoveRange(_context.Users);
            await _context.SaveChangesAsync();

            // Seed lại dữ liệu mới
            await SeedAsync();
        }
    }
}

