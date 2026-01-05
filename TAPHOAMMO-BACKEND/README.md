# TAPHOAMMO Backend API

Backend API cho ứng dụng TAPHOAMMO được xây dựng bằng ASP.NET Core 8.0.

## Cài Đặt và Chạy

### Yêu Cầu
- .NET 8.0 SDK
- SQL Server

### Cài Đặt Packages
```bash
dotnet restore
```

### Cấu Hình Database
Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=YOUR_SERVER;uid=YOUR_USER;password=YOUR_PASSWORD;database=TAPHOAMMO;Encrypt=True;TrustServerCertificate=True;"
  }
}
```

### Tạo Database và Migration
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Seed Dữ Liệu Mẫu
Sau khi chạy ứng dụng, gọi API:
```
POST http://localhost:5133/api/seed
```

Hoặc sử dụng Swagger UI tại `http://localhost:5133/swagger`

### Chạy Ứng Dụng
```bash
dotnet run
```

Ứng dụng sẽ chạy tại:
- HTTP: `http://localhost:5133`
- HTTPS: `https://localhost:7088`
- Swagger: `http://localhost:5133/swagger`

## Xử Lý Lỗi "File is locked"

Nếu gặp lỗi:
```
The file is locked by: "TAPHOAMMO-BACKEND (40380)"
```

**Giải pháp:**

1. **Sử dụng PowerShell Script:**
   ```powershell
   .\stop-app.ps1
   ```

2. **Hoặc dừng thủ công:**
   ```powershell
   # Tìm và dừng process
   Get-Process -Name "dotnet" | Stop-Process -Force
   
   # Hoặc tìm process theo port
   netstat -ano | findstr :5133
   # Sau đó dừng process bằng PID
   taskkill /PID <PID> /F
   ```

3. **Hoặc đóng cửa sổ terminal đang chạy ứng dụng**

Sau đó build lại:
```bash
dotnet build
dotnet run
```

## API Documentation

Xem file `API_DOCUMENTATION_FOR_ANGULAR.md` để biết chi tiết về các API endpoints và cách sử dụng từ Angular frontend.

## Tài Khoản Mẫu (Sau khi seed)

**User:**
- Email: `user1@example.com`
- Password: `123456`

**Seller:**
- Email: `seller1@example.com` / Password: `123456`
- Email: `seller2@example.com` / Password: `123456`

## Cấu Trúc Project

```
TAPHOAMMO-BACKEND/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   └── SeedController.cs
├── Models/              # Data Models
│   ├── User.cs
│   ├── Product.cs
│   └── DTOs/           # Data Transfer Objects
├── Services/            # Business Logic
│   ├── AuthService.cs
│   └── DataSeeder.cs
├── Data/               # Database Context
│   └── ApplicationDbContext.cs
├── Migrations/         # Database Migrations
└── Program.cs          # Application Entry Point
```

