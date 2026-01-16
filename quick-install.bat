@echo off
echo ========================================
echo vMenu Vehicle Save Fix - Quick Install
echo ========================================
echo.

REM Ask for server path
set /p serverpath="Enter your FiveM server path (e.g., C:\FXServer\server-data\resources\vMenu): "

if not exist "%serverpath%" (
    echo ERROR: Path does not exist!
    echo Please check the path and try again.
    pause
    exit /b 1
)

echo.
echo Target path: %serverpath%
echo.
echo Building latest version...
dotnet build vMenu.sln --configuration Release --no-restore

if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Copying files to server...

REM Backup old files
if exist "%serverpath%\vMenuClient.net.dll" (
    echo Creating backup...
    copy "%serverpath%\vMenuClient.net.dll" "%serverpath%\vMenuClient.net.dll.backup" >nul
    copy "%serverpath%\vMenuServer.net.dll" "%serverpath%\vMenuServer.net.dll.backup" >nul
)

REM Copy new files
echo Copying vMenuClient.net.dll...
copy "C:\Users\prabu\Desktop\webpro\build\net452\vMenuClient.net.dll" "%serverpath%\" /Y

echo Copying vMenuServer.net.dll...
copy "C:\Users\prabu\Desktop\webpro\build\net452\vMenuServer.net.dll" "%serverpath%\" /Y

echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo IMPORTANT NEXT STEPS:
echo 1. Close FiveM completely (if running)
echo 2. Delete cache: %%localappdata%%\FiveM\FiveM.app\cache\
echo 3. Restart your FiveM server
echo 4. Join server and test vehicle save
echo.
echo Backup files created:
echo - vMenuClient.net.dll.backup
echo - vMenuServer.net.dll.backup
echo.
pause
