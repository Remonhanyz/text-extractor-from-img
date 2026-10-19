#!/usr/bin/env bash
# ==============================================================================
# Open Text Extraction History Web UI in default Linux browser
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PORT=48821

if ! curl -s "http://127.0.0.1:${PORT}/health" >/dev/null 2>&1; then
    echo "[*] Starting OCR server..."
    nohup node "${ROOT_DIR}/src/server/server.js" >/dev/null 2>&1 &
    sleep 1
fi

if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "http://127.0.0.1:${PORT}/history"
else
    echo "Open in your browser: http://127.0.0.1:${PORT}/history"
fi
