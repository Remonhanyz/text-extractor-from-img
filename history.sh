#!/usr/bin/env bash
# ==============================================================================
# Open Arabic & English OCR History in Default Linux Web Browser
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PORT=48821

# Ensure OCR service is running
if ! curl -s "http://127.0.0.1:${PORT}/health" >/dev/null 2>&1; then
    echo "[*] Starting OCR service..."
    nohup node "${SCRIPT_DIR}/ocr-service.js" >/dev/null 2>&1 &
    sleep 1
fi

if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "http://127.0.0.1:${PORT}/history"
elif command -v sensible-browser >/dev/null 2>&1; then
    sensible-browser "http://127.0.0.1:${PORT}/history"
else
    echo "Open in your browser: http://127.0.0.1:${PORT}/history"
fi
