const http = require('http');
const https = require('https');
const fs = require('fs');
const path = require('path');
const ocrWorker = require('./ocr-worker');
const historyStore = require('./history-store');

const PORT = process.env.PORT || 48821;
const WEB_DIR = path.join(__dirname, 'web');
const CONFIG_PATH = path.resolve(__dirname, '../../data/config.json');

function saveConfig(updated) {
  let existing = {};
  try {
    if (fs.existsSync(CONFIG_PATH)) {
      const raw = fs.readFileSync(CONFIG_PATH, 'utf8').replace(/^\uFEFF/, '');
      existing = JSON.parse(raw);
    }
  } catch {}

  const merged = { ...existing, ...updated };
  const dir = path.dirname(CONFIG_PATH);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(merged, null, 2), 'utf8');
  return merged;
}

const server = http.createServer(async (req, res) => {
  // CORS Headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, X-OCR-Lang, X-OCR-Engine, X-Gemini-Key, X-Vision-Key');

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

  // Config API - Get
  if (req.url === '/api/config' && req.method === 'GET') {
    const cfg = ocrWorker.loadConfig();
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify(cfg));
    return;
  }

  // Config API - Update
  if (req.url === '/api/config' && req.method === 'POST') {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
      try {
        const payload = JSON.parse(body);
        const updated = saveConfig(payload);
        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ success: true, config: updated }));
      } catch (err) {
        res.writeHead(400, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ success: false, error: err.message }));
      }
    });
    return;
  }

  // Test Gemini API Key
  if (req.url === '/api/test-key' && req.method === 'POST') {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
      try {
        const { apiKey } = JSON.parse(body);
        if (!apiKey || !apiKey.trim()) {
          res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
          res.end(JSON.stringify({ valid: false, error: 'API key is empty' }));
          return;
        }

        const testReq = https.request({
          hostname: 'generativelanguage.googleapis.com',
          port: 443,
          path: `/v1beta/models?key=${encodeURIComponent(apiKey.trim())}`,
          method: 'GET',
          timeout: 10000
        }, (testRes) => {
          let testData = '';
          testRes.on('data', c => testData += c);
          testRes.on('end', () => {
            if (testRes.statusCode >= 200 && testRes.statusCode < 300) {
              res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
              res.end(JSON.stringify({ valid: true, message: 'Google AI Studio key is valid!' }));
            } else {
              let errMsg = 'Invalid API key';
              try {
                const parsed = JSON.parse(testData);
                errMsg = parsed.error?.message || errMsg;
              } catch {}
              res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
              res.end(JSON.stringify({ valid: false, error: errMsg }));
            }
          });
        });

        testReq.on('error', (err) => {
          res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
          res.end(JSON.stringify({ valid: false, error: 'Network error: ' + err.message }));
        });
        testReq.on('timeout', () => {
          testReq.destroy();
          res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
          res.end(JSON.stringify({ valid: false, error: 'Request timed out (10s)' }));
        });
        testReq.end();
      } catch (err) {
        res.writeHead(400, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ valid: false, error: err.message }));
      }
    });
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
    const lang = parsedUrl.searchParams.get('lang') || req.headers['x-ocr-lang'] || '';
    const engine = parsedUrl.searchParams.get('engine') || req.headers['x-ocr-engine'] || '';
    const geminiKey = req.headers['x-gemini-key'] || '';
    const visionKey = req.headers['x-vision-key'] || '';

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

        const result = await ocrWorker.recognize(imageBuffer, {
          engine,
          lang,
          geminiApiKey: geminiKey,
          cloudVisionApiKey: visionKey
        });

        // Auto-save non-empty extractions to history
        if (result.text && result.text.length > 0) {
          historyStore.addHistoryItem(result.text);
        }

        const preview = result.text.length > 60 ? result.text.substring(0, 60) + '...' : result.text;
        console.log(`[OCR-Server] [${result.engine}] Recognized (${result.durationMs}ms, Conf: ${result.confidence}%): "${preview.replace(/\n/g, ' ')}"`);

        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({
          success: true,
          text: result.text,
          confidence: result.confidence,
          durationMs: result.durationMs,
          engine: result.engine,
          fallbackFrom: result.fallbackFrom,
          fallbackReason: result.fallbackReason
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
    console.log('[OCR-Server] History & Settings Dashboard: http://127.0.0.1:' + PORT + '/history');
  });
}).catch(err => {
  console.error('[OCR-Server] Failed to initialize worker:', err);
  process.exit(1);
});
