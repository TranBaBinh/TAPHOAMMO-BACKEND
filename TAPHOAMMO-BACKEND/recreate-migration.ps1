# Script to recreate migration
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Recreating migration..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptPath "TAPHOAMMO-BACKEND"

if (-not (Test-Path $projectDir)) {
    $projectDir = $scriptPath
}

Write-Host "Project directory: $projectDir" -ForegroundColor Yellow

# Find .csproj file
$csprojFile = Get-ChildItem -Path $projectDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($csprojFile) {
    Write-Host "Found project file: $($csprojFile.FullName)" -ForegroundColor Green
    Set-Location $csprojFile.DirectoryName
    
    Write-Host "`nRemoving old migration file..." -ForegroundColor Yellow
    Remove-Item "Migrations\20260103190000_SeparateAuthInfoAndProductStats.cs" -ErrorAction SilentlyContinue
    
    Write-Host "Creating new migration..." -ForegroundColor Cyan
    dotnet ef migrations add SeparateAuthInfoAndProductStats
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n========================================" -ForegroundColor Cyan
        Write-Host "Migration created successfully!" -ForegroundColor Green
        Write-Host "Now run: dotnet ef database update" -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Cyan
    } else {
        Write-Host "`n========================================" -ForegroundColor Cyan
        Write-Host "Migration creation failed!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Cyan
    }
} else {
    Write-Host "Could not find .csproj file!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

