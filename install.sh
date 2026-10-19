#!/usr/bin/env bash
# ==============================================================================
# Arabic & English Screen OCR Extractor - 1-Step Linux Installer
# Automates package installation, reassignable GNOME shortcuts, and service activation
# ==============================================================================

set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SNIPPER_SCRIPT="${ROOT_DIR}/src/linux/snipper.sh"
HISTORY_SCRIPT="${ROOT_DIR}/src/linux/history.sh"
CONFIG_FILE="${ROOT_DIR}/data/config.json"

echo "================================================================="
echo "   Arabic & English Screen Text Extractor - Linux Setup          "
echo "================================================================="

# Helper to convert shortcut like "Alt+Shift+A" to GNOME "<Alt><Shift>a"
format_gnome_shortcut() {
    local raw="$1"
    local result=""
    IFS='+' read -ra PARTS <<< "$raw"
    for part in "${PARTS[@]}"; do
        part="$(echo "$part" | xargs)"
        case "$part" in
            Alt)   result="${result}<Alt>" ;;
            Ctrl|Control) result="${result}<Control>" ;;
            Shift) result="${result}<Shift>" ;;
            Win|Super)    result="${result}<Super>" ;;
            *)     result="${result}$(echo "$part" | tr '[:upper:]' '[:lower:]')" ;;
        esac
    done
    echo "$result"
}

# Read configured shortcuts from data/config.json if available
SNIP_RAW="Alt+Shift+A"
HIST_RAW="Alt+Shift+H"

if [ -f "$CONFIG_FILE" ] && command -v jq >/dev/null 2>&1; then
    SNIP_RAW=$(jq -r '.snipShortcut // "Alt+Shift+A"' "$CONFIG_FILE")
    HIST_RAW=$(jq -r '.historyShortcut // "Alt+Shift+H"' "$CONFIG_FILE")
elif [ -f "$CONFIG_FILE" ] && command -v node >/dev/null 2>&1; then
    SNIP_RAW=$(node -e "try{console.log(require('$CONFIG_FILE').snipShortcut || 'Alt+Shift+A')}catch(e){console.log('Alt+Shift+A')}")
    HIST_RAW=$(node -e "try{console.log(require('$CONFIG_FILE').historyShortcut || 'Alt+Shift+H')}catch(e){console.log('Alt+Shift+H')}")
fi

GNOME_SNIP=$(format_gnome_shortcut "$SNIP_RAW")
GNOME_HIST=$(format_gnome_shortcut "$HIST_RAW")

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
fi

# 4. Auto-bind Keyboard Shortcuts in GNOME via gsettings
echo "[4/4] Configuring keyboard shortcuts (${SNIP_RAW} & ${HIST_RAW})..."
if command -v gsettings >/dev/null 2>&1; then
    SCHEMA="org.gnome.settings-daemon.plugins.media-keys"
    KB_PATH="/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings"

    gsettings set ${SCHEMA} custom-keybindings "['${KB_PATH}/arabic-ocr-snip/', '${KB_PATH}/arabic-ocr-hist/']"
    
    # Snip Shortcut
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ name 'Screen OCR Extractor'
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ command "${SNIPPER_SCRIPT}"
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-snip/ binding "${GNOME_SNIP}"

    # History Shortcut
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ name 'Screen OCR History'
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ command "${HISTORY_SCRIPT}"
    gsettings set ${SCHEMA}.custom-keybinding:${KB_PATH}/arabic-ocr-hist/ binding "${GNOME_HIST}"

    echo "[✓] Shortcuts registered in GNOME:"
    echo "    - ${SNIP_RAW} : Snip screen & extract text"
    echo "    - ${HIST_RAW} : Open Text History"
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
    echo "[✓] Background service active via systemd."
fi

echo ""
echo "================================================================="
echo " 🎉 Installation Complete!"
echo " Press ${SNIP_RAW} anywhere on screen to snip & extract text!"
echo "================================================================="
