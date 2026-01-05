namespace TAPHOAMMO_BACKEND.Models.DTOs
{
    /// <summary>
    /// DTO cho Activity Log response
    /// </summary>
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }
        public string? Metadata { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// DTO cho danh sách Activity Log với phân trang
    /// </summary>
    public class ActivityLogListDto
    {
        public List<ActivityLogDto> ActivityLogs { get; set; } = new List<ActivityLogDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// DTO để tạo Activity Log mới (cho internal use)
    /// </summary>
    public class CreateActivityLogDto
    {
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string? IPAddress { get; set; }
        public string? Description { get; set; }
        public string? Metadata { get; set; }
        public string? UserAgent { get; set; }
    }
}

