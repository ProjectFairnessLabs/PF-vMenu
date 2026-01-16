@echo off
echo ========================================
echo Building PF-vMenu
echo ========================================
echo.

REM Restore NuGet packages
echo [1/3] Restoring NuGet packages...
dotnet restore "vMenu.sln"
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore NuGet packages
    pause
    exit /b %errorlevel%
)
echo.

REM Build the solution in Release mode
echo [2/3] Building solution (Release)...
dotnet build "vMenu.sln" --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b %errorlevel%
)
echo.

echo [3/3] Build completed successfully!
echo.
echo Output location: build\
echo.
pause
