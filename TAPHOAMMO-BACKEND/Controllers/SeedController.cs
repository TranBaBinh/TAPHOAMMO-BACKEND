using Microsoft.AspNetCore.Mvc;
using TAPHOAMMO_BACKEND.Services;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly DataSeeder _dataSeeder;
        private readonly ILogger<SeedController> _logger;

        public SeedController(DataSeeder dataSeeder, ILogger<SeedController> logger)
        {
            _dataSeeder = dataSeeder;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await _dataSeeder.SeedAsync();
                return Ok(new { message = "Dữ liệu mẫu đã được thêm vào database thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi seed data");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi seed data", error = ex.Message });
            }
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetAndSeedData()
        {
            try
            {
                await _dataSeeder.ResetAndSeedAsync();
                return Ok(new { message = "Database đã được reset và seed lại dữ liệu mẫu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset và seed data");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi reset và seed data", error = ex.Message });
            }
        }
    }
}

