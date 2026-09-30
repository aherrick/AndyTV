@echo off
setlocal

set "TAILSCALE_IP="
for /f "usebackq delims=" %%i in (`tailscale ip -4 2^>nul`) do (
	if not defined TAILSCALE_IP set "TAILSCALE_IP=%%i"
)

tailscale serve --https=443 off >nul 2>&1

echo.
if defined TAILSCALE_IP (
	echo   POST /start: http://%TAILSCALE_IP%:5050/start?url=...
	echo   HLS:         http://%TAILSCALE_IP%:5050/{id}/live.m3u8
) else (
	echo   Tailscale IP not detected. Run: tailscale ip
)
echo.

python "%~dp0server.py"
pause
