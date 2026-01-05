using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/seller")]
    [Authorize(Roles = "seller")]
    public class SellerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SellerController> _logger;

        public SellerController(ApplicationDbContext context, ILogger<SellerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm của seller (cho khu vực quản lý shop)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetMyProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] bool? isActive = null) // null = tất cả, true = chỉ active, false = chỉ inactive
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Base query - lấy sản phẩm của seller
            var query = _context.Products
                .Where(p => p.SellerId == userId)
                .Include(p => p.Stats)
                .AsQueryable();

            // Filter by IsActive if specified
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)) ||
                    (p.Category != null && p.Category.ToLower().Contains(search))
                );
            }

            // Sorting
            switch (sortBy?.ToLower())
            {
                case "name_asc":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(p => p.Name);
                    break;
                case "price_asc":
                    query = query.OrderBy(p => p.DiscountPrice ?? p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price);
                    break;
                case "sales":
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0);
                    break;
                case "oldest":
                    query = query.OrderBy(p => p.CreatedAt);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.ProductCode,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.DiscountPrice,
                    p.Stock,
                    p.ImageUrl,
                    p.ImageUrls,
                    p.Category,
                    p.ServiceType,
                    p.ProductType,
                    p.EmailType,
                    Rating = p.Stats != null ? p.Stats.Rating : 0,
                    TotalRatings = p.Stats != null ? p.Stats.TotalRatings : 0,
                    TotalSales = p.Stats != null ? p.Stats.TotalSales : 0,
                    TotalComplaints = p.Stats != null ? p.Stats.TotalComplaints : 0,
                    TotalViews = p.Stats != null ? p.Stats.TotalViews : 0,
                    TotalFavorites = p.Stats != null ? p.Stats.TotalFavorites : 0,
                    p.IsFeatured,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                products,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                search,
                sortBy
            });
        }

        /// <summary>
        /// Lấy thống kê shop của seller
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetShopStats()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var totalProducts = await _context.Products.CountAsync(p => p.SellerId == userId);
            var activeProducts = await _context.Products.CountAsync(p => p.SellerId == userId && p.IsActive);
            var totalSales = await _context.Products
                .Where(p => p.SellerId == userId)
                .Include(p => p.Stats)
                .SumAsync(p => p.Stats != null ? p.Stats.TotalSales : 0);
            var totalViews = await _context.Products
                .Where(p => p.SellerId == userId)
                .Include(p => p.Stats)
                .SumAsync(p => p.Stats != null ? p.Stats.TotalViews : 0);
            var totalFavorites = await _context.Products
                .Where(p => p.SellerId == userId)
                .Include(p => p.Stats)
                .SumAsync(p => p.Stats != null ? p.Stats.TotalFavorites : 0);

            var user = await _context.Users.FindAsync(userId);

            return Ok(new
            {
                totalProducts,
                activeProducts,
                inactiveProducts = totalProducts - activeProducts,
                totalSales,
                totalViews,
                totalFavorites,
                shopRating = user?.Rating ?? 0,
                totalRatings = user?.TotalRatings ?? 0
            });
        }
    }
}

