@echo off
cd /d "%~dp0"
echo Starting Arabic Screen OCR Extractor in the background...
wscript.exe run.vbs
echo.
echo ========================================================
echo Arabic Screen OCR Extractor is running in your System Tray!
echo Shortcut: Press Alt + Shift + A anywhere on screen to snip.
echo ========================================================
timeout /t 3 >nul
