# PowerShell script để chạy SQL script tạo bảng UserBankInfos
# Yêu cầu: sqlcmd phải được cài đặt

Write-Host "Creating UserBankInfos table..." -ForegroundColor Yellow

# Đọc connection string từ appsettings.Development.json
$appsettingsPath = "TAPHOAMMO-BACKEND\appsettings.Development.json"
if (-not (Test-Path $appsettingsPath)) {
    $appsettingsPath = "appsettings.Development.json"
}

if (-not (Test-Path $appsettingsPath)) {
    Write-Host "Error: Cannot find appsettings.Development.json" -ForegroundColor Red
    Write-Host "Please run this script from the TAPHOAMMO-BACKEND directory" -ForegroundColor Yellow
    exit 1
}

$json = Get-Content $appsettingsPath | ConvertFrom-Json
$connectionString = $json.ConnectionStrings.DefaultConnection

if (-not $connectionString) {
    Write-Host "Error: Cannot find ConnectionStrings:DefaultConnection in appsettings" -ForegroundColor Red
    Write-Host "Please check your appsettings.Development.json file" -ForegroundColor Yellow
    exit 1
}

Write-Host "Connection String found!" -ForegroundColor Green
Write-Host "Executing SQL script..." -ForegroundColor Yellow

# Đọc SQL script
$sqlScript = Get-Content "create-userbankinfo-table.sql" -Raw

# Kiểm tra sqlcmd có tồn tại không
$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue

if (-not $sqlcmdPath) {
    Write-Host "Error: sqlcmd is not installed or not in PATH" -ForegroundColor Red
    Write-Host "" -ForegroundColor Yellow
    Write-Host "Please run the SQL script manually:" -ForegroundColor Yellow
    Write-Host "1. Open SQL Server Management Studio or Azure Data Studio" -ForegroundColor Yellow
    Write-Host "2. Connect to your database" -ForegroundColor Yellow
    Write-Host "3. Open and execute: create-userbankinfo-table.sql" -ForegroundColor Yellow
    exit 1
}

# Chạy SQL script
try {
    $sqlScript | sqlcmd -S $connectionString -d $($connectionString -split ';' | Where-Object { $_ -like 'Database=*' } | ForEach-Object { $_ -replace 'Database=', '' })
    Write-Host "SQL script executed successfully!" -ForegroundColor Green
} catch {
    Write-Host "Error executing SQL script: $_" -ForegroundColor Red
    Write-Host "" -ForegroundColor Yellow
    Write-Host "Please run the SQL script manually:" -ForegroundColor Yellow
    Write-Host "1. Open SQL Server Management Studio or Azure Data Studio" -ForegroundColor Yellow
    Write-Host "2. Connect to your database" -ForegroundColor Yellow
    Write-Host "3. Open and execute: create-userbankinfo-table.sql" -ForegroundColor Yellow
    exit 1
}

