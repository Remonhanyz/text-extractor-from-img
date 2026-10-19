const http = require('http');
const fs = require('fs');
const path = require('path');
const { createWorker } = require('tesseract.js');

const PORT = 48821;
const HISTORY_FILE = path.join(__dirname, 'history.json');
let worker = null;
let isReady = false;

// --- History Storage Helper ---
function loadHistory() {
  try {
    if (fs.existsSync(HISTORY_FILE)) {
      const data = fs.readFileSync(HISTORY_FILE, 'utf8');
      return JSON.parse(data);
    }
  } catch (err) {
    console.error('[OCR-Service] Error reading history.json:', err.message);
  }
  return [];
}

function saveHistory(list) {
  try {
    fs.writeFileSync(HISTORY_FILE, JSON.stringify(list, null, 2), 'utf8');
  } catch (err) {
    console.error('[OCR-Service] Error writing history.json:', err.message);
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

// --- Initialize Tesseract ---
async function initWorker() {
  console.log('[OCR-Service] Initializing offline Tesseract worker (ara + eng)...');
  try {
    worker = await createWorker(['ara', 'eng'], 1, {
      cachePath: __dirname,
      gzip: false
    });
    isReady = true;
    console.log('[OCR-Service] Ready! Offline Arabic & English OCR listening on http://127.0.0.1:' + PORT);
    console.log('[OCR-Service] History Web UI available at http://127.0.0.1:' + PORT + '/history');
  } catch (err) {
    console.error('[OCR-Service] Worker initialization error:', err);
  }
}

// --- Web History HTML Template ---
function getHistoryHtml() {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Screen OCR Extractor - Text History</title>
  <style>
    :root {
      --bg: #0f172a;
      --card: #1e293b;
      --border: #334155;
      --accent: #38bdf8;
      --text: #f8fafc;
      --text-muted: #94a3b8;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
      background: var(--bg);
      color: var(--text);
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
    }
    header {
      background: var(--card);
      border-bottom: 1px solid var(--border);
      padding: 12px 20px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
    }
    header h1 {
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--accent);
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .search-box {
      flex: 1;
      max-width: 450px;
      position: relative;
    }
    .search-box input {
      width: 100%;
      background: #0f172a;
      border: 1px solid var(--border);
      color: #fff;
      padding: 8px 14px;
      border-radius: 8px;
      font-size: 0.95rem;
      outline: none;
    }
    .search-box input:focus {
      border-color: var(--accent);
    }
    .btn {
      background: #334155;
      color: #fff;
      border: none;
      padding: 8px 14px;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.9rem;
      transition: background 0.2s;
    }
    .btn:hover { background: #475569; }
    .btn-danger { background: #991b1b; }
    .btn-danger:hover { background: #b91c1c; }
    .btn-primary { background: #0284c7; }
    .btn-primary:hover { background: #0369a1; }
    main {
      flex: 1;
      display: flex;
      overflow: hidden;
    }
    .list-pane {
      width: 320px;
      border-right: 1px solid var(--border);
      overflow-y: auto;
      background: #111827;
    }
    .history-item {
      padding: 12px 16px;
      border-bottom: 1px solid var(--border);
      cursor: pointer;
      transition: background 0.15s;
    }
    .history-item:hover { background: #1e293b; }
    .history-item.active {
      background: #1e293b;
      border-left: 3px solid var(--accent);
    }
    .history-time {
      font-size: 0.75rem;
      color: var(--text-muted);
      margin-bottom: 4px;
    }
    .history-preview {
      font-size: 0.9rem;
      color: #e2e8f0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      direction: auto;
    }
    .detail-pane {
      flex: 1;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 14px;
      background: var(--bg);
    }
    .detail-meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      color: var(--text-muted);
      font-size: 0.85rem;
    }
    .detail-text {
      flex: 1;
      background: var(--card);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 16px;
      color: #fff;
      font-size: 1.05rem;
      line-height: 1.6;
      resize: none;
      outline: none;
      direction: auto;
      font-family: inherit;
    }
    .toast {
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: #059669;
      color: white;
      padding: 10px 18px;
      border-radius: 6px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.3);
      font-size: 0.9rem;
      opacity: 0;
      transform: translateY(10px);
      transition: opacity 0.3s, transform 0.3s;
      pointer-events: none;
    }
    .toast.show { opacity: 1; transform: translateY(0); }
  </style>
</head>
<body>
  <header>
    <h1>🔍 Screen Text Extractor</h1>
    <div class="search-box">
      <input type="text" id="searchInput" placeholder="Search Arabic & English snippets...">
    </div>
    <div style="display: flex; gap: 8px;">
      <button class="btn" onclick="fetchHistory()">↻ Refresh</button>
      <button class="btn btn-danger" onclick="clearHistory()">Clear All</button>
    </div>
  </header>
  <main>
    <div class="list-pane" id="listPane"></div>
    <div class="detail-pane">
      <div class="detail-meta">
        <span id="detailTimestamp">Select a snippet from the list</span>
        <div style="display: flex; gap: 8px;">
          <button class="btn btn-primary" onclick="copyDetail()">Copy to Clipboard</button>
          <button class="btn btn-danger" onclick="deleteDetail()">Delete</button>
        </div>
      </div>
      <textarea id="detailText" class="detail-text" placeholder="Snippet content will appear here..."></textarea>
    </div>
  </main>
  <div id="toast" class="toast">Copied to clipboard!</div>

  <script>
    let historyData = [];
    let activeItem = null;

    async function fetchHistory() {
      try {
        const res = await fetch('/api/history');
        historyData = await res.json();
        renderList();
      } catch (e) {
        console.error(e);
      }
    }

    function renderList() {
      const q = document.getElementById('searchInput').value.toLowerCase().trim();
      const listPane = document.getElementById('listPane');
      listPane.innerHTML = '';

      const filtered = historyData.filter(item => !q || (item.Text && item.Text.toLowerCase().includes(q)));
      filtered.forEach((item, idx) => {
        const div = document.createElement('div');
        div.className = 'history-item' + (activeItem && activeItem.Id === item.Id ? ' active' : '');
        div.onclick = () => selectItem(item);
        div.ondblclick = () => { selectItem(item); copyDetail(); };
        
        const timeDiv = document.createElement('div');
        timeDiv.className = 'history-time';
        timeDiv.textContent = item.Timestamp;

        const previewDiv = document.createElement('div');
        previewDiv.className = 'history-preview';
        previewDiv.textContent = item.Text ? item.Text.replace(/\\n/g, ' ') : '';

        div.appendChild(timeDiv);
        div.appendChild(previewDiv);
        listPane.appendChild(div);

        if (idx === 0 && (!activeItem || !historyData.find(x => x.Id === activeItem.Id))) {
          selectItem(item);
        }
      });

      if (filtered.length === 0) {
        listPane.innerHTML = '<div style="padding: 20px; color: #64748b; text-align: center;">No snippets found</div>';
        document.getElementById('detailText').value = '';
        document.getElementById('detailTimestamp').textContent = 'No selection';
      }
    }

    function selectItem(item) {
      activeItem = item;
      document.querySelectorAll('.history-item').forEach(el => el.classList.remove('active'));
      document.getElementById('detailText').value = item.Text || '';
      document.getElementById('detailTimestamp').textContent = 'Captured: ' + item.Timestamp + ' | ' + (item.Text ? item.Text.length : 0) + ' characters';
      event && event.currentTarget && event.currentTarget.classList.add('active');
    }

    function copyDetail() {
      const text = document.getElementById('detailText').value;
      if (!text) return;
      navigator.clipboard.writeText(text).then(() => showToast('Copied to clipboard!'));
    }

    async function deleteDetail() {
      if (!activeItem) return;
      await fetch('/api/history/delete?id=' + encodeURIComponent(activeItem.Id), { method: 'POST' });
      activeItem = null;
      fetchHistory();
    }

    async function clearHistory() {
      if (!confirm('Are you sure you want to clear all history?')) return;
      await fetch('/api/history/clear', { method: 'POST' });
      activeItem = null;
      fetchHistory();
    }

    function showToast(msg) {
      const t = document.getElementById('toast');
      t.textContent = msg;
      t.classList.add('show');
      setTimeout(() => t.classList.remove('show'), 2000);
    }

    document.getElementById('searchInput').addEventListener('input', renderList);
    fetchHistory();
  </script>
</body>
</html>`;
}

// --- HTTP Server ---
const server = http.createServer(async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  // Health check
  if (req.url === '/health' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ status: isReady ? 'ready' : 'loading', offline: true, languages: ['ara', 'eng'] }));
    return;
  }

  // Web History GUI
  if ((req.url === '/history' || req.url === '/') && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(getHistoryHtml());
    return;
  }

  // API: Get History
  if (req.url === '/api/history' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify(loadHistory()));
    return;
  }

  // API: Delete Item
  if (req.url.startsWith('/api/history/delete') && req.method === 'POST') {
    const url = new URL(req.url, 'http://127.0.0.1:' + PORT);
    const id = url.searchParams.get('id');
    if (id) {
      let list = loadHistory();
      list = list.filter(item => item.Id !== id);
      saveHistory(list);
    }
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ success: true }));
    return;
  }

  // API: Clear History
  if (req.url === '/api/history/clear' && req.method === 'POST') {
    saveHistory([]);
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ success: true }));
    return;
  }

  // OCR Processing
  if (req.url === '/ocr' && req.method === 'POST') {
    if (!isReady || !worker) {
      res.writeHead(503, { 'Content-Type': 'application/json; charset=utf-8' });
      res.end(JSON.stringify({ success: false, error: 'OCR worker is still initializing' }));
      return;
    }

    const chunks = [];
    req.on('data', chunk => chunks.push(chunk));
    req.on('end', async () => {
      try {
        const imageBuffer = Buffer.concat(chunks);
        if (imageBuffer.length === 0) {
          res.writeHead(400, { 'Content-Type': 'application/json; charset=utf-8' });
          res.end(JSON.stringify({ success: false, error: 'Empty image buffer' }));
          return;
        }

        const start = Date.now();
        const ret = await worker.recognize(imageBuffer);
        const duration = Date.now() - start;

        let text = (ret.data && ret.data.text) ? ret.data.text.trim() : '';
        text = text.replace(/\r\n/g, '\n').replace(/\n{3,}/g, '\n\n');

        // Automatically store in history.json
        if (text.length > 0) {
          addHistoryItem(text);
        }

        console.log('[OCR-Service] Recognized (' + duration + 'ms): "' + text.substring(0, 60) + (text.length > 60 ? '...' : '') + '"');

        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({
          success: true,
          text: text,
          durationMs: duration
        }));
      } catch (err) {
        console.error('[OCR-Service] Recognition error:', err);
        res.writeHead(500, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ success: false, error: err.message }));
      }
    });
    return;
  }

  res.writeHead(404);
  res.end('Not Found');
});

initWorker().then(() => {
  server.listen(PORT, '127.0.0.1');
}).catch(err => {
  console.error('[OCR-Service] Fatal worker init failure:', err);
  process.exit(1);
});
