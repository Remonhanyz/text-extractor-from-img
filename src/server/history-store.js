const fs = require('fs');
const path = require('path');

const DATA_DIR = path.resolve(__dirname, '../../data');
const HISTORY_FILE = path.join(DATA_DIR, 'history.json');

function ensureDataDir() {
  if (!fs.existsSync(DATA_DIR)) {
    try { fs.mkdirSync(DATA_DIR, { recursive: true }); } catch (e) { }
  }
}

function loadHistory() {
  try {
    ensureDataDir();
    if (fs.existsSync(HISTORY_FILE)) {
      let data = fs.readFileSync(HISTORY_FILE, 'utf8');
      // Strip UTF-8 BOM if present
      data = data.replace(/^\uFEFF/, '').trim();
      if (!data) return [];
      return JSON.parse(data);
    }
  } catch (err) {
    console.error('[HistoryStore] Error reading history.json:', err.message);
  }
  return [];
}

function saveHistory(list) {
  try {
    ensureDataDir();
    fs.writeFileSync(HISTORY_FILE, JSON.stringify(list, null, 2), 'utf8');
  } catch (err) {
    console.error('[HistoryStore] Error writing history.json:', err.message);
  }
}

function addHistoryItem(text) {
  if (!text || text.trim().length === 0) return;
  const list = loadHistory();
  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;

  const newItem = {
    Id: Math.random().toString(36).substring(2, 12),
    Timestamp: timestamp,
    Text: text.trim()
  };

  list.unshift(newItem);
  if (list.length > 200) {
    list.splice(200);
  }
  saveHistory(list);
}

function deleteHistoryItem(id) {
  let list = loadHistory();
  list = list.filter(item => item.Id !== id);
  saveHistory(list);
}

function clearHistory() {
  saveHistory([]);
}

module.exports = {
  loadHistory,
  saveHistory,
  addHistoryItem,
  deleteHistoryItem,
  clearHistory
};
