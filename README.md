# Arabic & English Screen OCR Text Extractor 🔍

A lightweight, high-performance screen text extractor and OCR tool for **Windows and Linux**, inspired by Microsoft PowerToys Text Extractor, designed with full **Arabic and English** support and running **100% offline with zero Administrator privileges**.

---

## ✨ Features

- ⚡ **Ultra-Fast Local OCR**: Uses a pre-warmed neural engine (`tesseract.js` with `ara` + `eng`) delivering recognition in **30–70 ms**.
- 📴 **100% Offline**: Neural models (`ara.traineddata` & `eng.traineddata`) are stored locally. Zero cloud dependency, zero telemetry, total privacy.
- 🎯 **PowerToys-Style Screen Snip**: Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> to dim your monitors, drag a selection box around any text, and instantly copy it to your clipboard.
- 📜 **Searchable Text History**:
  - **Windows**: Native desktop History Window (<kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd>).
  - **Linux / Web**: Modern dark dashboard at `http://localhost:48821/history` (or run `./src/linux/history.sh`).
- 🚀 **Auto-Start on Boot**:
  - **Windows**: Right-click tray icon &rarr; `[✓] Start with Windows`.
  - **Linux**: Auto-installed systemd user service (`systemctl --user enable arabic-ocr`).
- 🎨 **Custom App Icon**: Clean custom app icon embedded into the executable, desktop shortcut, and system tray.
- 🐧 **1-Step Linux Setup**: Automated installer (`./install.sh`) that installs dependencies and binds GNOME shortcuts automatically.

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
   git clone https://github.com/Remonhanyz/text-extractor-from-img.git
   cd text-extractor-from-img
   ```
2. Double-click `start.bat` or `run.vbs`.
3. Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> anywhere on screen!

---

### 🐧 Linux Setup (1-Step Automated)

1. Clone the repository:
   ```bash
   git clone https://github.com/Remonhanyz/text-extractor-from-img.git
   cd text-extractor-from-img
   ```
2. Run the automated installer:
   ```bash
   chmod +x install.sh
   ./install.sh
   ```
3. That's it! The script automatically:
   - Detects your package manager (`apt`, `dnf`, `pacman`) and installs capture utilities (`grim`, `slurp`, `wl-clipboard`, `maim`, `xclip`).
   - Runs `npm install`.
   - Automatically registers <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> and <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd> in GNOME.
   - Activates the background daemon via `systemd`.
   - You can immediately press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd>!

---

## 📂 Project Architecture

```
text-extractor-from-img/
├── src/
│   ├── windows/                          # Windows Native C# Application
│   │   ├── Program.cs                    # Entry point & ApplicationContext
│   │   ├── OverlayForm.cs                # Fullscreen snipping & marquee selection UI
│   │   ├── HistoryForm.cs                # Desktop text extraction history UI
│   │   ├── HotkeyWindow.cs               # Win32 global hotkey registration
│   │   ├── HistoryManager.cs             # Local history read/write logic
│   │   ├── StartupManager.cs             # Registry startup toggle helper
│   │   └── build.bat                     # Build script to compile ArabicSnipper.exe
│   │
│   ├── linux/                            # Linux Native Shell Scripts
│   │   ├── snipper.sh                    # Wayland (grim+slurp) & X11 (maim) capture
│   │   └── history.sh                    # Web history browser launcher
│   │
│   ├── server/                           # Backend OCR Service (Node.js)
│   │   ├── server.js                     # HTTP API router
│   │   ├── ocr-worker.js                 # Tesseract.js worker initialization & recognition
│   │   ├── history-store.js              # History JSON storage module
│   │   └── web/                          # History Web Dashboard
│   │       ├── index.html                # Clean web GUI template
│   │       ├── style.css                 # Dark-mode styling
│   │       └── app.js                    # Client-side interactions & search
│   │
│   └── models/                           # Offline Tesseract Neural Models
│       ├── ara.traineddata               # Arabic LSTM model
│       └── eng.traineddata               # English LSTM model
│
├── assets/                               # Visual assets
│   ├── app.ico                           # Multi-resolution Windows icon
│   └── icon.jpg                          # Master app graphic
│
├── data/                                 # User data & persistent state
│   └── history.json                      # Persistent snippet history
│
├── bin/                                  # Compiled Windows Binaries
│   └── ArabicSnipper.exe                 # Native executable
│
├── install.sh                            # 1-Command Linux Auto-Installer
├── start.bat                             # Windows 1-click launcher
├── stop.bat                              # Windows 1-click stop script
├── run.vbs                               # Windows silent background runner
├── package.json                          # Node project manifest
├── package-lock.json
├── README.md                             # Documentation
└── .gitignore
```
