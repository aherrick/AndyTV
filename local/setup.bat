@echo off
echo === IPTV Buffer Setup ===
echo.

:: Create output directory
mkdir "%~dp0buffer" 2>nul

:: Download FFmpeg (x86 release build from gyan.dev - official recommended source)
echo Downloading FFmpeg...
powershell -Command "Invoke-WebRequest -Uri 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' -OutFile '%~dp0ffmpeg.zip'"

echo Extracting FFmpeg...
powershell -Command "Expand-Archive -Path '%~dp0ffmpeg.zip' -DestinationPath '%~dp0' -Force"

:: Rename extracted folder to just "ffmpeg"
for /d %%i in ("%~dp0ffmpeg-*-essentials_build") do (
    if exist "%~dp0ffmpeg" rmdir /s /q "%~dp0ffmpeg"
    rename "%%i" "ffmpeg"
)

:: Clean up zip
del "%~dp0ffmpeg.zip" 2>nul

echo.
echo === Setup Complete ===
echo FFmpeg is at: %~dp0ffmpeg\bin\ffmpeg.exe
echo.
echo Next steps:
echo   1. Install Tailscale on this PC and your phone
echo   2. Edit stream.bat and set your M3U stream URL
echo   3. Run start-all.bat
echo.
pause
