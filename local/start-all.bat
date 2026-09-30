@echo off
if not exist "%~dp0ffmpeg\bin\ffmpeg.exe" call "%~dp0setup.bat"
python "%~dp0server.py"
pause
