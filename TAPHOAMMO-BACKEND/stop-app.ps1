# Script to stop TAPHOAMMO-BACKEND application
Write-Host "Đang tìm và dừng các process TAPHOAMMO-BACKEND..." -ForegroundColor Yellow

# Stop dotnet processes related to TAPHOAMMO
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like "*TAPHOAMMO*"
}

if ($processes) {
    foreach ($proc in $processes) {
        Write-Host "Đang dừng process: $($proc.Id) - $($proc.ProcessName)" -ForegroundColor Red
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Đã dừng tất cả process!" -ForegroundColor Green
} else {
    Write-Host "Không tìm thấy process nào đang chạy." -ForegroundColor Green
}

# Also try to stop by port
$port = 5133
$connections = netstat -ano | findstr ":$port"
if ($connections) {
    Write-Host "Đang kiểm tra port $port..." -ForegroundColor Yellow
    $connections | ForEach-Object {
        $parts = $_ -split '\s+'
        $pid = $parts[-1]
        if ($pid -match '^\d+$') {
            Write-Host "Đang dừng process sử dụng port $port (PID: $pid)" -ForegroundColor Red
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "Hoàn tất!" -ForegroundColor Green
Start-Sleep -Seconds 2

