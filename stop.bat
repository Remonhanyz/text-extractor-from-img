@echo off
echo Stopping Arabic Screen OCR Extractor...
taskkill /F /IM ArabicSnipper.exe 2>nul
powershell -Command "Get-Process node -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*ocr-service.js*' } | Stop-Process -Force" 2>nul
echo Done!
timeout /t 2 >nul
