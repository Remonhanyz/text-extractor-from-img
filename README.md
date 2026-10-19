# Arabic & English Screen OCR Text Extractor 🔍

A lightweight, high-performance screen text extractor and OCR tool for **Windows and Linux**, inspired by Microsoft PowerToys Text Extractor, designed with full **Arabic and English** support and running **100% offline with zero Administrator privileges**.

---

## ✨ Features

- ⚡ **Ultra-Fast Local OCR**: Uses a pre-warmed neural engine (`tesseract.js` with `ara` + `eng`) delivering recognition in **30–70 ms**.
- 📴 **100% Offline**: Neural models (`ara.traineddata` & `eng.traineddata`) are stored locally. Zero cloud dependency, zero telemetry, total privacy.
- 🎯 **PowerToys-Style Screen Snip**: Dim your monitors, drag a selection box around any text, and instantly copy it to your clipboard.
- ⌨️ **Reassignable Shortcuts**: Fully customizable keyboard shortcuts! Rebind via the **Shortcut Settings** GUI or `data/config.json`.
- 📜 **Searchable Text History**:
  - **Windows**: Native desktop History Window.
  - **Linux / Web**: Modern dark dashboard at `http://localhost:48821/history` (or run `./src/linux/history.sh`).
- 🚀 **Auto-Start on Boot**:
  - **Windows**: Right-click tray icon &rarr; `[✓] Start with Windows`.
  - **Linux**: Auto-installed systemd user service (`systemctl --user enable arabic-ocr`).
- 🎨 **Custom App Icon**: Clean custom app icon embedded into the executable, desktop shortcut, and system tray.
- 🐧 **1-Step Linux Setup**: Automated installer (`./install.sh`) that installs dependencies and binds shortcuts automatically.

---

## ⌨️ Shortcuts (Fully Customizable)

| Action | Default Shortcut | Customization |
|---|---|---|
| **Snip Screen & Extract Text** | <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> | Tray Icon &rarr; **Shortcut Settings...** or edit `data/config.json` |
| **Open Text History Window** | <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd> | Tray Icon &rarr; **Shortcut Settings...** or edit `data/config.json` |
| **Cancel Selection / Close Dialog** | <kbd>Esc</kbd> | Fixed standard key |

### How to Change Shortcuts:
- **On Windows**: Right-click the System Tray icon &rarr; select **Shortcut Settings...** &rarr; click inside any shortcut box and press your preferred combination (e.g., <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>X</kbd>) &rarr; click **Save & Apply** (instant hotkey re-registration, no restart needed!).
- **On Linux**: Edit `data/config.json` and run `./install.sh`.

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
   - Automatically registers your configured shortcuts in GNOME.
   - Activates the background daemon via `systemd`.
   - You can immediately press your shortcut to snip!

---

## 📂 Project Architecture

```
text-extractor-from-img/
├── src/
│   ├── windows/                          # Windows Native C# Application
│   │   ├── Program.cs                    # Entry point & ApplicationContext
│   │   ├── SettingsForm.cs               # Interactive shortcut reassign dialog
│   │   ├── ConfigManager.cs              # Hotkey parsing & config.json manager
│   │   ├── OverlayForm.cs                # Fullscreen snipping & marquee selection UI
│   │   ├── HistoryForm.cs                # Desktop text extraction history UI
│   │   ├── HotkeyWindow.cs               # Dynamic Win32 global hotkey registration
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
├── data/                                 # User data & persistent configuration
│   ├── config.json                       # Reassignable shortcut settings
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
