@echo off
setlocal

set /p "STREAM_URL=Enter stream URL: "
if not defined STREAM_URL (
  echo Error: stream URL is required.
  pause
  exit /b 1
)

set FFMPEG=%~dp0ffmpeg\bin\ffmpeg.exe
if not exist "%FFMPEG%" call "%~dp0setup.bat"
if not exist "%FFMPEG%" (
  echo Error: ffmpeg not found.
  pause
  exit /b 1
)

set OUT_DIR=%~dp0buffer

:: Fresh buffer every launch
if exist "%OUT_DIR%" rmdir /s /q "%OUT_DIR%"
mkdir "%OUT_DIR%"

:: Build a simple M3U wrapper playlist apps can import.
(
  echo #EXTM3U
  echo #EXTINF:-1 tvg-id="live" tvg-name="live",live
  echo live.m3u8
) > "%OUT_DIR%\live.m3u"

echo Starting stream transcode to 360p...
echo Source: %STREAM_URL%
echo M3U wrapper: %OUT_DIR%\live.m3u
echo Output: %OUT_DIR%\live.m3u8
echo.

"%FFMPEG%" ^
  -i "%STREAM_URL%" ^
  -c:v libx264 ^
  -preset veryfast ^
  -b:v 260k ^
  -maxrate 340k ^
  -bufsize 520k ^
  -vf scale=-2:360 ^
  -c:a aac ^
  -b:a 128k ^
  -f hls ^
  -hls_time 6 ^
  -hls_list_size 60 ^
  -hls_flags delete_segments+program_date_time+independent_segments ^
  "%OUT_DIR%\live.m3u8"

echo.
echo Stream ended or errored. Press any key to close.
pause
