using Microsoft.AspNetCore.Http;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;
using System.Text.Json;

namespace TAPHOAMMO_BACKEND.Services
{
    /// <summary>
    /// Service để log các hoạt động của user và hệ thống
    /// </summary>
    public interface IActivityLogService
    {
        Task LogActivityAsync(CreateActivityLogDto dto);
        Task LogActivityAsync(int? userId, string action, string operation, string? ipAddress = null, string? description = null, object? metadata = null, string? userAgent = null);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Log activity từ DTO
        /// </summary>
        public async Task LogActivityAsync(CreateActivityLogDto dto)
        {
            var activityLog = new ActivityLog
            {
                UserId = dto.UserId,
                Action = dto.Action,
                Operation = dto.Operation,
                IPAddress = dto.IPAddress ?? GetClientIPAddress(),
                Description = dto.Description,
                Metadata = dto.Metadata,
                UserAgent = dto.UserAgent ?? GetUserAgent(),
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Log activity với các tham số riêng lẻ (convenience method)
        /// </summary>
        public async Task LogActivityAsync(int? userId, string action, string operation, string? ipAddress = null, string? description = null, object? metadata = null, string? userAgent = null)
        {
            var dto = new CreateActivityLogDto
            {
                UserId = userId,
                Action = action,
                Operation = operation,
                IPAddress = ipAddress ?? GetClientIPAddress(),
                Description = description,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                UserAgent = userAgent ?? GetUserAgent()
            };

            await LogActivityAsync(dto);
        }

        /// <summary>
        /// Lấy IP address từ HttpContext
        /// </summary>
        private string? GetClientIPAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Kiểm tra X-Forwarded-For header (khi có reverse proxy)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            // Kiểm tra X-Real-IP header
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Lấy từ RemoteIpAddress
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Lấy User Agent từ HttpContext
        /// </summary>
        private string? GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }
    }
}

