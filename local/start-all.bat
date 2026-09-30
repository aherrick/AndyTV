@echo off
setlocal

set "TAILSCALE_IP="
for /f "usebackq delims=" %%i in (`tailscale ip -4 2^>nul`) do (
	if not defined TAILSCALE_IP set "TAILSCALE_IP=%%i"
)

tailscale serve --https=443 off >nul 2>&1

echo.
if defined TAILSCALE_IP (
	echo   HLS: http://%TAILSCALE_IP%:5050/live.m3u8?session={32-hex}^&quality=320^&url=...
) else (
	echo   Tailscale IP not detected. Run: tailscale ip
)
echo.

if not exist "%~dp0ffmpeg\bin\ffmpeg.exe" call "%~dp0setup.bat"

python "%~dp0server.py"
pause
