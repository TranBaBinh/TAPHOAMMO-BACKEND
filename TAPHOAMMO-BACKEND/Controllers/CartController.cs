using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy giỏ hàng của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                    .ThenInclude(p => p.Seller)
                .Include(c => c.ProductOption)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var items = cartItems.Select(c => new
            {
                c.Id,
                Product = new
                {
                    c.Product.Id,
                    c.Product.Name,
                    c.Product.ImageUrl,
                    Seller = new
                    {
                        c.Product.Seller.Id,
                        c.Product.Seller.Username,
                        c.Product.Seller.ShopName
                    }
                },
                ProductOption = c.ProductOption != null ? new
                {
                    c.ProductOption.Id,
                    c.ProductOption.Name,
                    c.ProductOption.Description
                } : null,
                c.Quantity,
                c.UnitPrice,
                c.DiscountPrice,
                TotalPrice = (c.DiscountPrice ?? c.UnitPrice) * c.Quantity,
                c.CreatedAt,
                c.UpdatedAt
            }).ToList();

            var totalAmount = items.Sum(i => i.TotalPrice);
            var totalItems = items.Sum(i => i.Quantity);

            return Ok(new
            {
                items,
                totalAmount,
                totalItems,
                itemCount = items.Count
            });
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto addToCartDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Kiểm tra product tồn tại
            var product = await _context.Products
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == addToCartDto.ProductId && p.IsActive);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            // Xác định giá
            decimal unitPrice;
            decimal? discountPrice = null;
            int? productOptionId = null;

            if (addToCartDto.ProductOptionId.HasValue)
            {
                // Nếu có chọn option
                var option = product.Options.FirstOrDefault(o => o.Id == addToCartDto.ProductOptionId.Value && o.IsActive);
                if (option == null)
                {
                    return BadRequest(new { message = "Option không tồn tại" });
                }

                if (option.Stock < addToCartDto.Quantity)
                {
                    return BadRequest(new { message = "Số lượng không đủ" });
                }

                unitPrice = option.Price;
                discountPrice = option.DiscountPrice;
                productOptionId = option.Id;
            }
            else
            {
                // Nếu không chọn option, dùng giá của product
                unitPrice = product.DiscountPrice ?? product.Price;
                discountPrice = product.DiscountPrice;
                
                if (product.Stock < addToCartDto.Quantity)
                {
                    return BadRequest(new { message = "Số lượng không đủ" });
                }
            }

            // Kiểm tra item đã có trong giỏ chưa
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         c.ProductId == addToCartDto.ProductId &&
                                         c.ProductOptionId == productOptionId);

            if (existingItem != null)
            {
                // Cập nhật số lượng
                existingItem.Quantity += addToCartDto.Quantity;
                existingItem.UnitPrice = unitPrice; // Cập nhật giá mới
                existingItem.DiscountPrice = discountPrice;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Tạo mới
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = addToCartDto.ProductId,
                    ProductOptionId = productOptionId,
                    Quantity = addToCartDto.Quantity,
                    UnitPrice = unitPrice,
                    DiscountPrice = discountPrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm vào giỏ hàng" });
        }

        /// <summary>
        /// Cập nhật số lượng item trong giỏ hàng
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.ProductOption)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Item không tồn tại trong giỏ hàng" });
            }

            // Kiểm tra stock
            if (cartItem.ProductOptionId.HasValue)
            {
                var option = await _context.ProductOptions.FindAsync(cartItem.ProductOptionId);
                if (option != null && option.Stock < updateDto.Quantity)
                {
                    return BadRequest(new { message = "Số lượng không đủ" });
                }
            }
            else
            {
                if (cartItem.Product.Stock < updateDto.Quantity)
                {
                    return BadRequest(new { message = "Số lượng không đủ" });
                }
            }

            cartItem.Quantity = updateDto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật giỏ hàng" });
        }

        /// <summary>
        /// Xóa item khỏi giỏ hàng
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveCartItem(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Item không tồn tại trong giỏ hàng" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khỏi giỏ hàng" });
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
        }
    }
}

