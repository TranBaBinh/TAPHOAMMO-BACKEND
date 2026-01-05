@echo off
cd /d "C:\Users\letha\OneDrive\Documents\Kỳ6\TAPHOAMMO\backend\TAPHOAMMO-BACKEND\TAPHOAMMO-BACKEND"
dotnet build 2>&1 | findstr /i "error"
if %errorlevel% equ 0 (
    echo.
    echo Found build errors. Running full build to see details...
    dotnet build
) else (
    echo No build errors found.
)

