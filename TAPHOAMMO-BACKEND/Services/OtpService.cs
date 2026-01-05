using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;

namespace TAPHOAMMO_BACKEND.Services
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string email, string purpose = "login");
        Task<bool> VerifyOtpAsync(string email, string code, string purpose = "login");
        Task<bool> SendOtpEmailAsync(string email, string code, string purpose = "login");
    }

    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            ApplicationDbContext context, 
            IEmailService emailService,
            ILogger<OtpService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(string email, string purpose = "login")
        {
            try
            {
                // Xóa các mã OTP cũ của email này (sử dụng raw SQL để tránh concurrency issue)
                try
                {
                    var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM OtpCodes WHERE Email = {0} AND Purpose = {1} AND IsUsed = 0",
                        email, purpose);
                    
                    if (deletedCount > 0)
                    {
                        _logger.LogInformation($"Đã xóa {deletedCount} mã OTP cũ cho {email}");
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, $"Lỗi khi xóa OTP cũ (có thể đã bị xóa): {deleteEx.Message}");
                    // Bỏ qua lỗi, tiếp tục tạo OTP mới
                }

                // Tạo mã OTP 6 số ngẫu nhiên
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();

                // Lưu mã OTP vào database
                var otpCode = new OtpCode
                {
                    Email = email,
                    Code = code,
                    Purpose = purpose,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                _context.OtpCodes.Add(otpCode);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã tạo mã OTP cho {email}, purpose: {purpose}");

                // Gửi email OTP - BẮT BUỘC phải gửi được
                try
                {
                    var emailSent = await SendOtpEmailAsync(email, code, purpose);
                    
                    if (!emailSent)
                    {
                        _logger.LogError($"KHÔNG THỂ gửi email OTP đến {email}. Mã OTP đã được tạo nhưng không gửi được email!");
                        // Xóa OTP đã tạo vì không gửi được email (sử dụng raw SQL)
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "DELETE FROM OtpCodes WHERE Id = {0}",
                                otpCode.Id);
                            _logger.LogWarning($"Đã xóa mã OTP vì không gửi được email");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, $"Không thể xóa OTP sau khi gửi email thất bại: {deleteEx.Message}");
                        }
                        
                        throw new Exception($"Không thể gửi email OTP đến {email}. EmailService trả về false.");
                    }
                    
                    _logger.LogInformation($"Đã gửi email OTP thành công đến {email}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Lỗi khi gửi email OTP đến {email}");
                    // Xóa OTP đã tạo vì không gửi được email (sử dụng raw SQL)
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM OtpCodes WHERE Id = {0}",
                            otpCode.Id);
                        _logger.LogWarning($"Đã xóa mã OTP vì không gửi được email");
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, $"Không thể xóa OTP sau khi gửi email thất bại: {deleteEx.Message}");
                    }
                    
                    // Throw lại exception với thông tin chi tiết
                    throw new Exception($"Không thể gửi email OTP đến {email}. {emailEx.Message}", emailEx);
                }

                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo OTP cho {email}, purpose: {purpose}");
                throw;
            }
        }

        public async Task<bool> VerifyOtpAsync(string email, string code, string purpose = "login")
        {
            var otp = await _context.OtpCodes
                .Where(o => o.Email == email 
                    && o.Code == code 
                    && o.Purpose == purpose 
                    && !o.IsUsed
                    && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return false; // Mã OTP không hợp lệ hoặc đã hết hạn
            }

            // Đánh dấu mã đã sử dụng
            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SendOtpEmailAsync(string email, string code, string purpose = "login")
        {
            try
            {
                // Sử dụng EmailService để gửi email OTP
                // Nếu chưa cấu hình SMTP, EmailService sẽ tự động fallback về console
                var result = await _emailService.SendOtpEmailAsync(email, code, purpose);
                
                if (!result)
                {
                    _logger.LogWarning($"EmailService trả về false khi gửi OTP đến {email}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email OTP đến {email}");
                // Vẫn trả về false thay vì throw exception để không làm gián đoạn flow
                return false;
            }
        }
    }
}

