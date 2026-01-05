using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace TAPHOAMMO_BACKEND.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose = "login");
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["Email:FromName"] ?? "TAPHOAMMO";

                _logger.LogInformation($"=== BẮT ĐẦU GỬI EMAIL ===");
                _logger.LogInformation($"To: {toEmail}");
                _logger.LogInformation($"From: {fromEmail} ({fromName})");
                _logger.LogInformation($"SMTP Host: {smtpHost}");
                _logger.LogInformation($"SMTP Port: {smtpPort}");
                _logger.LogInformation($"SMTP Username: {smtpUsername}");
                _logger.LogInformation($"SMTP Password: {(string.IsNullOrEmpty(smtpPassword) ? "EMPTY" : "***")}");

                // Nếu chưa cấu hình email, chỉ log ra console (development mode)
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
                {
                    _logger.LogWarning("Email service chưa được cấu hình. Mã OTP sẽ được in ra console.");
                    Console.WriteLine($"\n=== EMAIL (Chưa cấu hình SMTP) ===");
                    Console.WriteLine($"To: {toEmail}");
                    Console.WriteLine($"Subject: {subject}");
                    Console.WriteLine($"Body: {body}");
                    Console.WriteLine($"===================================\n");
                    return false; // Trả về false thay vì true để báo lỗi
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                if (isHtml)
                {
                    message.Body = new TextPart("html")
                    {
                        Text = body
                    };
                }
                else
                {
                    message.Body = new TextPart("plain")
                    {
                        Text = body
                    };
                }

                using (var client = new SmtpClient())
                {
                    // Xác định loại kết nối bảo mật dựa trên PORT
                    // Port 465 = SSL/TLS ngay từ đầu (SslOnConnect)
                    // Port 587 = StartTLS
                    SecureSocketOptions socketOptions;
                    
                    if (smtpPort == 465)
                    {
                        // Port 465 yêu cầu SSL ngay từ đầu
                        socketOptions = SecureSocketOptions.SslOnConnect;
                        _logger.LogInformation($"Đang kết nối đến SMTP với SSL (port 465): {smtpHost}:{smtpPort}");
                    }
                    else
                    {
                        // Port 587 hoặc các port khác dùng StartTLS
                        socketOptions = SecureSocketOptions.StartTls;
                        _logger.LogInformation($"Đang kết nối đến SMTP với StartTLS (port {smtpPort}): {smtpHost}:{smtpPort}");
                    }

                    try
                    {
                        await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                        _logger.LogInformation("Đã kết nối SMTP thành công");
                        
                        await client.AuthenticateAsync(smtpUsername, smtpPassword);
                        _logger.LogInformation("Đã xác thực SMTP thành công");
                        
                        await client.SendAsync(message);
                        _logger.LogInformation($"Đã gửi email thành công đến {toEmail}");
                        
                        await client.DisconnectAsync(true);
                        _logger.LogInformation("Đã ngắt kết nối SMTP");
                    }
                    catch (Exception connectEx)
                    {
                        // Nếu StartTLS thất bại với Gmail, thử SSL (port 465) với client mới
                        if (smtpHost.Contains("gmail.com") && socketOptions == SecureSocketOptions.StartTls && smtpPort == 587)
                        {
                            _logger.LogWarning($"StartTLS thất bại, thử SSL với port 465: {connectEx.Message}");
                            _logger.LogWarning($"Chi tiết lỗi: {connectEx}");
                            
                            // Tạo client mới cho SSL
                            using (var sslClient = new SmtpClient())
                            {
                                try
                                {
                                    _logger.LogInformation("Đang thử kết nối Gmail qua SSL (port 465)...");
                                    await sslClient.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                                    _logger.LogInformation("Đã kết nối Gmail qua SSL thành công");
                                    
                                    await sslClient.AuthenticateAsync(smtpUsername, smtpPassword);
                                    _logger.LogInformation("Đã xác thực Gmail qua SSL thành công");
                                    
                                    await sslClient.SendAsync(message);
                                    _logger.LogInformation($"Đã gửi email thành công đến {toEmail} (qua SSL)");
                                    
                                    await sslClient.DisconnectAsync(true);
                                    _logger.LogInformation("Đã ngắt kết nối Gmail SSL");
                                }
                                catch (Exception sslEx)
                                {
                                    _logger.LogError(sslEx, $"Lỗi khi thử SSL với Gmail: {sslEx.Message}");
                                    _logger.LogError($"Chi tiết lỗi SSL: {sslEx}");
                                    throw; // Ném lại exception để được catch ở ngoài
                                }
                            }
                        }
                        else
                        {
                            _logger.LogError(connectEx, $"Lỗi kết nối SMTP: {connectEx.Message}");
                            _logger.LogError($"Chi tiết lỗi: {connectEx}");
                            throw; // Ném lại exception
                        }
                    }
                }

                _logger.LogInformation($"Email đã được gửi thành công đến {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email đến {toEmail}");
                _logger.LogError($"Exception Type: {ex.GetType().Name}");
                _logger.LogError($"Exception Message: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                // Fallback: In ra console nếu gửi email thất bại
                Console.WriteLine($"\n=== EMAIL (Gửi thất bại - Fallback) ===");
                Console.WriteLine($"To: {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"======================================\n");
                
                // Throw lại exception với thông tin chi tiết hơn
                throw new Exception($"Không thể gửi email đến {toEmail}. Lỗi: {ex.Message}", ex);
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose = "login")
        {
            var purposeText = purpose == "register" ? "đăng ký" : "đăng nhập";
            var subject = $"Mã OTP {purposeText} TAPHOAMMO";
            
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; padding: 20px; background-color: white; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>TAPHOAMMO</h1>
        </div>
        <div class='content'>
            <h2>Mã OTP {purposeText}</h2>
            <p>Xin chào,</p>
            <p>Bạn đã yêu cầu mã OTP để {purposeText} tài khoản TAPHOAMMO.</p>
            
            <div class='otp-code'>
                {otpCode}
            </div>
            
            <p>Mã OTP này có hiệu lực trong <strong>5 phút</strong>.</p>
            
            <div class='warning'>
                <strong>⚠️ Lưu ý:</strong> Không chia sẻ mã OTP này với bất kỳ ai. Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
            </div>
            
            <p>Trân trọng,<br>Đội ngũ TAPHOAMMO</p>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p>&copy; 2024 TAPHOAMMO. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            var plainBody = $@"
TAPHOAMMO - Mã OTP {purposeText}

Xin chào,

Bạn đã yêu cầu mã OTP để {purposeText} tài khoản TAPHOAMMO.

Mã OTP của bạn là: {otpCode}

Mã OTP này có hiệu lực trong 5 phút.

⚠️ Lưu ý: Không chia sẻ mã OTP này với bất kỳ ai. Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.

Trân trọng,
Đội ngũ TAPHOAMMO

---
Email này được gửi tự động, vui lòng không trả lời.
© 2024 TAPHOAMMO. All rights reserved.
";

            return await SendEmailAsync(toEmail, subject, htmlBody, true);
        }
    }
}

