using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;
using TAPHOAMMO_BACKEND.Services;
using BCrypt.Net;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IActivityLogService _activityLogService;

        public AuthController(IAuthService authService, IOtpService otpService, ApplicationDbContext context, ILogger<AuthController> logger, IActivityLogService activityLogService)
        {
            _authService = authService;
            _otpService = otpService;
            _context = context;
            _logger = logger;
            _activityLogService = activityLogService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterWithOtpDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!registerDto.AgreeToTerms || !registerDto.AgreeToPolicy)
            {
                return BadRequest(new { message = "Bạn phải đồng ý với điều khoản và chính sách sử dụng" });
            }

            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _authService.CheckUserExistsAsync(registerDto.Email);
            if (emailExists)
            {
                return BadRequest(new { message = "Email đã được đăng ký" });
            }

            // Kiểm tra username đã tồn tại chưa
            var usernameExists = await _authService.CheckUsernameExistsAsync(registerDto.Username);
            if (usernameExists)
            {
                return BadRequest(new { message = "Tên tài khoản đã được sử dụng" });
            }

            // Xác thực mã OTP
            var otpValid = await _otpService.VerifyOtpAsync(registerDto.Email, registerDto.OtpCode, "register");
            if (!otpValid)
            {
                return BadRequest(new { message = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới." });
            }

            // Đăng ký tài khoản
            var registerData = new RegisterDto
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Password = registerDto.Password,
                ConfirmPassword = registerDto.ConfirmPassword,
                AgreeToTerms = registerDto.AgreeToTerms,
                AgreeToPolicy = registerDto.AgreeToPolicy,
                Role = registerDto.Role
            };

            var result = await _authService.RegisterAsync(registerData);

            if (result == null)
            {
                return BadRequest(new { message = "Đăng ký thất bại" });
            }

            // Log activity: Tạo tài khoản
            try
            {
                await _activityLogService.LogActivityAsync(
                    result.User.Id,
                    "Tạo tài khoản",
                    "register",
                    null,
                    $"Đăng ký tài khoản mới: {registerDto.Username}",
                    new { Username = registerDto.Username, Email = registerDto.Email, Role = registerDto.Role }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho register");
            }

            return Ok(result);
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto requestOtpDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Kiểm tra email có tồn tại không (chỉ cho login)
                if (requestOtpDto.Purpose == "login")
                {
                    var userExists = await _authService.CheckUserExistsAsync(requestOtpDto.Email);
                    if (!userExists)
                    {
                        return BadRequest(new { message = "Email chưa được đăng ký. Vui lòng đăng ký trước." });
                    }
                }

                // Generate và gửi OTP
                var code = await _otpService.GenerateOtpAsync(requestOtpDto.Email, requestOtpDto.Purpose);

                _logger.LogInformation($"OTP đã được tạo và gửi đến {requestOtpDto.Email} cho mục đích {requestOtpDto.Purpose}");

                return Ok(new { 
                    message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra email.",
                    email = requestOtpDto.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi OTP đến {requestOtpDto.Email}");
                
                // Lấy thông báo lỗi chi tiết
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                // Kiểm tra loại lỗi để trả về thông báo phù hợp
                if (errorMessage.Contains("Authentication") || errorMessage.Contains("535") || errorMessage.Contains("Invalid credentials"))
                {
                    return StatusCode(500, new { 
                        message = "Lỗi xác thực email. Vui lòng kiểm tra lại App Password của Gmail.",
                        error = "Authentication failed. Kiểm tra App Password có đúng không.",
                        details = errorMessage
                    });
                }
                else if (errorMessage.Contains("Connection") || errorMessage.Contains("timeout") || errorMessage.Contains("refused"))
                {
                    return StatusCode(500, new { 
                        message = "Không thể kết nối đến Gmail SMTP. Vui lòng kiểm tra kết nối internet hoặc firewall.",
                        error = "Connection failed",
                        details = errorMessage
                    });
                }
                else if (errorMessage.Contains("chưa được cấu hình"))
                {
                    return StatusCode(500, new { 
                        message = "Email service chưa được cấu hình đầy đủ.",
                        error = "Email configuration missing",
                        details = errorMessage
                    });
                }
                
                return StatusCode(500, new { 
                    message = "Có lỗi xảy ra khi gửi mã OTP. Vui lòng thử lại sau.",
                    error = errorMessage,
                    details = ex.ToString()
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto verifyOtpDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _otpService.VerifyOtpAsync(
                verifyOtpDto.Email, 
                verifyOtpDto.Code, 
                verifyOtpDto.Purpose
            );

            if (!isValid)
            {
                return BadRequest(new { message = "Mã OTP không đúng hoặc đã hết hạn" });
            }

            return Ok(new { 
                message = "Mã OTP hợp lệ",
                email = verifyOtpDto.Email
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginWithOtpDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Bước 1: Kiểm tra email và password
            var loginCheck = new LoginDto
            {
                Email = loginDto.Email,
                Password = loginDto.Password,
                RememberMe = loginDto.RememberMe
            };

            // Kiểm tra xem user có phải là Google-only user không (có GoogleId nhưng không có password)
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Email == loginCheck.Email);
            if (user != null && user.AuthInfo != null && 
                !string.IsNullOrEmpty(user.AuthInfo.GoogleId) && 
                string.IsNullOrEmpty(user.AuthInfo.PasswordHash))
            {
                return BadRequest(new { 
                    message = "Tài khoản này được đăng ký bằng Google. Vui lòng sử dụng chức năng 'Đăng nhập với Google'." 
                });
            }

            var userValid = await _authService.ValidateCredentialsAsync(loginCheck.Email, loginCheck.Password);
            if (!userValid)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
            }

            // Bước 2: Kiểm tra OTP
            var otpValid = await _otpService.VerifyOtpAsync(loginDto.Email, loginDto.OtpCode, "login");
            if (!otpValid)
            {
                return BadRequest(new { message = "Mã OTP không đúng hoặc đã hết hạn" });
            }

            // Bước 3: Đăng nhập thành công
            var result = await _authService.LoginAsync(loginCheck);

            if (result == null)
            {
                return Unauthorized(new { message = "Đăng nhập thất bại" });
            }

            // Log activity: Đăng nhập
            try
            {
                await _activityLogService.LogActivityAsync(
                    result.User.Id,
                    "Đăng nhập",
                    "login",
                    null,
                    $"Đăng nhập thành công",
                    new { Email = loginDto.Email, RememberMe = loginDto.RememberMe }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho login");
            }

            return Ok(result);
        }

        [HttpPost("login-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto googleLoginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginWithGoogleAsync(
                googleLoginDto.GoogleId,
                googleLoginDto.Email,
                googleLoginDto.Name
            );

            if (result == null)
            {
                return BadRequest(new { message = "Không thể đăng nhập với Google" });
            }

            // Log activity: Đăng nhập với Google
            try
            {
                await _activityLogService.LogActivityAsync(
                    result.User.Id,
                    "Đăng nhập với Google",
                    "login_google",
                    null,
                    $"Đăng nhập với Google: {googleLoginDto.Email}",
                    new { GoogleId = googleLoginDto.GoogleId, Email = googleLoginDto.Email }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho login_google");
            }

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (string.IsNullOrEmpty(refreshTokenDto.Token) || string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
            {
                return BadRequest(new { message = "Token và RefreshToken là bắt buộc" });
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken);

            if (result == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn" });
            }

            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            return Ok(new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CCCD = user.CCCD,
                Phone = user.Phone,
                FacebookLink = user.FacebookLink,
                ShopName = user.ShopName,
                IsVerified = user.IsVerified
            });
        }

        [Authorize]
        [HttpPost("become-seller")]
        public async Task<IActionResult> BecomeSeller([FromBody] BecomeSellerDto becomeSellerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Kiểm tra đã là seller chưa
            if (user.Role == "seller")
            {
                return BadRequest(new { message = "Bạn đã là seller rồi" });
            }

            // Kiểm tra email có khớp với tài khoản không
            if (user.Email.ToLower() != becomeSellerDto.Email.ToLower())
            {
                return BadRequest(new { message = "Email không khớp với tài khoản của bạn" });
            }

            // Kiểm tra CCCD đã được sử dụng chưa
            var cccdExists = await _context.Users.AnyAsync(u => u.CCCD == becomeSellerDto.CCCD && u.Id != userId);
            if (cccdExists)
            {
                return BadRequest(new { message = "Số CCCD này đã được sử dụng" });
            }

            // Cập nhật thông tin seller (chỉ dùng username có sẵn, không cần FullName)
            user.CCCD = becomeSellerDto.CCCD;
            user.Phone = becomeSellerDto.Phone;
            user.FacebookLink = becomeSellerDto.FacebookLink;
            user.Role = "seller";
            user.JoinDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate token mới với role = "seller" để frontend có thể sử dụng ngay
            var newToken = _authService.GenerateJwtToken(user, true);
            var newRefreshToken = _authService.GenerateRefreshToken();
            
            // Lưu refresh token mới vào AuthInfo
            var authInfo = await _context.UserAuthInfos.FirstOrDefaultAsync(a => a.UserId == userId);
            if (authInfo == null)
            {
                authInfo = new UserAuthInfo
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAuthInfos.Add(authInfo);
            }
            authInfo.RefreshToken = newRefreshToken;
            authInfo.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            authInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng ký seller thành công",
                token = newToken,
                refreshToken = newRefreshToken,
                expiresAt = DateTime.UtcNow.AddDays(30),
                user = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CCCD = user.CCCD,
                    Phone = user.Phone,
                    FacebookLink = user.FacebookLink,
                    ShopName = user.ShopName,
                    IsVerified = user.IsVerified
                }
            });
        }

        /// <summary>
        /// Lấy thông tin chi tiết profile của user (level, stats, verification, etc.)
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Products)
                .Include(u => u.BankInfo)
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Tính level dựa trên tổng giao dịch (mua + bán)
            // Tạm thời chỉ tính từ TotalPurchaseAmount, vì chưa có model Order/Transaction
            // Nếu là seller, có thể tính thêm từ TotalSales nhưng chưa có totalSaleAmount chính xác
            var totalTransactionAmount = user.TotalPurchaseAmount;
            int level = CalculateLevel(totalTransactionAmount);

            // Nếu là seller, lấy thống kê
            int? totalShops = null;
            int? totalSales = null;
            decimal? totalSaleAmount = null;

            if (user.Role == "seller")
            {
                totalShops = await _context.Products.CountAsync(p => p.SellerId == userId);
                totalSales = user.TotalSales; // User.TotalSales vẫn giữ nguyên (seller stats)
                // Tạm thời tính totalSaleAmount từ products (chưa có Order model) - sử dụng ProductStats
                totalSaleAmount = await _context.Products
                    .Where(p => p.SellerId == userId)
                    .Include(p => p.Stats)
                    .SumAsync(p => (decimal?)(p.Stats != null ? p.Stats.TotalSales : 0) * (p.DiscountPrice ?? p.Price)) ?? 0;
            }

            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                Level = level,
                TotalPurchaseAmount = user.TotalPurchaseAmount,
                TotalPurchases = user.TotalPurchases,
                TotalShops = totalShops,
                TotalSales = totalSales,
                TotalSaleAmount = totalSaleAmount,
                IsVerified = user.IsVerified,
                TwoFactorEnabled = user.AuthInfo?.TwoFactorEnabled ?? false,
                EKYCFrontImage = user.EKYCFrontImage,
                EKYCBackImage = user.EKYCBackImage,
                EKYCPortraitImage = user.EKYCPortraitImage,
                Phone = user.Phone,
                ShopName = user.ShopName,
                BankName = user.BankInfo?.BankName,
                BankAccountNumber = user.BankInfo?.BankAccountNumber,
                BankAccountHolder = user.BankInfo?.BankAccountHolder,
                BankBranch = user.BankInfo?.BankBranch
            };

            return Ok(profileDto);
        }

        /// <summary>
        /// Cập nhật thông tin profile của user
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Kiểm tra phone unique nếu có cập nhật
            if (!string.IsNullOrEmpty(updateDto.Phone) && updateDto.Phone != user.Phone)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.Phone == updateDto.Phone && u.Id != userId);
                if (phoneExists)
                {
                    return BadRequest(new { message = "Số điện thoại này đã được sử dụng" });
                }
            }

            // Cập nhật các fields (chỉ cập nhật nếu có giá trị)
            if (!string.IsNullOrEmpty(updateDto.ShopName))
            {
                user.ShopName = updateDto.ShopName;
            }

            if (!string.IsNullOrEmpty(updateDto.Phone))
            {
                user.Phone = updateDto.Phone;
            }

            if (!string.IsNullOrEmpty(updateDto.EKYCFrontImage))
            {
                user.EKYCFrontImage = updateDto.EKYCFrontImage;
            }

            if (!string.IsNullOrEmpty(updateDto.EKYCBackImage))
            {
                user.EKYCBackImage = updateDto.EKYCBackImage;
            }

            if (!string.IsNullOrEmpty(updateDto.EKYCPortraitImage))
            {
                user.EKYCPortraitImage = updateDto.EKYCPortraitImage;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin thành công" });
        }

        /// <summary>
        /// Bật/tắt xác thực 2 lớp (2FA)
        /// </summary>
        /// <param name="toggleDto">DTO chứa trạng thái enable/disable</param>
        /// <returns>Thông báo kết quả và trạng thái 2FA hiện tại</returns>
        [Authorize]
        [HttpPost("toggle-2fa")]
        public async Task<IActionResult> Toggle2FA([FromBody] Toggle2FADto toggleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Lấy hoặc tạo AuthInfo
            var authInfo = user.AuthInfo;
            if (authInfo == null)
            {
                authInfo = new UserAuthInfo
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAuthInfos.Add(authInfo);
            }

            // Cập nhật trạng thái 2FA
            authInfo.TwoFactorEnabled = toggleDto.Enable;
            authInfo.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = toggleDto.Enable ? "Đã bật xác thực 2 lớp" : "Đã tắt xác thực 2 lớp",
                twoFactorEnabled = authInfo.TwoFactorEnabled
            });
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="changePasswordDto">DTO chứa mật khẩu hiện tại, mật khẩu mới và xác nhận mật khẩu mới</param>
        /// <returns>Thông báo kết quả</returns>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Lấy hoặc tạo AuthInfo
            var authInfo = user.AuthInfo;
            if (authInfo == null)
            {
                authInfo = new UserAuthInfo
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAuthInfos.Add(authInfo);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra nếu đăng nhập bằng Google (không có password)
            if (!string.IsNullOrEmpty(authInfo.GoogleId) && string.IsNullOrEmpty(authInfo.PasswordHash))
            {
                return BadRequest(new { message = "Tài khoản đăng nhập bằng Google không thể đổi mật khẩu" });
            }

            // Kiểm tra mật khẩu hiện tại
            if (string.IsNullOrEmpty(authInfo.PasswordHash))
            {
                return BadRequest(new { message = "Tài khoản chưa có mật khẩu. Vui lòng đặt mật khẩu trước." });
            }

            // Xác thực mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, authInfo.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
            }

            // Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(changePasswordDto.NewPassword, authInfo.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu mới phải khác với mật khẩu hiện tại" });
            }

            // Cập nhật mật khẩu mới vào AuthInfo
            authInfo.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            authInfo.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        /// <summary>
        /// Lấy thông tin ngân hàng của user
        /// </summary>
        /// <returns>Thông tin ngân hàng</returns>
        [Authorize]
        [HttpGet("bank-info")]
        public async Task<IActionResult> GetBankInfo()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.BankInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Lấy hoặc tạo mới UserBankInfo nếu chưa có
            var bankInfo = user.BankInfo ?? new UserBankInfo { UserId = userId };

            var bankInfoDto = new BankInfoDto
            {
                BankName = bankInfo.BankName,
                BankAccountNumber = bankInfo.BankAccountNumber,
                BankAccountHolder = bankInfo.BankAccountHolder,
                BankBranch = bankInfo.BankBranch
            };

            return Ok(bankInfoDto);
        }

        /// <summary>
        /// Cập nhật thông tin ngân hàng
        /// </summary>
        /// <param name="updateDto">DTO chứa thông tin ngân hàng cần cập nhật</param>
        /// <returns>Thông báo kết quả và thông tin ngân hàng đã cập nhật</returns>
        [Authorize]
        [HttpPut("bank-info")]
        public async Task<IActionResult> UpdateBankInfo([FromBody] UpdateBankInfoDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.BankInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

            // Lấy hoặc tạo mới UserBankInfo
            var bankInfo = user.BankInfo;
            if (bankInfo == null)
            {
                bankInfo = new UserBankInfo
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserBankInfos.Add(bankInfo);
            }

            // Cập nhật thông tin ngân hàng (chỉ cập nhật các field có giá trị)
            if (!string.IsNullOrEmpty(updateDto.BankName))
            {
                bankInfo.BankName = updateDto.BankName;
            }

            if (!string.IsNullOrEmpty(updateDto.BankAccountNumber))
            {
                bankInfo.BankAccountNumber = updateDto.BankAccountNumber;
            }

            if (!string.IsNullOrEmpty(updateDto.BankAccountHolder))
            {
                bankInfo.BankAccountHolder = updateDto.BankAccountHolder;
            }

            if (!string.IsNullOrEmpty(updateDto.BankBranch))
            {
                bankInfo.BankBranch = updateDto.BankBranch;
            }

            bankInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var bankInfoDto = new BankInfoDto
            {
                BankName = bankInfo.BankName,
                BankAccountNumber = bankInfo.BankAccountNumber,
                BankAccountHolder = bankInfo.BankAccountHolder,
                BankBranch = bankInfo.BankBranch
            };

            return Ok(new
            {
                message = "Cập nhật thông tin ngân hàng thành công",
                bankInfo = bankInfoDto
            });
        }

        /// <summary>
        /// Tính level dựa trên tổng giao dịch
        /// </summary>
        private int CalculateLevel(decimal totalAmount)
        {
            if (totalAmount < 1_000_000)
                return 1;
            else if (totalAmount < 5_000_000)
                return 2;
            else if (totalAmount < 10_000_000)
                return 3;
            else if (totalAmount < 50_000_000)
                return 4;
            else
                return 5;
        }

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailDto testEmailDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                var result = await emailService.SendOtpEmailAsync(
                    testEmailDto.Email, 
                    "123456", 
                    "test"
                );

                if (result)
                {
                    return Ok(new { 
                        message = "Email test đã được gửi thành công!",
                        email = testEmailDto.Email
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Không thể gửi email test",
                        email = testEmailDto.Email
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi test email đến {testEmailDto.Email}");
                return StatusCode(500, new { 
                    message = "Lỗi khi gửi email test",
                    error = ex.Message,
                    details = ex.ToString()
                });
            }
        }
    }

    public class TestEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class GoogleLoginDto
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}

