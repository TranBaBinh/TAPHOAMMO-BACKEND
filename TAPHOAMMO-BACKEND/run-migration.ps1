# Script to run database migration
# Run this script from the TAPHOAMMO-BACKEND directory

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running database migration..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Script directory: $scriptPath" -ForegroundColor Yellow

# Try to find .csproj file in current directory or subdirectories
$csprojFile = $null

# First try current directory
$csprojFile = Get-ChildItem -Path $scriptPath -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1

# If not found, try subdirectories
if (-not $csprojFile) {
    $csprojFile = Get-ChildItem -Path $scriptPath -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($csprojFile) {
    Write-Host "Found project file: $($csprojFile.FullName)" -ForegroundColor Green
    
    # Change to the directory containing the .csproj file
    $projectDir = $csprojFile.DirectoryName
    Write-Host "Changing to directory: $projectDir" -ForegroundColor Yellow
    Set-Location $projectDir
    
    try {
        Write-Host "Running: dotnet ef database update" -ForegroundColor Cyan
        # Run the migration
        dotnet ef database update
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "Migration completed successfully!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Cyan
        } else {
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            Write-Host "========================================" -ForegroundColor Cyan
        }
    }
    catch {
        Write-Host "Error running migration: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "Could not find .csproj file in: $scriptPath" -ForegroundColor Red
    Write-Host "Please run this script from the TAPHOAMMO-BACKEND directory" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

