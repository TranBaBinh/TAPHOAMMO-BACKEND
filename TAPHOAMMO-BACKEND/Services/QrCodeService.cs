using QRCoder;
using System.Collections.Generic;
using System.Linq;

namespace TAPHOAMMO_BACKEND.Services
{
    public interface IQrCodeService
    {
        string GenerateVietQrCode(string accountNumber, string accountName, string bankCode, decimal amount, string content);
        string GenerateVietQrImageUrl(string accountNumber, string accountName, string bankCode, decimal amount, string content);
        string GenerateEmvQrCode(string accountNumber, string accountName, string bankCode, decimal amount, string content);
        string GenerateSimpleQrCode(string accountNumber, string accountName, string content);
        byte[] GenerateQrCodeImage(string data);
        string GenerateQrCodeBase64(string data);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly ILogger<QrCodeService> _logger;

        public QrCodeService(ILogger<QrCodeService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tạo QR code theo format VietQR.io API cho ngân hàng Việt Nam
        /// Format: https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.jpg?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}
        /// Format này được app ngân hàng nhận diện và quét được
        /// </summary>
        public string GenerateVietQrCode(string accountNumber, string accountName, string bankCode, decimal amount, string content)
        {
            // Format VietQR.io API - chuẩn để app ngân hàng quét được
            var encodedContent = Uri.EscapeDataString(content);
            var encodedAccountName = Uri.EscapeDataString(accountName);
            
            // Sử dụng bankCode được truyền vào, mặc định MBank nếu không có
            var actualBankCode = !string.IsNullOrEmpty(bankCode) && bankCode.Length == 6 ? bankCode : "970422";
            
            // Format VietQR.io image URL - PHẢI có -compact2.jpg để app ngân hàng quét được
            // https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.jpg?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}
            var url = $"https://img.vietqr.io/image/{actualBankCode}-{accountNumber}-compact2.jpg";
            
            // Thêm query parameters
            var queryParams = new List<string>();
            
            // Thêm amount nếu có - PHẢI là số nguyên (không có decimal)
            if (amount > 0)
            {
                var amountInt = (int)amount; // Convert sang integer
                queryParams.Add($"amount={amountInt}");
            }
            
            // Thêm addInfo (nội dung chuyển khoản)
            if (!string.IsNullOrEmpty(content))
            {
                queryParams.Add($"addInfo={encodedContent}");
            }
            
            // Thêm accountName
            if (!string.IsNullOrEmpty(accountName))
            {
                queryParams.Add($"accountName={encodedAccountName}");
            }
            
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            _logger.LogInformation($"Generated VietQR URL: {url}");
            return url;
        }

        /// <summary>
        /// Tạo URL ảnh QR code từ VietQR.io API
        /// Format: https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.jpg?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}
        /// Sử dụng khi muốn hiển thị QR code trực tiếp từ VietQR.io thay vì generate local
        /// </summary>
        public string GenerateVietQrImageUrl(string accountNumber, string accountName, string bankCode, decimal amount, string content)
        {
            var encodedContent = Uri.EscapeDataString(content);
            var encodedAccountName = Uri.EscapeDataString(accountName);
            
            // Sử dụng bankCode được truyền vào, mặc định MBank nếu không có
            var actualBankCode = !string.IsNullOrEmpty(bankCode) && bankCode.Length == 6 ? bankCode : "970422";
            
            // Format VietQR.io image API - PHẢI có -compact2.jpg để app ngân hàng quét được
            // https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-compact2.jpg?amount={AMOUNT}&addInfo={CONTENT}&accountName={ACCOUNT_NAME}
            var url = $"https://img.vietqr.io/image/{actualBankCode}-{accountNumber}-compact2.jpg";
            
            // Thêm query parameters
            var queryParams = new List<string>();
            
            // Thêm amount nếu có - PHẢI là số nguyên (không có decimal)
            if (amount > 0)
            {
                var amountInt = (int)amount; // Convert sang integer
                queryParams.Add($"amount={amountInt}");
            }
            
            // Thêm addInfo (nội dung chuyển khoản)
            if (!string.IsNullOrEmpty(content))
            {
                queryParams.Add($"addInfo={encodedContent}");
            }
            
            // Thêm accountName
            if (!string.IsNullOrEmpty(accountName))
            {
                queryParams.Add($"accountName={encodedAccountName}");
            }
            
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            _logger.LogInformation($"Generated VietQR Image URL: {url}");
            return url;
        }

        /// <summary>
        /// Tạo QR code theo chuẩn EMV QR Code (VietQR/NAPAS 247)
        /// Format này đúng chuẩn để app ngân hàng nhận diện
        /// </summary>
        public string GenerateEmvQrCode(string accountNumber, string accountName, string bankCode, decimal amount, string content)
        {
            // EMV QR Code format theo chuẩn NAPAS 247 (VietQR)
            // Cấu trúc: 00-99 là các data object identifiers
            
            var builder = new System.Text.StringBuilder();
            
            // 00: Payload Format Indicator (01 = EMV QR Code)
            builder.Append("000201");
            
            // 01: Point of Initiation Method (11 = Static, 12 = Dynamic)
            builder.Append("010212"); // 12 = Dynamic (có thể thay đổi số tiền)
            
            // 38: Merchant Account Information (VietQR)
            // 00: GUID = A000000727 (NAPAS)
            // 01: Bank BIN (6 chữ số) - sử dụng bankCode được truyền vào
            // 02: Account Number
            var bankBin = bankCode.Length == 6 ? bankCode : "970422"; // Mặc định MBank
            var merchantInfo = $"0010A00000072701{bankBin.Length:D2}{bankBin}02{accountNumber.Length:D2}{accountNumber}";
            
            // Thêm nội dung chuyển khoản nếu có
            if (!string.IsNullOrEmpty(content))
            {
                merchantInfo += $"08{content.Length:D2}{content}";
            }
            
            builder.Append($"38{merchantInfo.Length:D2}{merchantInfo}");
            
            // 52: Merchant Category Code (0000 = Default)
            builder.Append("52040000");
            
            // 53: Transaction Currency (704 = VND)
            builder.Append("5303704");
            
            // 54: Transaction Amount (optional - chỉ thêm nếu có amount)
            if (amount > 0)
            {
                var amountStr = amount.ToString("F2").Replace(".", "");
                builder.Append($"54{amountStr.Length:D2}{amountStr}");
            }
            
            // 58: Country Code (VN)
            builder.Append("5802VN");
            
            // 59: Merchant Name (Tên chủ tài khoản)
            var merchantName = accountName.Length > 25 ? accountName.Substring(0, 25) : accountName;
            builder.Append($"59{merchantName.Length:D2}{merchantName}");
            
            // 60: Merchant City
            builder.Append("6007Vietnam");
            
            // 62: Additional Data Field Template
            // 08: Store Label (Nội dung chuyển khoản)
            if (!string.IsNullOrEmpty(content))
            {
                var addData = $"08{content.Length:D2}{content}";
                builder.Append($"62{addData.Length:D2}{addData}");
            }
            
            // 63: CRC (Cyclic Redundancy Check) - tính sau
            var payload = builder.ToString();
            
            // Tính CRC16-CCITT
            var crc = CalculateCrc16Ccitt(payload + "6304");
            builder.Append($"6304{crc}");
            
            return builder.ToString();
        }

        /// <summary>
        /// Tính CRC16-CCITT cho EMV QR Code
        /// </summary>
        private string CalculateCrc16Ccitt(string data)
        {
            ushort crc = 0xFFFF;
            ushort polynomial = 0x1021;
            
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            
            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    bool bit = ((b >> (7 - i) & 1) == 1);
                    bool c15 = ((crc >> 15 & 1) == 1);
                    crc <<= 1;
                    if (c15 ^ bit) crc ^= polynomial;
                }
            }
            
            crc &= 0xFFFF;
            crc ^= 0xFFFF; // Final XOR
            return crc.ToString("X4");
        }

        /// <summary>
        /// Tạo QR code đơn giản - chỉ chứa thông tin tài khoản dạng text
        /// Format này app ngân hàng có thể tự nhận diện
        /// </summary>
        public string GenerateSimpleQrCode(string accountNumber, string accountName, string content)
        {
            // Format đơn giản: Chỉ số tài khoản
            // App ngân hàng sẽ tự nhận diện số tài khoản và điền thông tin
            // User sẽ tự nhập nội dung chuyển khoản
            return accountNumber;
        }

        /// <summary>
        /// Tạo QR code image từ data string (sử dụng PngByteQRCode để tương thích với .NET 8)
        /// </summary>
        public byte[] GenerateQrCodeImage(string data)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            }
        }

        /// <summary>
        /// Tạo QR code base64 string từ data
        /// </summary>
        public string GenerateQrCodeBase64(string data)
        {
            var imageBytes = GenerateQrCodeImage(data);
            return Convert.ToBase64String(imageBytes);
        }
    }
}

