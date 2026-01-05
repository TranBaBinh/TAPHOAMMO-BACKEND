using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Models;
using TAPHOAMMO_BACKEND.Models.DTOs;
using TAPHOAMMO_BACKEND.Services;

namespace TAPHOAMMO_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WalletController> _logger;
        private readonly IQrCodeService _qrCodeService;
        private readonly IActivityLogService _activityLogService;

        // Thông tin tài khoản admin cố định
        private const string ADMIN_ACCOUNT_NUMBER = "9945180596";
        private const string ADMIN_ACCOUNT_NAME = "VO MINH ANH";
        private const string ADMIN_BANK_NAME = "VCB"; // Ngân hàng Ngoại thương Việt Nam (Vietcombank)
        private const string ADMIN_BANK_CODE = "970436"; // Mã BIN của Vietcombank cho VietQR

        public WalletController(ApplicationDbContext context, ILogger<WalletController> logger, IQrCodeService qrCodeService, IActivityLogService activityLogService)
        {
            _context = context;
            _logger = logger;
            _qrCodeService = qrCodeService;
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Lấy số dư ví của user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                // Tạo wallet mới nếu chưa có
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    PendingBalance = 0
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return Ok(new WalletBalanceDto
            {
                Balance = wallet.Balance,
                PendingBalance = wallet.PendingBalance,
                AvailableBalance = wallet.Balance - wallet.PendingBalance
            });
        }

        /// <summary>
        /// Test QR code với tài khoản VCB - sử dụng format VietQR.io Image URL
        /// </summary>
        [HttpGet("test-qr-mb")]
        [AllowAnonymous]
        public IActionResult TestQrCodeMB()
        {
            // Test với tài khoản VCB (Vietcombank)
            const string TEST_ACCOUNT_NUMBER = "9945180596";
            const string TEST_ACCOUNT_NAME = "VO MINH ANH";
            const string TEST_BANK_NAME = "VCB";
            const string TEST_BANK_CODE = "970436";
            const string TEST_CONTENT = "TEST123";

            // Sử dụng VietQR.io Image URL - format đúng để app ngân hàng quét được
            var qrCodeUrl = _qrCodeService.GenerateVietQrImageUrl(
                TEST_ACCOUNT_NUMBER,
                TEST_ACCOUNT_NAME,
                TEST_BANK_CODE,
                0,
                TEST_CONTENT
            );

            var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrCodeUrl);

            return Ok(new
            {
                qrCodeBase64 = $"data:image/png;base64,{qrCodeBase64}",
                qrCodeUrl = qrCodeUrl,
                qrCodeText = qrCodeUrl,
                accountNumber = TEST_ACCOUNT_NUMBER,
                accountName = TEST_ACCOUNT_NAME,
                bankName = TEST_BANK_NAME,
                bankCode = TEST_BANK_CODE,
                content = TEST_CONTENT,
                format = "VietQR.io Image URL (Format chuẩn - app ngân hàng quét được)",
                note = "QR code được tạo theo format VietQR.io Image URL với -compact2.jpg"
            });
        }

        /// <summary>
        /// Lấy QR code để nạp tiền (mỗi user có mã cố định riêng)
        /// Sử dụng format VietQR.io Image URL - chuẩn để app ngân hàng quét được
        /// </summary>
        [HttpGet("deposit-qr")]
        public async Task<IActionResult> GetDepositQrCode()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Lấy hoặc tạo wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    PendingBalance = 0
                };
                _context.Wallets.Add(wallet);
            }

            // Tạo mã nạp tiền cố định cho user (nếu chưa có)
            if (string.IsNullOrEmpty(wallet.DepositCode))
            {
                // Format: NAP{userId} - mã cố định, không thay đổi
                wallet.DepositCode = $"NAP{userId}";
                wallet.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Sử dụng mã cố định làm nội dung chuyển khoản
            var transferContent = wallet.DepositCode;

            // Sử dụng VietQR.io Image URL - format đúng để app ngân hàng quét được
            // Format: https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.jpg?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}
            var qrCodeUrl = _qrCodeService.GenerateVietQrImageUrl(
                ADMIN_ACCOUNT_NUMBER,
                ADMIN_ACCOUNT_NAME,
                ADMIN_BANK_CODE,
                0, // Không set số tiền cố định
                transferContent
            );

            // Download image từ VietQR URL và convert sang base64
            string qrCodeBase64;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    var imageBytes = await httpClient.GetByteArrayAsync(qrCodeUrl);
                    var base64String = Convert.ToBase64String(imageBytes);
                    // Phải có prefix data:image/png;base64, để frontend hiển thị được
                    qrCodeBase64 = $"data:image/png;base64,{base64String}";
                    _logger.LogInformation($"Successfully downloaded QR code image from VietQR URL. Size: {imageBytes.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download QR code image from VietQR URL: {qrCodeUrl}");
                // Fallback: Generate QR code từ URL text nếu download thất bại
                qrCodeBase64 = $"data:image/png;base64,{_qrCodeService.GenerateQrCodeBase64(qrCodeUrl)}";
            }

            return Ok(new QrCodeResponseDto
            {
                QrCodeBase64 = qrCodeBase64, // Đã có prefix data:image/png;base64, rồi
                QrCodeUrl = qrCodeUrl,
                AccountNumber = ADMIN_ACCOUNT_NUMBER,
                AccountName = ADMIN_ACCOUNT_NAME,
                BankName = ADMIN_BANK_NAME,
                BankCode = ADMIN_BANK_CODE,
                Amount = null, // Không có số tiền cố định
                TransferContent = transferContent, // Mã cố định của user
                TransactionId = wallet.DepositCode
            });
        }

        /// <summary>
        /// Nạp tiền vào ví - User nạp vào tài khoản admin
        /// </summary>
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Lấy hoặc tạo wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    PendingBalance = 0
                };
                _context.Wallets.Add(wallet);
            }

            // Tạo transaction nạp tiền
            var transaction = new WalletTransaction
            {
                UserId = userId,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Pending,
                Amount = dto.Amount,
                Description = dto.Description ?? "Nạp tiền vào ví",
                AdminAccountNumber = ADMIN_ACCOUNT_NUMBER,
                AdminAccountName = ADMIN_ACCOUNT_NAME,
                AdminBankName = ADMIN_BANK_NAME,
                ProofImageUrl = dto.ProofImageUrl
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new WalletTransactionDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Amount = transaction.Amount,
                Description = transaction.Description,
                AdminAccountNumber = transaction.AdminAccountNumber,
                AdminAccountName = transaction.AdminAccountName,
                AdminBankName = transaction.AdminBankName,
                ProofImageUrl = transaction.ProofImageUrl,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        /// <summary>
        /// Rút tiền từ ví (chỉ seller)
        /// </summary>
        [HttpPost("withdraw")]
        [Authorize(Roles = "seller")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Lấy wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null || wallet.Balance < dto.Amount)
            {
                return BadRequest(new { message = "Số dư không đủ" });
            }

            // Kiểm tra số dư khả dụng
            var availableBalance = wallet.Balance - wallet.PendingBalance;
            if (availableBalance < dto.Amount)
            {
                return BadRequest(new { message = "Số dư khả dụng không đủ" });
            }

            // Tạo transaction rút tiền
            var transaction = new WalletTransaction
            {
                UserId = userId,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Pending,
                Amount = dto.Amount,
                Description = dto.Description ?? "Rút tiền từ ví",
                WithdrawalAccountNumber = dto.AccountNumber,
                WithdrawalAccountName = dto.AccountName,
                WithdrawalBankName = dto.BankName
            };

            // Trừ số dư (sẽ được cộng lại nếu rút thất bại)
            wallet.Balance -= dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Log activity: Rút tiền
            try
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "Rút tiền",
                    "withdraw",
                    null,
                    $"Rút tiền từ ví: {dto.Amount:N0} VNĐ",
                    new { Amount = dto.Amount, TransactionId = transaction.Id, AccountNumber = dto.AccountNumber, BankName = dto.BankName }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho withdraw");
            }

            return Ok(new WalletTransactionDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Amount = transaction.Amount,
                Description = transaction.Description,
                WithdrawalAccountNumber = transaction.WithdrawalAccountNumber,
                WithdrawalAccountName = transaction.WithdrawalAccountName,
                WithdrawalBankName = transaction.WithdrawalBankName,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        /// <summary>
        /// Lấy lịch sử giao dịch
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? type = null, // deposit, withdrawal, purchase
            [FromQuery] string? status = null, // pending, completed, failed, cancelled
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var query = _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Seller)
                .Include(t => t.Product)
                .AsQueryable();

            // Lọc theo type
            if (!string.IsNullOrEmpty(type))
            {
                if (Enum.TryParse<TransactionType>(type, true, out var transactionType))
                {
                    query = query.Where(t => t.Type == transactionType);
                }
            }

            // Lọc theo status
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<TransactionStatus>(status, true, out var transactionStatus))
                {
                    query = query.Where(t => t.Status == transactionStatus);
                }
            }

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var transactionDtos = transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                AdminAccountNumber = t.AdminAccountNumber,
                AdminAccountName = t.AdminAccountName,
                AdminBankName = t.AdminBankName,
                SellerId = t.SellerId,
                SellerName = t.Seller?.FullName ?? t.Seller?.Username,
                ProductId = t.ProductId,
                ProductName = t.Product?.Name,
                RelatedTransactionId = t.RelatedTransactionId,
                ReportDeadline = t.ReportDeadline,
                WithdrawalAccountNumber = t.WithdrawalAccountNumber,
                WithdrawalAccountName = t.WithdrawalAccountName,
                WithdrawalBankName = t.WithdrawalBankName,
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            return Ok(new TransactionHistoryDto
            {
                Transactions = transactionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Lấy lịch sử nạp tiền
        /// </summary>
        [HttpGet("deposits")]
        public async Task<IActionResult> GetDeposits(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await GetTransactions("deposit", status, page, pageSize);
        }

        /// <summary>
        /// Lấy lịch sử rút tiền
        /// </summary>
        [HttpGet("withdrawals")]
        [Authorize(Roles = "seller")]
        public async Task<IActionResult> GetWithdrawals(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await GetTransactions("withdrawal", status, page, pageSize);
        }

        /// <summary>
        /// Lấy lịch sử mua hàng
        /// </summary>
        [HttpGet("purchases")]
        public async Task<IActionResult> GetPurchases(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await GetTransactions("purchase", status, page, pageSize);
        }

        /// <summary>
        /// Admin: Xác nhận nạp tiền (chuyển từ Pending -> Completed)
        /// </summary>
        [HttpPost("transactions/{id}/approve")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveDeposit(int id, [FromBody] UpdateTransactionStatusDto dto)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.User)
                .ThenInclude(u => u.BankInfo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            if (transaction.Type != TransactionType.Deposit)
            {
                return BadRequest(new { message = "Chỉ có thể xác nhận giao dịch nạp tiền" });
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                return BadRequest(new { message = "Giao dịch không ở trạng thái chờ xử lý" });
            }

            // Cập nhật trạng thái
            var oldStatus = transaction.Status.ToString();
            transaction.Status = TransactionStatus.Completed;
            transaction.UpdatedAt = DateTime.UtcNow;

            // Cập nhật UpdatedFields
            UpdateTransactionFields(transaction, "Status", oldStatus, transaction.Status.ToString());

            // Cộng tiền vào ví
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == transaction.UserId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = transaction.UserId,
                    Balance = 0,
                    PendingBalance = 0
                };
                _context.Wallets.Add(wallet);
            }

            var oldBalance = wallet.Balance;
            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            UpdateWalletFields(wallet, "Balance", oldBalance.ToString(), wallet.Balance.ToString());

            await _context.SaveChangesAsync();

            // Log activity: Admin xác nhận nạp tiền
            try
            {
                var adminUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _activityLogService.LogActivityAsync(
                    adminUserId,
                    "Xác nhận nạp tiền",
                    "approve_deposit",
                    null,
                    $"Admin xác nhận nạp tiền: {transaction.Amount:N0} VNĐ cho user {transaction.UserId}",
                    new { TransactionId = transaction.Id, UserId = transaction.UserId, Amount = transaction.Amount }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho approve_deposit");
            }

            return Ok(new { message = "Xác nhận nạp tiền thành công" });
        }

        /// <summary>
        /// Admin: Từ chối nạp tiền
        /// </summary>
        [HttpPost("transactions/{id}/reject")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RejectDeposit(int id, [FromBody] UpdateTransactionStatusDto dto)
        {
            var transaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                return BadRequest(new { message = "Giao dịch không ở trạng thái chờ xử lý" });
            }

            var oldStatus = transaction.Status.ToString();
            transaction.Status = TransactionStatus.Failed;
            transaction.Description = $"{transaction.Description} - Từ chối: {dto.Note}";
            transaction.UpdatedAt = DateTime.UtcNow;
            UpdateTransactionFields(transaction, "Status", oldStatus, transaction.Status.ToString());

            await _context.SaveChangesAsync();

            return Ok(new { message = "Từ chối nạp tiền thành công" });
        }

        /// <summary>
        /// Admin: Xác nhận rút tiền (chuyển từ Pending -> Completed)
        /// </summary>
        [HttpPost("transactions/{id}/approve-withdrawal")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveWithdrawal(int id, [FromBody] UpdateTransactionStatusDto dto)
        {
            var transaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            if (transaction.Type != TransactionType.Withdrawal)
            {
                return BadRequest(new { message = "Chỉ có thể xác nhận giao dịch rút tiền" });
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                return BadRequest(new { message = "Giao dịch không ở trạng thái chờ xử lý" });
            }

            var oldStatus = transaction.Status.ToString();
            transaction.Status = TransactionStatus.Completed;
            transaction.UpdatedAt = DateTime.UtcNow;
            UpdateTransactionFields(transaction, "Status", oldStatus, transaction.Status.ToString());

            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác nhận rút tiền thành công" });
        }

        /// <summary>
        /// Admin: Từ chối rút tiền (hoàn lại số dư)
        /// </summary>
        [HttpPost("transactions/{id}/reject-withdrawal")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RejectWithdrawal(int id, [FromBody] UpdateTransactionStatusDto dto)
        {
            var transaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                return BadRequest(new { message = "Giao dịch không ở trạng thái chờ xử lý" });
            }

            var oldStatus = transaction.Status.ToString();
            transaction.Status = TransactionStatus.Failed;
            transaction.Description = $"{transaction.Description} - Từ chối: {dto.Note}";
            transaction.UpdatedAt = DateTime.UtcNow;
            UpdateTransactionFields(transaction, "Status", oldStatus, transaction.Status.ToString());

            // Hoàn lại số dư
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == transaction.UserId);

            if (wallet != null)
            {
                var oldBalance = wallet.Balance;
                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                UpdateWalletFields(wallet, "Balance", oldBalance.ToString(), wallet.Balance.ToString());
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Từ chối rút tiền thành công, đã hoàn lại số dư" });
        }

        /// <summary>
        /// Tạo giao dịch mua hàng (sẽ được gọi từ OrderController hoặc CartController)
        /// </summary>
        [HttpPost("purchase")]
        public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Lấy wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null || wallet.Balance < dto.Amount)
            {
                return BadRequest(new { message = "Số dư không đủ" });
            }

            // Kiểm tra số dư khả dụng
            var availableBalance = wallet.Balance - wallet.PendingBalance;
            if (availableBalance < dto.Amount)
            {
                return BadRequest(new { message = "Số dư khả dụng không đủ" });
            }

            // Lấy thông tin sản phẩm và seller
            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            // Tạo transaction mua hàng
            var transaction = new WalletTransaction
            {
                UserId = userId,
                Type = TransactionType.Purchase,
                Status = TransactionStatus.Completed, // Mua hàng tự động hoàn thành
                Amount = dto.Amount,
                Description = dto.Description ?? $"Mua sản phẩm: {product.Name}",
                SellerId = product.SellerId,
                ProductId = dto.ProductId,
                ReportDeadline = DateTime.UtcNow.AddDays(3) // 3 ngày để report
            };

            // Trừ số dư và tăng pending balance (tiền sẽ được chuyển cho seller sau 3 ngày)
            var oldBalance = wallet.Balance;
            var oldPendingBalance = wallet.PendingBalance;
            wallet.Balance -= dto.Amount;
            wallet.PendingBalance += dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            UpdateWalletFields(wallet, "Balance", oldBalance.ToString(), wallet.Balance.ToString());
            UpdateWalletFields(wallet, "PendingBalance", oldPendingBalance.ToString(), wallet.PendingBalance.ToString());

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Log activity: Mua hàng
            try
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "Mua hàng",
                    "purchase",
                    null,
                    $"Mua sản phẩm: {product.Name} - {dto.Amount:N0} VNĐ",
                    new { Amount = dto.Amount, TransactionId = transaction.Id, ProductId = dto.ProductId, ProductName = product.Name, SellerId = product.SellerId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi log activity cho purchase");
            }

            // Tạo background job để chuyển tiền cho seller sau 3 ngày (có thể dùng Hangfire hoặc background service)
            // Ở đây tạm thời chỉ lưu transaction, admin sẽ xử lý thủ công hoặc có scheduled job

            return Ok(new WalletTransactionDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Amount = transaction.Amount,
                Description = transaction.Description,
                SellerId = transaction.SellerId,
                SellerName = product.Seller?.FullName ?? product.Seller?.Username,
                ProductId = transaction.ProductId,
                ProductName = product.Name,
                ReportDeadline = transaction.ReportDeadline,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        /// <summary>
        /// Admin: Chuyển tiền cho seller sau 3 ngày (nếu không có report)
        /// </summary>
        [HttpPost("transactions/{id}/transfer-to-seller")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> TransferToSeller(int id)
        {
            var purchaseTransaction = await _context.WalletTransactions
                .Include(t => t.User)
                .Include(t => t.Seller)
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (purchaseTransaction == null)
            {
                return NotFound(new { message = "Giao dịch không tồn tại" });
            }

            if (purchaseTransaction.Type != TransactionType.Purchase)
            {
                return BadRequest(new { message = "Chỉ có thể chuyển tiền cho giao dịch mua hàng" });
            }

            if (purchaseTransaction.Status != TransactionStatus.Completed)
            {
                return BadRequest(new { message = "Giao dịch mua hàng chưa hoàn thành" });
            }

            // Kiểm tra đã quá 3 ngày chưa
            if (purchaseTransaction.ReportDeadline.HasValue && 
                purchaseTransaction.ReportDeadline.Value > DateTime.UtcNow)
            {
                return BadRequest(new { message = "Chưa đến thời hạn chuyển tiền (còn chờ 3 ngày)" });
            }

            // Kiểm tra đã chuyển tiền chưa
            var existingTransfer = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.RelatedTransactionId == id && 
                                         t.Type == TransactionType.TransferToSeller);

            if (existingTransfer != null)
            {
                return BadRequest(new { message = "Đã chuyển tiền cho seller rồi" });
            }

            // Lấy wallet của buyer
            var buyerWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == purchaseTransaction.UserId);

            if (buyerWallet == null || buyerWallet.PendingBalance < purchaseTransaction.Amount)
            {
                return BadRequest(new { message = "Số dư pending không đủ" });
            }

            // Lấy hoặc tạo wallet của seller
            var sellerWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == purchaseTransaction.SellerId);

            if (sellerWallet == null)
            {
                sellerWallet = new Wallet
                {
                    UserId = purchaseTransaction.SellerId ?? 0,
                    Balance = 0,
                    PendingBalance = 0
                };
                _context.Wallets.Add(sellerWallet);
            }

            // Tạo transaction chuyển tiền cho seller
            var transferTransaction = new WalletTransaction
            {
                UserId = purchaseTransaction.SellerId ?? 0,
                Type = TransactionType.TransferToSeller,
                Status = TransactionStatus.Completed,
                Amount = purchaseTransaction.Amount,
                Description = $"Nhận tiền từ giao dịch mua hàng #{purchaseTransaction.Id}",
                RelatedTransactionId = id,
                SellerId = purchaseTransaction.SellerId
            };

            // Cập nhật số dư
            // Buyer: giảm pending balance
            var oldBuyerPending = buyerWallet.PendingBalance;
            buyerWallet.PendingBalance -= purchaseTransaction.Amount;
            buyerWallet.UpdatedAt = DateTime.UtcNow;
            UpdateWalletFields(buyerWallet, "PendingBalance", oldBuyerPending.ToString(), buyerWallet.PendingBalance.ToString());

            // Seller: tăng balance
            var oldSellerBalance = sellerWallet.Balance;
            sellerWallet.Balance += purchaseTransaction.Amount;
            sellerWallet.UpdatedAt = DateTime.UtcNow;
            UpdateWalletFields(sellerWallet, "Balance", oldSellerBalance.ToString(), sellerWallet.Balance.ToString());

            _context.WalletTransactions.Add(transferTransaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chuyển tiền cho seller thành công" });
        }

        /// <summary>
        /// Admin: Lấy danh sách giao dịch cần xử lý
        /// </summary>
        [HttpGet("admin/pending-transactions")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetPendingTransactions(
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.WalletTransactions
                .Where(t => t.Status == TransactionStatus.Pending)
                .Include(t => t.User)
                .Include(t => t.Seller)
                .Include(t => t.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                if (Enum.TryParse<TransactionType>(type, true, out var transactionType))
                {
                    query = query.Where(t => t.Type == transactionType);
                }
            }

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var transactionDtos = transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                AdminAccountNumber = t.AdminAccountNumber,
                AdminAccountName = t.AdminAccountName,
                AdminBankName = t.AdminBankName,
                SellerId = t.SellerId,
                SellerName = t.Seller?.FullName ?? t.Seller?.Username,
                ProductId = t.ProductId,
                ProductName = t.Product?.Name,
                RelatedTransactionId = t.RelatedTransactionId,
                ReportDeadline = t.ReportDeadline,
                WithdrawalAccountNumber = t.WithdrawalAccountNumber,
                WithdrawalAccountName = t.WithdrawalAccountName,
                WithdrawalBankName = t.WithdrawalBankName,
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            return Ok(new TransactionHistoryDto
            {
                Transactions = transactionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Admin: Lấy danh sách giao dịch mua hàng cần chuyển tiền cho seller (sau 3 ngày)
        /// </summary>
        [HttpGet("admin/pending-transfers")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetPendingTransfers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.WalletTransactions
                .Where(t => t.Type == TransactionType.Purchase &&
                           t.Status == TransactionStatus.Completed &&
                           t.ReportDeadline.HasValue &&
                           t.ReportDeadline.Value <= DateTime.UtcNow)
                .Where(t => !_context.WalletTransactions.Any(tt => 
                    tt.RelatedTransactionId == t.Id && 
                    tt.Type == TransactionType.TransferToSeller))
                .Include(t => t.User)
                .Include(t => t.Seller)
                .Include(t => t.Product)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderBy(t => t.ReportDeadline)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var transactionDtos = transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                SellerId = t.SellerId,
                SellerName = t.Seller?.FullName ?? t.Seller?.Username,
                ProductId = t.ProductId,
                ProductName = t.Product?.Name,
                ReportDeadline = t.ReportDeadline,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            return Ok(new TransactionHistoryDto
            {
                Transactions = transactionDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // Helper methods để cập nhật UpdatedFields
        private void UpdateTransactionFields(WalletTransaction transaction, string fieldName, string oldValue, string newValue)
        {
            Dictionary<string, string>? fields = null;
            
            if (!string.IsNullOrEmpty(transaction.UpdatedFields))
            {
                try
                {
                    fields = JsonSerializer.Deserialize<Dictionary<string, string>>(transaction.UpdatedFields);
                }
                catch
                {
                    fields = new Dictionary<string, string>();
                }
            }
            else
            {
                fields = new Dictionary<string, string>();
            }

            fields![fieldName] = $"{oldValue}->{newValue}";
            transaction.UpdatedFields = JsonSerializer.Serialize(fields);
        }

        private void UpdateWalletFields(Wallet wallet, string fieldName, string oldValue, string newValue)
        {
            Dictionary<string, string>? fields = null;
            
            if (!string.IsNullOrEmpty(wallet.UpdatedFields))
            {
                try
                {
                    fields = JsonSerializer.Deserialize<Dictionary<string, string>>(wallet.UpdatedFields);
                }
                catch
                {
                    fields = new Dictionary<string, string>();
                }
            }
            else
            {
                fields = new Dictionary<string, string>();
            }

            fields![fieldName] = $"{oldValue}->{newValue}";
            wallet.UpdatedFields = JsonSerializer.Serialize(fields);
        }
    }

}

