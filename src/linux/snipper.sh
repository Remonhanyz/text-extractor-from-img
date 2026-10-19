#!/usr/bin/env bash
# ==============================================================================
# Arabic & English Screen OCR Extractor (Linux)
# Supports Wayland (grim + slurp) & X11 (maim / scrot)
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PORT=48821
TMP_IMG="/tmp/ocr_snip_$(date +%s).png"

# 1. Ensure OCR background server is running
if ! curl -s "http://127.0.0.1:${PORT}/health" >/dev/null 2>&1; then
    echo "[*] Starting OCR server..."
    nohup node "${ROOT_DIR}/src/server/server.js" >/dev/null 2>&1 &
    for i in {1..20}; do
        if curl -s "http://127.0.0.1:${PORT}/health" | grep -q '"ready":true'; then
            break
        fi
        sleep 0.2
    done
fi

# 2. Capture Selection
if [ -n "$WAYLAND_DISPLAY" ] || [ "$XDG_SESSION_TYPE" = "wayland" ]; then
    if command -v grim >/dev/null 2>&1 && command -v slurp >/dev/null 2>&1; then
        SELECTION=$(slurp 2>/dev/null)
        if [ -z "$SELECTION" ]; then
            exit 0 # User pressed Esc
        fi
        grim -g "$SELECTION" "$TMP_IMG"
    else
        notify-send -u critical "OCR Extractor" "Wayland tools missing: sudo apt install grim slurp"
        exit 1
    fi
else
    # X11 Display
    if command -v maim >/dev/null 2>&1; then
        maim -s "$TMP_IMG" 2>/dev/null || exit 0
    elif command -v scrot >/dev/null 2>&1; then
        scrot -s "$TMP_IMG" 2>/dev/null || exit 0
    elif command -v import >/dev/null 2>&1; then
        import "$TMP_IMG" 2>/dev/null || exit 0
    else
        notify-send -u critical "OCR Extractor" "X11 tools missing: sudo apt install maim"
        exit 1
    fi
fi

if [ ! -f "$TMP_IMG" ] || [ ! -s "$TMP_IMG" ]; then
    exit 0
fi

# 3. High-Accuracy Image Preprocessing (if ImageMagick is available)
if command -v magick >/dev/null 2>&1; then
    magick "$TMP_IMG" -filter Lanczos -resize 220% -bordercolor white -border 24 -colorspace Gray "$TMP_IMG" 2>/dev/null || true
elif command -v convert >/dev/null 2>&1; then
    convert "$TMP_IMG" -filter Lanczos -resize 220% -bordercolor white -border 24 -colorspace Gray "$TMP_IMG" 2>/dev/null || true
fi

# 4. Read configured language mode (auto, ara, eng)
LANG_MODE="auto"
CONFIG_FILE="${ROOT_DIR}/data/config.json"
if [ -f "$CONFIG_FILE" ]; then
    CONFIG_LANG=$(node -e "try { const c = JSON.parse(fs.readFileSync(process.argv[1], 'utf8')); if (c.ocrLanguage) console.log(c.ocrLanguage); } catch(e){}" "$CONFIG_FILE" 2>/dev/null || echo "")
    if [ -n "$CONFIG_LANG" ]; then
        LANG_MODE="$CONFIG_LANG"
    fi
fi

# 5. Post image to OCR service
RESPONSE=$(curl -s -X POST --data-binary @"$TMP_IMG" -H "Content-Type: image/png" -H "X-OCR-Lang: ${LANG_MODE}" "http://127.0.0.1:${PORT}/ocr?lang=${LANG_MODE}")
rm -f "$TMP_IMG"

# 6. Parse response text
if command -v jq >/dev/null 2>&1; then
    TEXT=$(echo "$RESPONSE" | jq -r '.text // empty')
else
    TEXT=$(node -e "try { const d = JSON.parse(process.argv[1]); if (d.success && d.text) console.log(d.text); } catch(e){}" "$RESPONSE")
fi

if [ -z "$TEXT" ]; then
    notify-send "OCR Extractor" "No text detected in selected area."
    exit 0
fi

# 7. Copy text to Linux clipboard
if [ -n "$WAYLAND_DISPLAY" ] && command -v wl-copy >/dev/null 2>&1; then
    printf "%s" "$TEXT" | wl-copy
elif command -v xclip >/dev/null 2>&1; then
    printf "%s" "$TEXT" | xclip -selection clipboard
elif command -v xsel >/dev/null 2>&1; then
    printf "%s" "$TEXT" | xsel -b -i
fi

# 8. Show desktop notification
PREVIEW=$(echo "$TEXT" | head -n 2 | cut -c 1-80)
notify-send "Text Copied (${#TEXT} chars)" "$PREVIEW"
