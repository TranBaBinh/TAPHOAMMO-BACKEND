# Script để xóa các thư mục lồng nhau không cần thiết
# Chạy script này trong PowerShell

$projectPath = "TAPHOAMMO-BACKEND\TAPHOAMMO-BACKEND"

Write-Host "Đang dọn dẹp thư mục lồng nhau..." -ForegroundColor Yellow

# Xóa thư mục obj và bin
if (Test-Path "$projectPath\obj") {
    Write-Host "Xóa thư mục obj..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force "$projectPath\obj" -ErrorAction SilentlyContinue
}

if (Test-Path "$projectPath\bin") {
    Write-Host "Xóa thư mục bin..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force "$projectPath\bin" -ErrorAction SilentlyContinue
}

# Xóa các thư mục TAPHOAMMO-BACKEND lồng nhau trong TAPHOAMMO-BACKEND
$nestedPath = "$projectPath\TAPHOAMMO-BACKEND"
if (Test-Path $nestedPath) {
    Write-Host "Xóa thư mục lồng nhau TAPHOAMMO-BACKEND..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force $nestedPath -ErrorAction SilentlyContinue
}

Write-Host "Đã dọn dẹp xong!" -ForegroundColor Green
Write-Host ""
Write-Host "Bây giờ chạy:" -ForegroundColor Yellow
Write-Host "  cd $projectPath" -ForegroundColor White
Write-Host "  dotnet clean" -ForegroundColor White
Write-Host "  dotnet build" -ForegroundColor White

