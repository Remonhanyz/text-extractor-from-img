# Arabic & English Screen OCR Text Extractor 🔍

A lightweight, high-performance screen text extractor and OCR tool for **Windows and Linux**, inspired by Microsoft PowerToys Text Extractor, designed with full **Arabic and English** support and running **100% offline with zero Administrator privileges**.

---

## ✨ Features

- ⚡ **Ultra-Fast Local OCR**: Uses a pre-warmed neural engine (`tesseract.js` with `ara` + `eng`) delivering recognition in **30–70 ms**.
- 📴 **100% Offline**: Neural models (`ara.traineddata` & `eng.traineddata`) are stored locally. Zero cloud dependency, zero telemetry, total privacy.
- 🎯 **PowerToys-Style Screen Snip**: Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> to dim your monitors, drag a selection box around any text, and instantly copy it to your clipboard.
- 📜 **Searchable Text History**:
  - **Windows**: Native desktop History Window (<kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd>).
  - **Linux / Cross-Platform**: Web Dashboard at `http://localhost:48821/history` or run `./history.sh`.
- 🚀 **Start with Windows & Linux Toggle**:
  - **Windows**: Right-click tray icon &rarr; `[✓] Start with Windows`.
  - **Linux**: Auto-installed systemd user service (`systemctl --user enable arabic-ocr`).
- 🎨 **Custom App Icon**: Clean custom app icon embedded into the executable, desktop shortcut, and system tray.
- 🐧 **Cross-Platform**: Full native support for Windows 10/11 and Linux (Wayland & X11).

---

## ⌨️ Shortcuts

| Shortcut | Action | Windows | Linux |
|---|---|---|---|
| <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> | Snip screen and extract text to clipboard | Supported (Global Hotkey) | Supported (via `snipper.sh`) |
| <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd> | Open Text Extraction History | Supported (Desktop Window) | Supported (via `history.sh`) |
| <kbd>Esc</kbd> | Cancel active snipping overlay | Supported | Supported |

---

## 🚀 Getting Started

### 🪟 Windows Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/Remonhanyz/arabic-text-extractor.git
   cd arabic-text-extractor
   ```
2. Run `start.bat` or double-click `run.vbs`.
3. Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> anywhere on screen!

---

### 🐧 Linux Setup

#### 1. System Requirements:
- **Node.js** (v18+)
- Capture & Clipboard utilities:
  - **On Wayland** (Ubuntu 22.04+, Fedora, GNOME 40+):
    ```bash
    sudo apt install grim slurp wl-clipboard curl
    # Or on Arch: sudo pacman -S grim slurp wl-clipboard curl
    ```
  - **On X11** (XFCE, KDE X11, i3):
    ```bash
    sudo apt install maim xclip curl
    # Or on Arch: sudo pacman -S maim xclip curl
    ```

#### 2. Run the 1-Step Linux Installer:
```bash
git clone https://github.com/Remonhanyz/arabic-text-extractor.git
cd arabic-text-extractor
chmod +x install-linux.sh
./install-linux.sh
```

#### 3. Bind Keyboard Shortcut on Linux:
- **GNOME (Ubuntu / Fedora)**:
  1. Open **Settings &rarr; Keyboard &rarr; View and Customize Shortcuts &rarr; Custom Shortcuts &rarr; Add (+)**.
  2. Set Name: `Screen Text OCR`
  3. Set Command: `/path/to/arabic-text-extractor/snipper.sh`
  4. Set Shortcut: <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd>
- **KDE Plasma**:
  - System Settings &rarr; Shortcuts &rarr; Custom Shortcuts &rarr; Edit &rarr; New &rarr; Global Shortcut &rarr; Command/URL.
- **Hyprland / Sway / i3**:
  ```ini
  # Sway / Hyprland config:
  bindsym Mod1+Shift+a exec /path/to/arabic-text-extractor/snipper.sh
  bindsym Mod1+Shift+h exec /path/to/arabic-text-extractor/history.sh
  ```

---

## 🛠️ Project Structure

- `ArabicSnipper.exe` - Windows native C# overlay snipper application.
- `snipper.sh` - Linux native Wayland (`grim` + `slurp`) & X11 (`maim`/`scrot`) snipper.
- `ocr-service.js` - Background local Node.js service running offline Tesseract worker + Web History GUI.
- `history.sh` - Linux launcher to open History Web UI.
- `install-linux.sh` - Automated Linux installer (systemd service + desktop entry).
- `run.vbs` - Silent background runner for Windows.
- `ara.traineddata` & `eng.traineddata` - Offline neural network language models.
- `history.json` - Persistent storage for extracted snippets.
- `app.ico` - Multi-resolution application icon.
