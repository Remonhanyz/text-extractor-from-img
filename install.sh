#!/usr/bin/env bash
# ==============================================================================
# Arabic & English Screen OCR Extractor - 1-Step Linux Installer
# Automates package installation, GNOME shortcuts, and service activation
# ==============================================================================

set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SNIPPER_SCRIPT="${ROOT_DIR}/src/linux/snipper.sh"
HISTORY_SCRIPT="${ROOT_DIR}/src/linux/history.sh"

echo "================================================================="
echo "   Arabic & English Screen Text Extractor - Linux Setup          "
echo "================================================================="

# 1. Auto-detect & install package dependencies
echo "[1/4] Checking system package dependencies..."
MISSING_PKGS=()

if [ -n "$WAYLAND_DISPLAY" ] || [ "$XDG_SESSION_TYPE" = "wayland" ]; then
    command -v grim >/dev/null 2>&1 || MISSING_PKGS+=("grim")
    command -v slurp >/dev/null 2>&1 || MISSING_PKGS+=("slurp")
    command -v wl-copy >/dev/null 2>&1 || MISSING_PKGS+=("wl-clipboard")
else
    command -v maim >/dev/null 2>&1 || MISSING_PKGS+=("maim")
    command -v xclip >/dev/null 2>&1 || MISSING_PKGS+=("xclip")
fi
command -v curl >/dev/null 2>&1 || MISSING_PKGS+=("curl")
command -v jq >/dev/null 2>&1 || MISSING_PKGS+=("jq")

if [ ${#MISSING_PKGS[@]} -gt 0 ]; then
    echo "[*] Installing missing utilities: ${MISSING_PKGS[*]}"
    if command -v apt-get >/dev/null 2>&1; then
        sudo apt-get update -qq && sudo apt-get install -y "${MISSING_PKGS[@]}"
    elif command -v dnf >/dev/null 2>&1; then
        sudo dnf install -y "${MISSING_PKGS[@]}"
    elif command -v pacman >/dev/null 2>&1; then
        sudo pacman -S --needed --noconfirm "${MISSING_PKGS[@]}"
    else
        echo "[!] Please install: ${MISSING_PKGS[*]}"
    fi
else
    echo "[✓] All screen capture & clipboard tools are installed."
fi

# 2. Make scripts executable
echo "[2/4] Setting execution permissions..."
chmod +x "${SNIPPER_SCRIPT}"
chmod +x "${HISTORY_SCRIPT}"

# 3. Install Node.js dependencies
echo "[3/4] Installing Node dependencies..."
if command -v npm >/dev/null 2>&1; then
    (cd "${ROOT_DIR}" && npm install --silent)
    echo "[✓] Node dependencies ready."
else
    echo "[!] Node.js / npm not found. Please install Node.js (v18+)."
fi

# 4. Auto-bind Keyboard Shortcuts in GNOME via gsettings
echo "[4/4] Configuring keyboard shortcuts..."
if command -v gsettings >/dev/null 2>&1; then
    SCHEMA="org.gnome.settings-daemon.plugins.media-keys"
    KB_PATH="/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings"

    # Add custom bindings
    gsettings set ${SCHEMA} custom-keybindings "['${KB_PATH}/arabic-ocr-snip/', '${KB_PATH}/arabic-ocr-hist/']"
    
    # 1. Alt + Shift + A (Snip)
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ name 'Screen OCR Extractor'
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ command "${SNIPPER_SCRIPT}"
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ binding '<Alt><Shift>a'

    # 2. Alt + Shift + H (History)
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ name 'Screen OCR History'
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ command "${HISTORY_SCRIPT}"
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ binding '<Alt><Shift>h'

    echo "[✓] Keyboard shortcuts bound in GNOME:"
    echo "    - Alt + Shift + A : Snip screen & extract text"
    echo "    - Alt + Shift + H : Open Text History"
fi

# 5. Systemd User Service (Background Daemon)
SYSTEMD_DIR="${HOME}/.config/systemd/user"
mkdir -p "${SYSTEMD_DIR}"
cat <<EOF > "${SYSTEMD_DIR}/arabic-ocr.service"
[Unit]
Description=Arabic & English Screen OCR Extractor Daemon
After=network.target

[Service]
Type=simple
WorkingDirectory=${ROOT_DIR}
ExecStart=$(which node) ${ROOT_DIR}/src/server/server.js
Restart=on-failure
RestartSec=2

[Install]
WantedBy=default.target
EOF

if command -v systemctl >/dev/null 2>&1; then
    systemctl --user daemon-reload
    systemctl --user enable arabic-ocr.service
    systemctl --user restart arabic-ocr.service
    echo "[✓] Background service enabled & started via systemd."
fi

echo ""
echo "================================================================="
echo " 🎉 Installation Complete! Everything is ready."
echo " Press Alt + Shift + A anywhere on screen to snip & extract text!"
echo "================================================================="
