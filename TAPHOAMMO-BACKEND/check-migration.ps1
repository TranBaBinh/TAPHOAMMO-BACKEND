# Script to check and run migration
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Checking migrations..." -ForegroundColor Green
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
    
    Write-Host "`nListing migrations..." -ForegroundColor Cyan
    dotnet ef migrations list
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "To run migration, use:" -ForegroundColor Yellow
    Write-Host "dotnet ef database update" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
} else {
    Write-Host "Could not find .csproj file!" -ForegroundColor Red
}

