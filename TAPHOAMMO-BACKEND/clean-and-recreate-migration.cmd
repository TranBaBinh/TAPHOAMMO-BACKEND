@echo off
echo ========================================
echo Cleaning old migrations...
echo ========================================
cd /d "C:\Users\letha\OneDrive\Documents\Kỳ6\TAPHOAMMO\backend\TAPHOAMMO-BACKEND\TAPHOAMMO-BACKEND"

if exist "Migrations\20260103190000_SeparateAuthInfoAndProductStats.cs" (
    del "Migrations\20260103190000_SeparateAuthInfoAndProductStats.cs"
    echo Deleted old migration file
)

if exist "Migrations\20260103174458_SeparateAuthInfoAndProductStats.Designer.cs" (
    del "Migrations\20260103174458_SeparateAuthInfoAndProductStats.Designer.cs"
    echo Deleted orphaned Designer file
)

echo.
echo ========================================
echo Creating new migration...
echo ========================================
dotnet ef migrations add SeparateAuthInfoAndProductStats

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo Migration created successfully!
    echo Now run: dotnet ef database update
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Migration creation failed!
    echo ========================================
)

pause

