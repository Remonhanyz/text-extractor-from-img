# Arabic & English Screen OCR Text Extractor 🔍

A lightweight, high-performance screen text extractor and OCR tool for Windows, inspired by Microsoft PowerToys Text Extractor, designed with full **Arabic and English** support and running **100% offline with zero Administrator privileges**.

---

## ✨ Features

- ⚡ **Ultra-Fast Local OCR**: Uses a pre-warmed neural engine (`tesseract.js` with `ara` + `eng`) delivering recognition in **30–70 ms**.
- 📴 **100% Offline**: Neural models (`ara.traineddata` & `eng.traineddata`) are stored locally. Zero cloud dependency, zero telemetry, total privacy.
- 🎯 **PowerToys-Style Screen Snip**: Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> to dim all monitors, drag a selection box around any text, and instantly copy it to your clipboard.
- 📜 **Searchable Text History**: Press <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd> to open the History Window. Search past extractions, view character counts, and double-click to re-copy.
- 🚀 **Start with Windows Toggle**: Toggle auto-start on boot directly from the System Tray menu without needing admin permissions.
- 🎨 **Custom Windows Icon**: Clean custom app icon embedded into the executable, desktop shortcut, and system tray.
- 🛡️ **Zero Admin Rights Needed**: Runs entirely within user space.

---

## ⌨️ Shortcuts

| Shortcut | Action |
|---|---|
| <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>A</kbd> | Snip screen and extract Arabic / English text to clipboard |
| <kbd>Alt</kbd> + <kbd>Shift</kbd> + <kbd>H</kbd> | Open Text Extraction History window |
| <kbd>Esc</kbd> | Cancel active snipping overlay or close History window |

---

## 🛠️ Project Structure

- `ArabicSnipper.exe` - Native Windows C# application providing the dark overlay, marquee selection, global hotkey listener, and system tray integration.
- `ocr-service.js` - Background Node.js service keeping the Arabic and English neural OCR workers warm in memory.
- `run.vbs` - Silent background launcher script.
- `start.bat` / `stop.bat` - Quick control scripts.
- `history.json` - Local persistent storage for extracted snippets.
- `app.ico` - Multi-resolution application icon.

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 or 11
- Node.js (v18+)

### Running
1. Clone the repository:
   ```bash
   git clone https://github.com/Remonhanyz/arabic-text-extractor.git
   cd arabic-text-extractor
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Run:
   - Double-click `run.vbs` or `start.bat`
   - Or run `ArabicSnipper.exe`
