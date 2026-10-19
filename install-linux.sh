#!/usr/bin/env bash
# ==============================================================================
# Arabic & English Screen OCR Extractor - Linux Installer
# ==============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "=== Installing Arabic & English Screen OCR Extractor for Linux ==="

# 1. Make scripts executable
chmod +x "${SCRIPT_DIR}/snipper.sh"
chmod +x "${SCRIPT_DIR}/history.sh"
echo "[✓] Executable permissions set."

# 2. Install NPM dependencies
if command -v npm >/dev/null 2>&1; then
    echo "[*] Installing node dependencies..."
    (cd "${SCRIPT_DIR}" && npm install --silent)
    echo "[✓] Dependencies installed."
else
    echo "[!] Warning: npm not found. Please install Node.js (v18+) to run the OCR service."
fi

# 3. Create Desktop Entry in ~/.local/share/applications/
DESKTOP_DIR="${HOME}/.local/share/applications"
mkdir -p "${DESKTOP_DIR}"
cat <<EOF > "${DESKTOP_DIR}/arabic-text-extractor.desktop"
[Desktop Entry]
Name=Arabic & English Screen Text Extractor
Comment=Extract Arabic and English text from any screen selection
Exec=${SCRIPT_DIR}/snipper.sh
Icon=${SCRIPT_DIR}/app.ico
Terminal=false
Type=Application
Categories=Utility;Office;
Keywords=OCR;Arabic;English;Extractor;Text;
Actions=History;

[Desktop Action History]
Name=Open Text Extraction History
Exec=${SCRIPT_DIR}/history.sh
EOF
chmod +x "${DESKTOP_DIR}/arabic-text-extractor.desktop"
echo "[✓] Desktop entry created at ${DESKTOP_DIR}/arabic-text-extractor.desktop"

# 4. Create Systemd User Service for background auto-start on boot
SYSTEMD_USER_DIR="${HOME}/.config/systemd/user"
mkdir -p "${SYSTEMD_USER_DIR}"
cat <<EOF > "${SYSTEMD_USER_DIR}/arabic-ocr.service"
[Unit]
Description=Arabic & English Screen OCR Extractor Service
After=network.target

[Service]
Type=simple
WorkingDirectory=${SCRIPT_DIR}
ExecStart=$(which node) ${SCRIPT_DIR}/ocr-service.js
Restart=on-failure
RestartSec=3

[Install]
WantedBy=default.target
EOF

if command -v systemctl >/dev/null 2>&1; then
    systemctl --user daemon-reload
    systemctl --user enable arabic-ocr.service
    systemctl --user start arabic-ocr.service
    echo "[✓] Systemd user service enabled and started (auto-starts on Linux login)."
fi

echo ""
echo "================================================================="
echo " Installation Complete! 🎉"
echo "================================================================="
echo "How to bind keyboard shortcuts:"
echo ""
echo "1. GNOME / Ubuntu / Fedora:"
echo "   Settings -> Keyboard -> Keyboard Shortcuts -> Custom Shortcuts -> Add (+)"
echo "   - Name: Snip Text (OCR)"
echo "   - Command: ${SCRIPT_DIR}/snipper.sh"
echo "   - Shortcut: Alt + Shift + A"
echo ""
echo "   Add second shortcut:"
echo "   - Name: OCR History"
echo "   - Command: ${SCRIPT_DIR}/history.sh"
echo "   - Shortcut: Alt + Shift + H"
echo ""
echo "2. Required Screen Capture Tools:"
echo "   - Wayland: sudo apt install grim slurp wl-clipboard"
echo "   - X11:     sudo apt install maim xclip"
echo "================================================================="
