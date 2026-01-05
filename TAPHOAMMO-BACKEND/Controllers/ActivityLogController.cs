using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models.DTOs;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityLogController> _logger;

        public ActivityLogController(ApplicationDbContext context, ILogger<ActivityLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy nhật ký hoạt động của user hiện tại
        /// </summary>
        /// <param name="operation">Lọc theo operation (ví dụ: "deposit", "login", "register")</param>
        /// <param name="startDate">Ngày bắt đầu (format: yyyy-MM-dd)</param>
        /// <param name="endDate">Ngày kết thúc (format: yyyy-MM-dd)</param>
        /// <param name="page">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Số lượng mỗi trang (mặc định: 20)</param>
        [HttpGet("my-activities")]
        public async Task<IActionResult> GetMyActivities(
            [FromQuery] string? operation = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            return await GetActivities(userId, operation, startDate, endDate, page, pageSize);
        }

        /// <summary>
        /// Admin: Lấy nhật ký hoạt động của tất cả users
        /// </summary>
        /// <param name="userId">Lọc theo userId (optional)</param>
        /// <param name="operation">Lọc theo operation (ví dụ: "deposit", "login", "register")</param>
        /// <param name="startDate">Ngày bắt đầu (format: yyyy-MM-dd)</param>
        /// <param name="endDate">Ngày kết thúc (format: yyyy-MM-dd)</param>
        /// <param name="page">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Số lượng mỗi trang (mặc định: 20)</param>
        [HttpGet("all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllActivities(
            [FromQuery] int? userId = null,
            [FromQuery] string? operation = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await GetActivities(userId, operation, startDate, endDate, page, pageSize);
        }

        /// <summary>
        /// Helper method để lấy danh sách activity logs
        /// </summary>
        private async Task<IActionResult> GetActivities(
            int? userId,
            string? operation,
            string? startDate,
            string? endDate,
            int page,
            int pageSize)
        {
            var query = _context.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            // Lọc theo userId
            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            // Lọc theo operation
            if (!string.IsNullOrEmpty(operation))
            {
                query = query.Where(a => a.Operation.ToLower() == operation.ToLower());
            }

            // Lọc theo ngày
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(a => a.Timestamp >= start.ToUniversalTime());
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                // Thêm 1 ngày để bao gồm cả ngày cuối
                query = query.Where(a => a.Timestamp < end.AddDays(1).ToUniversalTime());
            }

            var totalCount = await query.CountAsync();

            var activityLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Username = a.User != null ? a.User.Username : null,
                    FullName = a.User != null ? a.User.FullName : null,
                    Action = a.Action,
                    Operation = a.Operation,
                    IPAddress = a.IPAddress,
                    Timestamp = a.Timestamp,
                    Description = a.Description,
                    Metadata = a.Metadata,
                    UserAgent = a.UserAgent
                })
                .ToListAsync();

            return Ok(new ActivityLogListDto
            {
                ActivityLogs = activityLogs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Lấy thống kê hoạt động của user hiện tại
        /// </summary>
        [HttpGet("my-stats")]
        public async Task<IActionResult> GetMyStats(
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var query = _context.ActivityLogs
                .Where(a => a.UserId == userId)
                .AsQueryable();

            // Lọc theo ngày
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(a => a.Timestamp >= start.ToUniversalTime());
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                query = query.Where(a => a.Timestamp < end.AddDays(1).ToUniversalTime());
            }

            var stats = await query
                .GroupBy(a => a.Operation)
                .Select(g => new
                {
                    Operation = g.Key,
                    Count = g.Count(),
                    LastActivity = g.Max(a => a.Timestamp)
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

            var totalActivities = await query.CountAsync();

            return Ok(new
            {
                TotalActivities = totalActivities,
                Operations = stats
            });
        }
    }
}

