@echo off
chcp 65001 >nul
echo =====================================
echo Preparing LegalDocSystem for GitHub
echo =====================================
echo.

echo [1/6] Checking for appsettings.Development.json ...
if exist "src\appsettings.Development.json" (
    echo    ⚠️  WARNING: src\appsettings.Development.json exists! 
    echo    ⚠️  It may contain local secrets and should NOT be committed.
    echo    ✅ Make sure it's in .gitignore
) else (
    echo    ✅ No appsettings.Development.json in repo (good)
)
echo.

echo [2/6] Checking for .env file...
if exist ".env" (
    echo    ⚠️  WARNING: .env file exists! It should NOT be committed.
    echo    ✅ Make sure it's in .gitignore
) else (
    echo    ✅ No .env file found (good)
)
echo.

echo [3/6] Checking for docs\Archive ...
if exist "docs\Archive" (
    echo    ⚠️  WARNING: docs\Archive exists! It should be moved outside the repo.
    echo    ✅ Make sure it's in .gitignore
) else (
    echo    ✅ No docs\Archive folder found (good)
)
echo.

echo [4/6] Checking .gitignore ...
if exist ".gitignore" (
    echo    ✅ .gitignore exists (good)
    findstr /C:"appsettings.Development.json" .gitignore >nul
    if %errorlevel% equ 0 (
        echo    ✅ appsettings.Development.json is excluded
    ) else (
        echo    ⚠️  appsettings.Development.json might not be excluded
    )
    findstr /C:".env" .gitignore >nul
    if %errorlevel% equ 0 (
        echo    ✅ .env files are excluded
    ) else (
        echo    ⚠️  .env files might not be excluded
    )
    findstr /C:"docs/Archive" .gitignore >nul
    if %errorlevel% equ 0 (
        echo    ✅ docs/Archive is excluded
    ) else (
        echo    ⚠️  docs/Archive might not be excluded
    )
) else (
    echo    ❌ .gitignore missing!
)
echo.

echo [5/6] Checking example config files ...
if exist "appsettings.Development.example.json" (
    echo    ✅ appsettings.Development.example.json exists (good)
) else (
    echo    ⚠️  appsettings.Development.example.json not found (recommended)
)

if exist ".env.example" (
    echo    ✅ .env.example exists (optional, good)
) else (
    echo    ⚠️  .env.example not found (optional)
)
echo.

echo [6/6] Checking for secrets in example files ...
findstr /C:"YourDevelopmentPasswordHere" "appsettings.Development.example.json" >nul
if %errorlevel% equ 0 (
    echo    ✅ Example file contains placeholder passwords (good)
) else (
    echo    ⚠️  Check if example file has real passwords!
)

findstr /C:"YourStrongPasswordHere" ".env.example" >nul 2>nul
if %errorlevel% equ 0 (
    echo    ✅ .env.example contains placeholder passwords (good)
) else (
    echo    ⚠️  Check if .env.example has real passwords!
)
echo.

echo =====================================
echo Preparation check complete!
echo =====================================
echo.
echo Next steps:
echo 1. Review .gitignore to ensure all sensitive files are excluded
echo 2. Make sure appsettings.Development.example.json has NO real passwords
echo 3. Run: git status
echo 4. Verify no secrets are being committed
echo.
pause


