const http = require('http');
const fs = require('fs');
const path = require('path');
const ocrWorker = require('./ocr-worker');
const historyStore = require('./history-store');

const PORT = process.env.PORT || 48821;
const WEB_DIR = path.join(__dirname, 'web');

const server = http.createServer(async (req, res) => {
  // CORS Headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, X-OCR-Lang');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  // Health check
  if (req.url === '/health' && req.method === 'GET') {
    const status = ocrWorker.getStatus();
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify(status));
    return;
  }

  // History API - List
  if (req.url === '/api/history' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify(historyStore.loadHistory()));
    return;
  }

  // History API - Delete
  if (req.url.startsWith('/api/history/delete') && req.method === 'POST') {
    const url = new URL(req.url, 'http://127.0.0.1:' + PORT);
    const id = url.searchParams.get('id');
    if (id) historyStore.deleteHistoryItem(id);
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ success: true }));
    return;
  }

  // History API - Clear
  if (req.url === '/api/history/clear' && req.method === 'POST') {
    historyStore.clearHistory();
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ success: true }));
    return;
  }

  // Static Web Dashboard files
  if (req.method === 'GET') {
    let filePath = null;
    let contentType = 'text/plain';

    if (req.url === '/' || req.url === '/history' || req.url === '/index.html') {
      filePath = path.join(WEB_DIR, 'index.html');
      contentType = 'text/html; charset=utf-8';
    } else if (req.url === '/style.css') {
      filePath = path.join(WEB_DIR, 'style.css');
      contentType = 'text/css; charset=utf-8';
    } else if (req.url === '/app.js') {
      filePath = path.join(WEB_DIR, 'app.js');
      contentType = 'application/javascript; charset=utf-8';
    }

    if (filePath && fs.existsSync(filePath)) {
      res.writeHead(200, { 'Content-Type': contentType });
      fs.createReadStream(filePath).pipe(res);
      return;
    }
  }

  // OCR Endpoint
  if (req.url.startsWith('/ocr') && req.method === 'POST') {
    const parsedUrl = new URL(req.url, 'http://127.0.0.1:' + PORT);
    const lang = parsedUrl.searchParams.get('lang') || req.headers['x-ocr-lang'] || 'auto';

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

        const result = await ocrWorker.recognize(imageBuffer, lang);

        // Auto-save non-empty extractions to history
        if (result.text && result.text.length > 0) {
          historyStore.addHistoryItem(result.text);
        }

        const preview = result.text.length > 60 ? result.text.substring(0, 60) + '...' : result.text;
        console.log('[OCR-Server] Recognized [' + lang + '] (' + result.durationMs + 'ms, Conf: ' + result.confidence + '%): "' + preview.replace(/\n/g, ' ') + '"');

        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({
          success: true,
          text: result.text,
          confidence: result.confidence,
          durationMs: result.durationMs
        }));
      } catch (err) {
        console.error('[OCR-Server] Error processing image:', err.message);
        res.writeHead(500, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ success: false, error: err.message }));
      }
    });
    return;
  }

  res.writeHead(404);
  res.end('Not Found');
});

// Start worker and server
ocrWorker.initWorker().then(() => {
  server.listen(PORT, '127.0.0.1', () => {
    console.log('[OCR-Server] Server running on http://127.0.0.1:' + PORT);
    console.log('[OCR-Server] History Web Dashboard: http://127.0.0.1:' + PORT + '/history');
  });
}).catch(err => {
  console.error('[OCR-Server] Failed to initialize worker:', err);
  process.exit(1);
});
