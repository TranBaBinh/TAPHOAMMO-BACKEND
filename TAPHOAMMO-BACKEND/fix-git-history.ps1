# Script đơn giản để xóa secrets khỏi git history
# Sử dụng git filter-branch để thay thế secrets trong toàn bộ history

Write-Host "🔧 Xóa secrets khỏi git history..." -ForegroundColor Cyan
Write-Host "⚠️  CẢNH BÁO: Script này sẽ rewrite git history!" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Bạn có chắc chắn muốn tiếp tục? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Đã hủy." -ForegroundColor Yellow
    exit 0
}

# Tạo backup
Write-Host "`n📦 Tạo backup branch..." -ForegroundColor Cyan
$backupBranch = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
git branch $backupBranch
Write-Host "   Backup: $backupBranch" -ForegroundColor Gray

# Set environment variable để bỏ qua warning
$env:FILTER_BRANCH_SQUELCH_WARNING = "1"

Write-Host "`n🧹 Đang xóa secrets khỏi history (có thể mất vài phút)..." -ForegroundColor Cyan

# Sử dụng git filter-branch để thay thế secrets trong tất cả commits
# Thay thế Google Client ID
git filter-branch --force --tree-filter '
    if [ -f "TAPHOAMMO-BACKEND/QUICK_FIX.md" ]; then
        sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/QUICK_FIX.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md" ]; then
        sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md" ]; then
        sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/setup-user-secrets.ps1" ]; then
        sed -i "s/455053755634-5q6m7osgnus5n6oa4fot14fs75r72d3j\.apps\.googleusercontent\.com/YOUR_GOOGLE_CLIENT_ID/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1 2>/dev/null || true
    fi
' --prune-empty --tag-name-filter cat -- --all

# Thay thế Google Client Secret
git filter-branch --force --tree-filter '
    if [ -f "TAPHOAMMO-BACKEND/QUICK_FIX.md" ]; then
        sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/QUICK_FIX.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md" ]; then
        sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/FIX_GIT_SECRETS.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md" ]; then
        sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/SECRETS_MANAGEMENT.md 2>/dev/null || true
    fi
    if [ -f "TAPHOAMMO-BACKEND/setup-user-secrets.ps1" ]; then
        sed -i "s/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/YOUR_GOOGLE_CLIENT_SECRET/g" TAPHOAMMO-BACKEND/setup-user-secrets.ps1 2>/dev/null || true
    fi
' --prune-empty --tag-name-filter cat -- --all

# Xóa file config có secrets khỏi history
git filter-branch --force --index-filter '
    git rm --cached --ignore-unmatch TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.json 2>/dev/null || true
    git rm --cached --ignore-unmatch TAPHOAMMO-BACKEND/bin/Debug/net8.0/appsettings.Development.json 2>/dev/null || true
    git rm --cached --ignore-unmatch TAPHOAMMO-BACKEND/appsettings.json 2>/dev/null || true
    git rm --cached --ignore-unmatch TAPHOAMMO-BACKEND/appsettings.Development.json 2>/dev/null || true
' --prune-empty --tag-name-filter cat -- --all

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Đã xóa secrets khỏi git history!" -ForegroundColor Green
    
    Write-Host "`n🧹 Cleanup git references..." -ForegroundColor Cyan
    git for-each-ref --format="delete %(refname)" refs/original | git update-ref --stdin 2>$null
    git reflog expire --expire=now --all
    git gc --prune=now --aggressive
    
    Write-Host "`n✅ Hoàn tất!" -ForegroundColor Green
    Write-Host "`n📝 Bước tiếp theo:" -ForegroundColor Cyan
    Write-Host "   git push origin --force --all" -ForegroundColor Yellow
    Write-Host "`n⚠️  Sau khi push, hãy rotate tất cả secrets!" -ForegroundColor Red
} else {
    Write-Host "`n❌ Có lỗi xảy ra!" -ForegroundColor Red
    Write-Host "   Restore từ backup: git checkout $backupBranch" -ForegroundColor Yellow
}

