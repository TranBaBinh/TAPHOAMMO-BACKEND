# Script để xóa secrets khỏi git history
# ⚠️ CẢNH BÁO: Script này sẽ rewrite git history!
# Chỉ chạy nếu bạn chắc chắn và đã backup repository

param(
    [switch]$Force
)

Write-Host "⚠️  CẢNH BÁO: Script này sẽ rewrite toàn bộ git history!" -ForegroundColor Red
Write-Host "   Điều này sẽ thay đổi tất cả commit hashes." -ForegroundColor Yellow
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Bạn có chắc chắn muốn tiếp tục? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "Đã hủy." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "`n📦 Tạo backup branch..." -ForegroundColor Cyan
git branch backup-before-cleanup-$(Get-Date -Format "yyyyMMdd-HHmmss")

Write-Host "`n🧹 Xóa file có secrets khỏi git history..." -ForegroundColor Cyan
Write-Host "   (Quá trình này có thể mất vài phút...)`n" -ForegroundColor Gray

# Xóa các file có secrets khỏi toàn bộ history
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.json TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.Development.json TAPHOAMMO-BACKEND/appsettings.json TAPHOAMMO-BACKEND/appsettings.Development.json" `
  --prune-empty --tag-name-filter cat -- --all

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Đã xóa secrets khỏi git history!" -ForegroundColor Green
    Write-Host "`n📝 Các bước tiếp theo:" -ForegroundColor Cyan
    Write-Host "   1. Kiểm tra lại: git log --all --full-history -- '**/appsettings*.json'" -ForegroundColor Gray
    Write-Host "   2. Force push: git push origin --force --all" -ForegroundColor Gray
    Write-Host "   3. ⚠️  Rotate tất cả secrets sau khi push!" -ForegroundColor Red
} else {
    Write-Host "`n❌ Có lỗi xảy ra!" -ForegroundColor Red
    Write-Host "   Bạn có thể restore từ backup branch nếu cần." -ForegroundColor Yellow
}

Write-Host "`n💡 Tip: Nếu script chạy chậm, có thể dùng BFG Repo-Cleaner thay thế." -ForegroundColor Yellow

