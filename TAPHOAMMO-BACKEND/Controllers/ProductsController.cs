using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generate random product code (format: PRD-XXXXXX)
        /// </summary>
        private async Task<string> GenerateProductCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            
            do
            {
                var codeBuilder = new StringBuilder("PRD-");
                for (int i = 0; i < 6; i++)
                {
                    codeBuilder.Append(chars[random.Next(chars.Length)]);
                }
                code = codeBuilder.ToString();
            }
            while (await _context.Products.AnyAsync(p => p.ProductCode == code)); // Đảm bảo unique

            return code;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? serviceType = null,
            [FromQuery] string? productType = null,
            [FromQuery] string? emailTypes = null, // Comma-separated: "Gmail,Hotmail,Outlookmail"
            [FromQuery] string? sortBy = "newest")
        {
            // Base query
            var query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Seller)
                .Include(p => p.Stats)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(search) || 
                    (p.Description != null && p.Description.ToLower().Contains(search)) ||
                    (p.Category != null && p.Category.ToLower().Contains(search)) ||
                    (p.ServiceType != null && p.ServiceType.ToLower().Contains(search))
                );
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.ToLower() == category.ToLower());
            }

            // ServiceType filter
            if (!string.IsNullOrWhiteSpace(serviceType))
            {
                query = query.Where(p => p.ServiceType != null && p.ServiceType.ToLower() == serviceType.ToLower());
            }

            // ProductType filter
            if (!string.IsNullOrWhiteSpace(productType))
            {
                query = query.Where(p => p.ProductType != null && p.ProductType.ToLower() == productType.ToLower());
            }

            // EmailType filter - hỗ trợ nhiều loại email cùng lúc (comma-separated)
            if (!string.IsNullOrWhiteSpace(emailTypes))
            {
                var emailTypeList = emailTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLower())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                if (emailTypeList.Any())
                {
                    // Nếu có "Other" trong danh sách, cần xử lý đặc biệt
                    var hasOther = emailTypeList.Contains("other");
                    var otherTypes = emailTypeList.Where(e => e != "other").ToList();

                    if (hasOther && otherTypes.Any())
                    {
                        // Lọc các emailType trong danh sách HOẶC là null/không khớp với các loại đã định nghĩa
                        var definedTypes = new[] { "gmail", "hotmail", "outlookmail", "rumail", "yahoomail" };
                        query = query.Where(p => 
                            (p.EmailType != null && otherTypes.Contains(p.EmailType.ToLower())) ||
                            (p.EmailType == null || !definedTypes.Contains(p.EmailType.ToLower()))
                        );
                    }
                    else if (hasOther)
                    {
                        // Chỉ lọc "Other" - các emailType không phải là các loại đã định nghĩa
                        var definedTypes = new[] { "gmail", "hotmail", "outlookmail", "rumail", "yahoomail" };
                        query = query.Where(p => 
                            p.EmailType == null || !definedTypes.Contains(p.EmailType.ToLower())
                        );
                    }
                    else
                    {
                        // Lọc theo các emailType được chọn
                        query = query.Where(p => p.EmailType != null && emailTypeList.Contains(p.EmailType.ToLower()));
                    }
                }
            }

            // Sorting - sử dụng Stats
            switch (sortBy?.ToLower())
            {
                case "popular":
                    // Phổ biến - Lượt mua nhiều nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0);
                    break;
                case "bestseller":
                    // Bán chạy nhất - Lượt mua nhiều nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_asc":
                    // Giá tăng dần
                    query = query.OrderBy(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_desc":
                    // Giá giảm dần
                    query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "rating":
                    // Đánh giá cao nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0);
                    break;
                case "newest":
                default:
                    // Mới nhất (mặc định)
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
                    Seller = new
                    {
                        p.Seller.Id,
                        p.Seller.Username,
                        p.Seller.Email,
                        p.Seller.ShopName,
                        p.Seller.Telegram,
                        p.Seller.Rating,
                        p.Seller.IsVerified
                    },
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
                category,
                serviceType,
                productType,
                emailTypes,
                sortBy
            });
        }

        [HttpGet("search-by-name")]
        public async Task<IActionResult> SearchProductsByName(
            [FromQuery] string name,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "newest")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Tên sản phẩm không được để trống" });
            }

            // Base query - chỉ tìm theo tên sản phẩm
            var query = _context.Products
                .Where(p => p.IsActive && p.Name.ToLower().Contains(name.ToLower()))
                .Include(p => p.Seller)
                .Include(p => p.Stats)
                .AsQueryable();

            // Sorting
            switch (sortBy?.ToLower())
            {
                case "popular":
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0);
                    break;
                case "bestseller":
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_asc":
                    query = query.OrderBy(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "rating":
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0);
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
                    Seller = new
                    {
                        p.Seller.Id,
                        p.Seller.Username,
                        p.Seller.Email,
                        p.Seller.ShopName,
                        p.Seller.Telegram,
                        p.Seller.Rating,
                        p.Seller.IsVerified
                    },
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
                searchName = name,
                sortBy
            });
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var categories = await _context.Products
                .Where(p => p.IsActive && p.Category != null)
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var serviceTypes = await _context.Products
                .Where(p => p.IsActive && p.ServiceType != null)
                .Select(p => p.ServiceType!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var productTypes = await _context.Products
                .Where(p => p.IsActive && p.ProductType != null)
                .Select(p => p.ProductType!)
                .Distinct()
                .OrderBy(pt => pt)
                .ToListAsync();

            var emailTypes = await _context.Products
                .Where(p => p.IsActive && p.EmailType != null)
                .Select(p => p.EmailType!)
                .Distinct()
                .OrderBy(et => et)
                .ToListAsync();

            return Ok(new
            {
                categories,
                serviceTypes,
                productTypes,
                emailTypes
            });
        }

        [HttpGet("category/{categoryName}/service-types")]
        public async Task<IActionResult> GetServiceTypesByCategory(string categoryName)
        {
            var serviceTypes = await _context.Products
                .Where(p => p.IsActive && 
                           p.Category != null && 
                           p.Category.ToLower() == categoryName.ToLower() &&
                           p.ServiceType != null)
                .Select(p => p.ServiceType!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return Ok(new
            {
                category = categoryName,
                serviceTypes
            });
        }

        [HttpGet("category/{categoryName}")]
        public async Task<IActionResult> GetProductsByCategory(
            string categoryName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? serviceType = null,
            [FromQuery] string? productType = null,
            [FromQuery] string? sortBy = "newest")
        {
            // Base query - lọc theo category
            var query = _context.Products
                .Where(p => p.IsActive && 
                           p.Category != null && 
                           p.Category.ToLower() == categoryName.ToLower())
                .Include(p => p.Seller)
                .AsQueryable();

            // Lọc theo serviceType nếu có
            if (!string.IsNullOrWhiteSpace(serviceType))
            {
                query = query.Where(p => p.ServiceType != null && 
                                         p.ServiceType.ToLower() == serviceType.ToLower());
            }

            // Lọc theo productType nếu có
            if (!string.IsNullOrWhiteSpace(productType))
            {
                query = query.Where(p => p.ProductType != null && 
                                         p.ProductType.ToLower() == productType.ToLower());
            }

            // Sorting - sử dụng Stats
            switch (sortBy?.ToLower())
            {
                case "popular":
                    // Phổ biến - Lượt mua nhiều nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0);
                    break;
                case "bestseller":
                    // Bán chạy nhất - Lượt mua nhiều nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_asc":
                    // Giá tăng dần
                    query = query.OrderBy(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "price_desc":
                    // Giá giảm dần
                    query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                                 .ThenByDescending(p => p.CreatedAt);
                    break;
                case "rating":
                    // Đánh giá cao nhất
                    query = query.OrderByDescending(p => p.Stats != null ? p.Stats.Rating : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalRatings : 0)
                                 .ThenByDescending(p => p.Stats != null ? p.Stats.TotalSales : 0);
                    break;
                case "newest":
                default:
                    // Mới nhất (mặc định)
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
                    Seller = new
                    {
                        p.Seller.Id,
                        p.Seller.Username,
                        p.Seller.Email,
                        p.Seller.ShopName,
                        p.Seller.Telegram,
                        p.Seller.Rating,
                        p.Seller.IsVerified
                    },
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                category = categoryName,
                products,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                serviceType,
                productType,
                sortBy
            });
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetProductByCode(string code)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Options)
                .Include(p => p.Stats)
                .FirstOrDefaultAsync(p => p.ProductCode == code && p.IsActive);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            // Tính % khiếu nại (sử dụng Stats)
            var stats = product.Stats ?? new ProductStats { ProductId = product.Id };
            var complaintPercentage = stats.TotalSales > 0 
                ? Math.Round((decimal)stats.TotalComplaints / stats.TotalSales * 100, 2)
                : 0;

            // Lấy options (chỉ IsActive)
            var options = product.Options
                .Where(o => o.IsActive)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Price)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.Description,
                    o.Price,
                    o.DiscountPrice,
                    o.Stock,
                    o.SortOrder
                })
                .ToList();

            return Ok(new
            {
                product.Id,
                product.ProductCode,
                product.Name,
                product.Description,
                product.Price,
                product.DiscountPrice,
                product.Stock,
                product.ImageUrl,
                product.ImageUrls,
                product.Category,
                product.ServiceType,
                product.ProductType,
                product.EmailType,
                Rating = stats.Rating,
                TotalRatings = stats.TotalRatings,
                TotalSales = stats.TotalSales,
                TotalComplaints = stats.TotalComplaints,
                complaintPercentage,
                TotalViews = stats.TotalViews,
                TotalFavorites = stats.TotalFavorites,
                product.IsFeatured,
                options,
                Seller = new
                {
                    product.Seller.Id,
                    product.Seller.Username,
                    product.Seller.Email,
                    product.Seller.ShopName,
                    product.Seller.ShopDescription,
                    product.Seller.ShopAvatar,
                    product.Seller.Telegram,
                    product.Seller.Rating,
                    product.Seller.TotalRatings,
                    product.Seller.TotalSales,
                    product.Seller.TotalComplaints,
                    product.Seller.IsVerified
                },
                product.CreatedAt,
                product.UpdatedAt
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Options)
                .Include(p => p.Stats)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            // Tính % khiếu nại (sử dụng Stats)
            var stats = product.Stats ?? new ProductStats { ProductId = product.Id };
            var complaintPercentage = stats.TotalSales > 0 
                ? Math.Round((decimal)stats.TotalComplaints / stats.TotalSales * 100, 2)
                : 0;

            // Lấy options (chỉ IsActive)
            var options = product.Options
                .Where(o => o.IsActive)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Price)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.Description,
                    o.Price,
                    o.DiscountPrice,
                    o.Stock,
                    o.SortOrder
                })
                .ToList();

            return Ok(new
            {
                product.Id,
                product.ProductCode,
                product.Name,
                product.Description,
                product.Price,
                product.DiscountPrice,
                product.Stock,
                product.ImageUrl,
                product.ImageUrls,
                product.Category,
                product.ServiceType,
                product.ProductType,
                product.EmailType,
                Rating = stats.Rating,
                TotalRatings = stats.TotalRatings,
                TotalSales = stats.TotalSales,
                TotalComplaints = stats.TotalComplaints,
                complaintPercentage,
                TotalViews = stats.TotalViews,
                TotalFavorites = stats.TotalFavorites,
                product.IsFeatured,
                options,
                Seller = new
                {
                    product.Seller.Id,
                    product.Seller.Username,
                    product.Seller.Email,
                    product.Seller.ShopName,
                    product.Seller.ShopDescription,
                    product.Seller.ShopAvatar,
                    product.Seller.Telegram,
                    product.Seller.Rating,
                    product.Seller.TotalRatings,
                    product.Seller.TotalSales,
                    product.Seller.TotalComplaints,
                    product.Seller.IsVerified
                },
                product.CreatedAt,
                product.UpdatedAt
            });
        }

        [Authorize(Roles = "seller")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.Role != "seller")
            {
                return Unauthorized(new { message = "Chỉ seller mới có thể tạo sản phẩm" });
            }

            // Generate unique product code
            var productCode = await GenerateProductCodeAsync();

            var product = new Product
            {
                ProductCode = productCode,
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                DiscountPrice = createProductDto.DiscountPrice,
                Stock = createProductDto.Stock,
                ImageUrl = createProductDto.ImageUrl,
                ImageUrls = createProductDto.ImageUrls,
                Category = createProductDto.Category,
                ServiceType = createProductDto.ServiceType,
                ProductType = createProductDto.ProductType,
                EmailType = createProductDto.EmailType,
                SellerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsFeatured = createProductDto.IsFeatured
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Tạo ProductStats mặc định
            var productStats = new ProductStats
            {
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductStats.Add(productStats);
            await _context.SaveChangesAsync();

            // Load seller và stats để trả về thông tin đầy đủ
            await _context.Entry(product)
                .Reference(p => p.Seller)
                .LoadAsync();
            
            await _context.Entry(product)
                .Reference(p => p.Stats)
                .LoadAsync();

            // Lấy stats (sẽ có vì vừa tạo)
            var stats = product.Stats ?? new ProductStats { ProductId = product.Id };
            var complaintPercentage = stats.TotalSales > 0 
                ? Math.Round((decimal)stats.TotalComplaints / stats.TotalSales * 100, 2) 
                : 0;

            // Trả về response format giống GetProduct để tránh circular reference
            return CreatedAtAction(
                nameof(GetProduct), 
                new { id = product.Id }, 
                new
                {
                    product.Id,
                    product.ProductCode,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.DiscountPrice,
                    product.Stock,
                    product.ImageUrl,
                    product.ImageUrls,
                    product.Category,
                    product.ServiceType,
                    product.ProductType,
                    product.EmailType,
                    Rating = stats.Rating,
                    TotalRatings = stats.TotalRatings,
                    TotalSales = stats.TotalSales,
                    TotalComplaints = stats.TotalComplaints,
                    complaintPercentage,
                    TotalViews = stats.TotalViews,
                    TotalFavorites = stats.TotalFavorites,
                    product.IsFeatured,
                    options = new List<object>(),
                    Seller = new
                    {
                        product.Seller.Id,
                        product.Seller.Username,
                        product.Seller.Email,
                        product.Seller.ShopName,
                        product.Seller.ShopDescription,
                        product.Seller.ShopAvatar,
                        product.Seller.Telegram,
                        product.Seller.Rating,
                        product.Seller.TotalRatings,
                        product.Seller.TotalSales,
                        product.Seller.TotalComplaints,
                        product.Seller.IsVerified
                    },
                    product.CreatedAt,
                    product.UpdatedAt
                }
            );
        }

        [Authorize(Roles = "seller")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc bạn không có quyền chỉnh sửa" });
            }

            product.Name = updateProductDto.Name;
            product.Description = updateProductDto.Description;
            product.Price = updateProductDto.Price;
            product.DiscountPrice = updateProductDto.DiscountPrice;
            product.Stock = updateProductDto.Stock;
            product.ImageUrl = updateProductDto.ImageUrl;
            product.ImageUrls = updateProductDto.ImageUrls;
            product.Category = updateProductDto.Category;
            product.ServiceType = updateProductDto.ServiceType;
            product.ProductType = updateProductDto.ProductType;
            product.EmailType = updateProductDto.EmailType;
            product.IsFeatured = updateProductDto.IsFeatured;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [Authorize(Roles = "seller")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId);

                if (product == null)
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại hoặc bạn không có quyền xóa" });
                }

                _logger.LogInformation("Deleting product {ProductId} for seller {SellerId}. Current IsActive: {IsActive}", 
                    product.Id, userId, product.IsActive);

                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                
                var changes = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product {ProductId} deleted successfully. Changes saved: {Changes}. New IsActive: {IsActive}", 
                    product.Id, changes, product.IsActive);

                // Verify deletion
                var verifyProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (verifyProduct != null)
                {
                    _logger.LogWarning("Product {ProductId} still exists after deletion. IsActive: {IsActive}", 
                        verifyProduct.Id, verifyProduct.IsActive);
                }

                return Ok(new { message = "Sản phẩm đã được xóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm", error = ex.Message });
            }
        }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageUrls { get; set; }
        public string? Category { get; set; } // "Telegram", "TikTok", "Facebook", etc.
        public string? ServiceType { get; set; } // "Followers", "Likes", "Views", etc.
        public string? ProductType { get; set; } // "Tài khoản", "Phần mềm", "Cái khác"
        public string? EmailType { get; set; } // "Gmail", "Hotmail", "Outlookmail", "Rumail", "Yahoomail", "Other"
        public bool IsFeatured { get; set; } = false;
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageUrls { get; set; }
        public string? Category { get; set; }
        public string? ServiceType { get; set; }
        public string? ProductType { get; set; } // "Tài khoản", "Phần mềm", "Cái khác"
        public string? EmailType { get; set; } // "Gmail", "Hotmail", "Outlookmail", "Rumail", "Yahoomail", "Other"
        public bool IsFeatured { get; set; } = false;
    }
}

