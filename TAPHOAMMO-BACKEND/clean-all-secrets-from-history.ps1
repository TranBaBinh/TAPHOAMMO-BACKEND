# Script để xóa TẤT CẢ secrets khỏi git history
# ⚠️ CẢNH BÁO: Script này sẽ rewrite toàn bộ git history!
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
$backupBranch = "backup-before-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
git branch $backupBranch
Write-Host "   Backup branch: $backupBranch" -ForegroundColor Gray

Write-Host "`n🧹 Xóa TẤT CẢ file có secrets khỏi git history..." -ForegroundColor Cyan
Write-Host "   (Quá trình này có thể mất vài phút...)`n" -ForegroundColor Gray

# Danh sách tất cả file cần xóa khỏi history
$filesToRemove = @(
    "TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.json",
    "TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.Development.json",
    "TAPHOAMMO-BACKEND/appsettings.json",
    "TAPHOAMMO-BACKEND/appsettings.Development.json"
)

# Tạo filter command
$filterCmd = "git rm --cached --ignore-unmatch " + ($filesToRemove -join " ")

# Chạy filter-branch từ root của repository
$repoRoot = git rev-parse --show-toplevel
Set-Location $repoRoot

# Set environment variable để bỏ qua warning
$env:FILTER_BRANCH_SQUELCH_WARNING = "1"

# Xóa file khỏi toàn bộ history
git filter-branch --force --index-filter $filterCmd --prune-empty --tag-name-filter cat -- --all

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Đã xóa file config khỏi git history!" -ForegroundColor Green
    
    Write-Host "`n🧹 Xóa file documentation có secrets khỏi history..." -ForegroundColor Cyan
    
    # Xóa commit có secrets trong documentation (commit 23734a6)
    # Sử dụng filter-branch để thay thế nội dung file
    git filter-branch --force --tree-filter '
        if [ -f "TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md" ]; then
            sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md
            sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md
            sed -i "s/LAPTOP-715LSPJN/YOUR_SERVER/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md
            sed -i "s/LinhTDHE186757@fpt\.edu\.vn/YOUR_EMAIL@domain.com/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md
            sed -i "s/Linhtran1212@@/YOUR_PASSWORD/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md
        fi
        if [ -f "TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md" ]; then
            sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md
            sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md
            sed -i "s/LAPTOP-715LSPJN/YOUR_SERVER/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md
            sed -i "s/LinhTDHE186757@fpt\.edu\.vn/YOUR_EMAIL@domain.com/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md
            sed -i "s/Linhtran1212@@/YOUR_PASSWORD/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md
        fi
        if [ -f "TAPHOAMMO-BACKEND/setup-user-secrets.ps1" ]; then
            sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1
            sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1
            sed -i "s/LAPTOP-715LSPJN/YOUR_SERVER/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1
            sed -i "s/LinhTDHE186757@fpt\.edu\.vn/YOUR_EMAIL@domain.com/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1
            sed -i "s/Linhtran1212@@/YOUR_PASSWORD/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1
        fi
    ' --prune-empty --tag-name-filter cat -- --all
    
    Write-Host "`n✅ Đã xóa secrets khỏi documentation trong git history!" -ForegroundColor Green
    
    Write-Host "`n🧹 Cleanup git references..." -ForegroundColor Cyan
    git for-each-ref --format="delete %(refname)" refs/original | git update-ref --stdin
    git reflog expire --expire=now --all
    git gc --prune=now --aggressive
    
    Write-Host "`n✅ Hoàn tất! Git history đã được làm sạch." -ForegroundColor Green
    Write-Host "`n📝 Các bước tiếp theo:" -ForegroundColor Cyan
    Write-Host "   1. Kiểm tra lại: git log --all --full-history -- '**/appsettings*.json'" -ForegroundColor Gray
    Write-Host "   2. Force push: git push origin --force --all" -ForegroundColor Gray
    Write-Host "   3. ⚠️  Rotate tất cả secrets sau khi push!" -ForegroundColor Red
    Write-Host "`n💡 Backup branch: $backupBranch" -ForegroundColor Yellow
} else {
    Write-Host "`n❌ Có lỗi xảy ra!" -ForegroundColor Red
    Write-Host "   Bạn có thể restore từ backup branch: $backupBranch" -ForegroundColor Yellow
    Write-Host "   git checkout $backupBranch" -ForegroundColor Gray
}

