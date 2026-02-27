@echo off
echo ============================================
echo  Bulent Oto Elektrik - Installer Builder
echo ============================================
echo.

REM Step 1: Publish application
echo [1/3] Publishing application...
cd /d "%~dp0.."
dotnet publish src\BulentOtoElektrik.App\BulentOtoElektrik.App.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false ^
  -o publish

if errorlevel 1 (
    echo PUBLISH FAILED!
    pause
    exit /b 1
)

echo.
echo [2/3] Cleaning user data from publish directory...
del /q publish\bulentoto.db 2>nul
del /q publish\export_settings.txt 2>nul
del /q publish\*.pdb 2>nul
rmdir /s /q publish\logs 2>nul
rmdir /s /q publish\backups 2>nul

echo.
echo [3/3] Compiling Inno Setup installer...
"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer\setup.iss

if errorlevel 1 (
    echo.
    echo INNO SETUP COMPILE FAILED!
    pause
    exit /b 1
)

echo.
echo ============================================
echo  BASARILI! Installer olusturuldu:
echo  installer\Output\BulentOtoElektrik_Kurulum_v1.0.0.exe
echo ============================================
pause
