@echo off
echo ========================================
echo Vehicle Save Fix Verification
echo ========================================
echo.

REM Check if build files exist
echo [1/3] Checking if build files exist...
if exist "C:\Users\prabu\Desktop\webpro\build\net452\vMenuClient.net.dll" (
    echo ✓ vMenuClient.net.dll found
) else (
    echo ✗ vMenuClient.net.dll NOT FOUND
    echo ERROR: Build files missing. Run build.bat first!
    pause
    exit /b 1
)

if exist "C:\Users\prabu\Desktop\webpro\build\net452\vMenuServer.net.dll" (
    echo ✓ vMenuServer.net.dll found
) else (
    echo ✗ vMenuServer.net.dll NOT FOUND
    pause
    exit /b 1
)
echo.

REM Check DLL file date
echo [2/3] Checking DLL build date...
for %%I in ("C:\Users\prabu\Desktop\webpro\build\net452\vMenuClient.net.dll") do (
    echo Build Date: %%~tI
    echo Size: %%~zI bytes
)
echo.

REM Show summary
echo [3/3] Fix Summary:
echo.
echo ✓ SaveVehicle changed from async void to async Task
echo ✓ Menu callbacks now properly await SaveVehicle
echo ✓ UpdateMenuAvailableCategories() called after save
echo ✓ Category field added to VehicleInfo struct
echo.
echo ========================================
echo NEXT STEPS:
echo ========================================
echo 1. Copy build\net452\*.* to your FiveM server
echo 2. Restart FiveM completely (or use: restart vMenu)
echo 3. Test: Save a vehicle and check if it appears
echo.
echo Build location: C:\Users\prabu\Desktop\webpro\build\net452\
echo Documentation: CRITICAL_FIX_README.md
echo.
pause
