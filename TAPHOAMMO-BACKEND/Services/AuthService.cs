using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;
using BCrypt.Net;

namespace TAPHOAMMO_BACKEND.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto?> LoginWithGoogleAsync(string googleId, string email, string name);
        Task<AuthResponseDto?> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> CheckUserExistsAsync(string email);
        Task<bool> CheckUsernameExistsAsync(string username);
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<User?> GetUserByEmailAsync(string email);
        string GenerateJwtToken(User user, bool rememberMe = false);
        string GenerateRefreshToken();
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Helper method để đảm bảo UserAuthInfo tồn tại cho user
        /// </summary>
        private async Task<UserAuthInfo> GetOrCreateAuthInfoAsync(User user)
        {
            if (user.AuthInfo == null)
            {
                user.AuthInfo = new UserAuthInfo
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAuthInfos.Add(user.AuthInfo);
                await _context.SaveChangesAsync();
            }
            return user.AuthInfo;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return null; // Email already exists
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return null; // Username already exists
            }

            // Validate role
            if (registerDto.Role != "user" && registerDto.Role != "seller")
            {
                registerDto.Role = "user";
            }

            // Create new user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Role = registerDto.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Tạo UserAuthInfo với password
            var authInfo = new UserAuthInfo
            {
                UserId = user.Id,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserAuthInfos.Add(authInfo);

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token vào AuthInfo
            authInfo.RefreshToken = refreshToken;
            authInfo.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return null; // User not found
            }

            // Lấy hoặc tạo AuthInfo
            var authInfo = await GetOrCreateAuthInfoAsync(user);

            // Kiểm tra password
            if (string.IsNullOrEmpty(authInfo.PasswordHash) || 
                !BCrypt.Net.BCrypt.Verify(loginDto.Password, authInfo.PasswordHash))
            {
                return null; // Invalid credentials
            }

            // Generate tokens
            var token = GenerateJwtToken(user, loginDto.RememberMe);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token vào AuthInfo
            authInfo.RefreshToken = refreshToken;
            authInfo.RefreshTokenExpiryTime = loginDto.RememberMe 
                ? DateTime.UtcNow.AddDays(30) 
                : DateTime.UtcNow.AddDays(7);
            authInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = loginDto.RememberMe 
                    ? DateTime.UtcNow.AddDays(30) 
                    : DateTime.UtcNow.AddHours(1),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }

        public async Task<AuthResponseDto?> LoginWithGoogleAsync(string googleId, string email, string name)
        {
            // Tìm user theo GoogleId hoặc Email (kiểm tra trong AuthInfo)
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => 
                    (u.AuthInfo != null && u.AuthInfo.GoogleId == googleId) || 
                    u.Email == email);

            if (user == null)
            {
                // Create new user if doesn't exist (đăng ký bằng Google - không cần OTP)
                // Tạo username unique từ email (lấy phần trước @) hoặc name
                var baseUsername = !string.IsNullOrEmpty(name) ? name : email.Split('@')[0];
                // Làm sạch username: loại bỏ khoảng trắng và ký tự đặc biệt
                baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-zA-Z0-9_]", "");
                // Nếu sau khi làm sạch rỗng, dùng email prefix
                if (string.IsNullOrEmpty(baseUsername))
                {
                    baseUsername = email.Split('@')[0];
                }
                var username = baseUsername;
                
                // Đảm bảo username unique
                int counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{baseUsername}{counter}";
                    counter++;
                }

                user = new User
                {
                    Username = username,
                    FullName = name, // Lưu FullName từ Google
                    Email = email,
                    Role = "user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Tạo UserAuthInfo với GoogleId
                var authInfo = new UserAuthInfo
                {
                    UserId = user.Id,
                    GoogleId = googleId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAuthInfos.Add(authInfo);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Lấy hoặc tạo AuthInfo
                var authInfo = await GetOrCreateAuthInfoAsync(user);

                if (string.IsNullOrEmpty(authInfo.GoogleId))
                {
                    // Link Google account to existing user
                    authInfo.GoogleId = googleId;
                    authInfo.UpdatedAt = DateTime.UtcNow;
                }

                // Cập nhật FullName nếu chưa có
                if (string.IsNullOrEmpty(user.FullName))
                {
                    user.FullName = name;
                }
                await _context.SaveChangesAsync();
            }

            // Generate tokens
            var token = GenerateJwtToken(user, true);
            var refreshToken = GenerateRefreshToken();

            // Lấy AuthInfo để lưu refresh token
            var userAuthInfo = await GetOrCreateAuthInfoAsync(user);
            userAuthInfo.RefreshToken = refreshToken;
            userAuthInfo.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            userAuthInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                User = new UserInfoDto
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
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal == null) return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return null;

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            var authInfo = await GetOrCreateAuthInfoAsync(user);

            if (authInfo.RefreshToken != refreshToken || 
                authInfo.RefreshTokenExpiryTime == null || 
                authInfo.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            // Generate new tokens
            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Save new refresh token vào AuthInfo
            authInfo.RefreshToken = newRefreshToken;
            authInfo.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            authInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }

        public string GenerateJwtToken(User user, bool rememberMe = false)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = rememberMe ? 43200 : 60; // 30 days or 1 hour

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<bool> CheckUserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
            {
                return false;
            }

            var authInfo = await GetOrCreateAuthInfoAsync(user);

            // Nếu là Google user (không có password)
            if (string.IsNullOrEmpty(authInfo.PasswordHash))
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, authInfo.PasswordHash);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.AuthInfo)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}

