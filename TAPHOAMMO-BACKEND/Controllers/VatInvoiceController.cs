using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VatInvoiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VatInvoiceController> _logger;

        public VatInvoiceController(ApplicationDbContext context, ILogger<VatInvoiceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách thông tin hóa đơn VAT của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVatInvoiceInfos()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfos = await _context.VatInvoiceInfos
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.IsDefault)
                .ThenByDescending(v => v.CreatedAt)
                .Select(v => new VatInvoiceInfoDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    CustomerType = (int)v.CustomerType,
                    CustomerTypeName = v.CustomerType == CustomerType.Individual ? "Cá nhân" : "Công ty",
                    FullName = v.FullName,
                    TaxCode = v.TaxCode,
                    CCCD = v.CCCD,
                    Address = v.Address,
                    IsDefault = v.IsDefault,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Lấy danh sách thông tin hóa đơn VAT thành công",
                data = vatInvoiceInfos
            });
        }

        /// <summary>
        /// Lấy thông tin hóa đơn VAT mặc định của user
        /// </summary>
        [HttpGet("default")]
        public async Task<IActionResult> GetDefaultVatInvoiceInfo()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfo = await _context.VatInvoiceInfos
                .Where(v => v.UserId == userId && v.IsDefault)
                .FirstOrDefaultAsync();

            if (vatInvoiceInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin hóa đơn VAT mặc định"
                });
            }

            var dto = new VatInvoiceInfoDto
            {
                Id = vatInvoiceInfo.Id,
                UserId = vatInvoiceInfo.UserId,
                CustomerType = (int)vatInvoiceInfo.CustomerType,
                CustomerTypeName = vatInvoiceInfo.CustomerType == CustomerType.Individual ? "Cá nhân" : "Công ty",
                FullName = vatInvoiceInfo.FullName,
                TaxCode = vatInvoiceInfo.TaxCode,
                CCCD = vatInvoiceInfo.CCCD,
                Address = vatInvoiceInfo.Address,
                IsDefault = vatInvoiceInfo.IsDefault,
                CreatedAt = vatInvoiceInfo.CreatedAt,
                UpdatedAt = vatInvoiceInfo.UpdatedAt
            };

            return Ok(new
            {
                success = true,
                message = "Lấy thông tin hóa đơn VAT mặc định thành công",
                data = dto
            });
        }

        /// <summary>
        /// Lấy thông tin hóa đơn VAT theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVatInvoiceInfo(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfo = await _context.VatInvoiceInfos
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vatInvoiceInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin hóa đơn VAT"
                });
            }

            var dto = new VatInvoiceInfoDto
            {
                Id = vatInvoiceInfo.Id,
                UserId = vatInvoiceInfo.UserId,
                CustomerType = (int)vatInvoiceInfo.CustomerType,
                CustomerTypeName = vatInvoiceInfo.CustomerType == CustomerType.Individual ? "Cá nhân" : "Công ty",
                FullName = vatInvoiceInfo.FullName,
                TaxCode = vatInvoiceInfo.TaxCode,
                CCCD = vatInvoiceInfo.CCCD,
                Address = vatInvoiceInfo.Address,
                IsDefault = vatInvoiceInfo.IsDefault,
                CreatedAt = vatInvoiceInfo.CreatedAt,
                UpdatedAt = vatInvoiceInfo.UpdatedAt
            };

            return Ok(new
            {
                success = true,
                message = "Lấy thông tin hóa đơn VAT thành công",
                data = dto
            });
        }

        /// <summary>
        /// Tạo thông tin hóa đơn VAT mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateVatInvoiceInfo([FromBody] CreateVatInvoiceInfoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState
                });
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Validate CustomerType
            if (dto.CustomerType != 1 && dto.CustomerType != 2)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Loại khách hàng không hợp lệ. Chỉ chấp nhận 1 (Cá nhân) hoặc 2 (Công ty)"
                });
            }

            // Nếu đánh dấu làm mặc định, bỏ đánh dấu mặc định của các bản ghi khác
            if (dto.IsDefault)
            {
                var existingDefaults = await _context.VatInvoiceInfos
                    .Where(v => v.UserId == userId && v.IsDefault)
                    .ToListAsync();

                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }

            var vatInvoiceInfo = new VatInvoiceInfo
            {
                UserId = userId,
                CustomerType = (CustomerType)dto.CustomerType,
                FullName = dto.FullName.Trim(),
                TaxCode = string.IsNullOrWhiteSpace(dto.TaxCode) ? null : dto.TaxCode.Trim(),
                CCCD = string.IsNullOrWhiteSpace(dto.CCCD) ? null : dto.CCCD.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VatInvoiceInfos.Add(vatInvoiceInfo);
            await _context.SaveChangesAsync();

            var responseDto = new VatInvoiceInfoDto
            {
                Id = vatInvoiceInfo.Id,
                UserId = vatInvoiceInfo.UserId,
                CustomerType = (int)vatInvoiceInfo.CustomerType,
                CustomerTypeName = vatInvoiceInfo.CustomerType == CustomerType.Individual ? "Cá nhân" : "Công ty",
                FullName = vatInvoiceInfo.FullName,
                TaxCode = vatInvoiceInfo.TaxCode,
                CCCD = vatInvoiceInfo.CCCD,
                Address = vatInvoiceInfo.Address,
                IsDefault = vatInvoiceInfo.IsDefault,
                CreatedAt = vatInvoiceInfo.CreatedAt,
                UpdatedAt = vatInvoiceInfo.UpdatedAt
            };

            return CreatedAtAction(nameof(GetVatInvoiceInfo), new { id = vatInvoiceInfo.Id }, new
            {
                success = true,
                message = "Tạo thông tin hóa đơn VAT thành công",
                data = responseDto
            });
        }

        /// <summary>
        /// Cập nhật thông tin hóa đơn VAT
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVatInvoiceInfo(int id, [FromBody] UpdateVatInvoiceInfoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState
                });
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfo = await _context.VatInvoiceInfos
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vatInvoiceInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin hóa đơn VAT"
                });
            }

            // Validate CustomerType
            if (dto.CustomerType != 1 && dto.CustomerType != 2)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Loại khách hàng không hợp lệ. Chỉ chấp nhận 1 (Cá nhân) hoặc 2 (Công ty)"
                });
            }

            // Nếu đánh dấu làm mặc định, bỏ đánh dấu mặc định của các bản ghi khác
            if (dto.IsDefault && !vatInvoiceInfo.IsDefault)
            {
                var existingDefaults = await _context.VatInvoiceInfos
                    .Where(v => v.UserId == userId && v.IsDefault && v.Id != id)
                    .ToListAsync();

                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Cập nhật thông tin
            vatInvoiceInfo.CustomerType = (CustomerType)dto.CustomerType;
            vatInvoiceInfo.FullName = dto.FullName.Trim();
            vatInvoiceInfo.TaxCode = string.IsNullOrWhiteSpace(dto.TaxCode) ? null : dto.TaxCode.Trim();
            vatInvoiceInfo.CCCD = string.IsNullOrWhiteSpace(dto.CCCD) ? null : dto.CCCD.Trim();
            vatInvoiceInfo.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            vatInvoiceInfo.IsDefault = dto.IsDefault;
            vatInvoiceInfo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var responseDto = new VatInvoiceInfoDto
            {
                Id = vatInvoiceInfo.Id,
                UserId = vatInvoiceInfo.UserId,
                CustomerType = (int)vatInvoiceInfo.CustomerType,
                CustomerTypeName = vatInvoiceInfo.CustomerType == CustomerType.Individual ? "Cá nhân" : "Công ty",
                FullName = vatInvoiceInfo.FullName,
                TaxCode = vatInvoiceInfo.TaxCode,
                CCCD = vatInvoiceInfo.CCCD,
                Address = vatInvoiceInfo.Address,
                IsDefault = vatInvoiceInfo.IsDefault,
                CreatedAt = vatInvoiceInfo.CreatedAt,
                UpdatedAt = vatInvoiceInfo.UpdatedAt
            };

            return Ok(new
            {
                success = true,
                message = "Cập nhật thông tin hóa đơn VAT thành công",
                data = responseDto
            });
        }

        /// <summary>
        /// Xóa thông tin hóa đơn VAT
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVatInvoiceInfo(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfo = await _context.VatInvoiceInfos
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vatInvoiceInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin hóa đơn VAT"
                });
            }

            _context.VatInvoiceInfos.Remove(vatInvoiceInfo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa thông tin hóa đơn VAT thành công"
            });
        }

        /// <summary>
        /// Đặt thông tin hóa đơn VAT làm mặc định
        /// </summary>
        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultVatInvoiceInfo(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var vatInvoiceInfo = await _context.VatInvoiceInfos
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vatInvoiceInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin hóa đơn VAT"
                });
            }

            // Bỏ đánh dấu mặc định của các bản ghi khác
            var existingDefaults = await _context.VatInvoiceInfos
                .Where(v => v.UserId == userId && v.IsDefault && v.Id != id)
                .ToListAsync();

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = DateTime.UtcNow;
            }

            // Đặt làm mặc định
            vatInvoiceInfo.IsDefault = true;
            vatInvoiceInfo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đặt thông tin hóa đơn VAT làm mặc định thành công"
            });
        }
    }
}

