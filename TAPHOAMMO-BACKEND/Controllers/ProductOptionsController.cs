using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/options")]
    public class ProductOptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductOptionsController> _logger;

        public ProductOptionsController(ApplicationDbContext context, ILogger<ProductOptionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả các tùy chọn của một sản phẩm
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductOptions(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            var options = await _context.ProductOptions
                .Where(po => po.ProductId == productId && po.IsActive)
                .OrderBy(po => po.SortOrder)
                .ThenBy(po => po.Price)
                .ToListAsync();

            return Ok(new { productId, options });
        }

        /// <summary>
        /// Lấy chi tiết một tùy chọn sản phẩm
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductOption(int productId, int id)
        {
            var option = await _context.ProductOptions
                .FirstOrDefaultAsync(po => po.Id == id && po.ProductId == productId);

            if (option == null)
            {
                return NotFound(new { message = "Tùy chọn sản phẩm không tồn tại" });
            }

            return Ok(option);
        }

        /// <summary>
        /// Tạo tùy chọn mới cho sản phẩm (chỉ seller)
        /// </summary>
        [Authorize(Roles = "seller")]
        [HttpPost]
        public async Task<IActionResult> CreateProductOption(int productId, [FromBody] CreateProductOptionDto createProductOptionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == userId);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc bạn không có quyền tạo tùy chọn cho sản phẩm này" });
            }

            var productOption = new ProductOption
            {
                ProductId = productId,
                Name = createProductOptionDto.Name,
                Description = createProductOptionDto.Description,
                Price = createProductOptionDto.Price,
                DiscountPrice = createProductOptionDto.DiscountPrice,
                Stock = createProductOptionDto.Stock,
                SortOrder = createProductOptionDto.SortOrder,
                IsActive = createProductOptionDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProductOptions.Add(productOption);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductOption), new { productId, id = productOption.Id }, productOption);
        }

        /// <summary>
        /// Cập nhật tùy chọn sản phẩm (chỉ seller)
        /// </summary>
        [Authorize(Roles = "seller")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductOption(int productId, int id, [FromBody] UpdateProductOptionDto updateProductOptionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == userId);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc bạn không có quyền chỉnh sửa tùy chọn này" });
            }

            var productOption = await _context.ProductOptions.FirstOrDefaultAsync(po => po.Id == id && po.ProductId == productId);

            if (productOption == null)
            {
                return NotFound(new { message = "Tùy chọn sản phẩm không tồn tại" });
            }

            productOption.Name = updateProductOptionDto.Name;
            productOption.Description = updateProductOptionDto.Description;
            productOption.Price = updateProductOptionDto.Price;
            productOption.DiscountPrice = updateProductOptionDto.DiscountPrice;
            productOption.Stock = updateProductOptionDto.Stock;
            productOption.SortOrder = updateProductOptionDto.SortOrder;
            productOption.IsActive = updateProductOptionDto.IsActive;
            productOption.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(productOption);
        }

        /// <summary>
        /// Xóa tùy chọn sản phẩm (chỉ seller)
        /// </summary>
        [Authorize(Roles = "seller")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductOption(int productId, int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == userId);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc bạn không có quyền xóa tùy chọn này" });
            }

            var productOption = await _context.ProductOptions.FirstOrDefaultAsync(po => po.Id == id && po.ProductId == productId);

            if (productOption == null)
            {
                return NotFound(new { message = "Tùy chọn sản phẩm không tồn tại" });
            }

            _context.ProductOptions.Remove(productOption);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tùy chọn sản phẩm đã được xóa" });
        }
    }
}

