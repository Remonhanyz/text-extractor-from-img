@echo off
cd /d "%~dp0"
echo Starting Arabic & English Screen OCR Extractor...
wscript.exe run.vbs
echo.
echo ========================================================
echo Arabic & English Screen OCR Extractor is running!
echo Shortcut: Press Alt + Shift + A anywhere on screen.
echo ========================================================
ping 127.0.0.1 -n 3 >nul
