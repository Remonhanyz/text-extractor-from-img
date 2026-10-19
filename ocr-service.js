const http = require('http');
const path = require('path');
const { createWorker } = require('tesseract.js');

const PORT = 48821;
let worker = null;
let isReady = false;

async function initWorker() {
  console.log('[OCR-Service] Initializing local offline Tesseract worker (ara + eng)...');
  try {
    worker = await createWorker(['ara', 'eng'], 1, {
      cachePath: __dirname,
      gzip: false
    });
    isReady = true;
    console.log('[OCR-Service] Ready! Offline Arabic & English OCR listening on http://127.0.0.1:' + PORT);
  } catch (err) {
    console.error('[OCR-Service] Worker initialization error:', err);
  }
}

const server = http.createServer(async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.url === '/health' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ status: isReady ? 'ready' : 'loading', offline: true, languages: ['ara', 'eng'] }));
    return;
  }

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
