const https = require('https');

async function callVisionApi(base64Image, apiKey) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      requests: [
        {
          image: { content: base64Image },
          features: [{ type: 'DOCUMENT_TEXT_DETECTION' }],
          imageContext: { languageHints: ['ar', 'en'] }
        }
      ]
    });

    const options = {
      hostname: 'vision.googleapis.com',
      port: 443,
      path: `/v1/images:annotate?key=${encodeURIComponent(apiKey)}`,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData)
      },
      timeout: 25000
    };

    const req = https.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => data += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            const parsed = JSON.parse(data);
            const text = parsed.responses?.[0]?.fullTextAnnotation?.text || '';
            resolve(text.trim());
          } catch (e) {
            reject(new Error('Failed to parse Cloud Vision response: ' + e.message));
          }
        } else {
          try {
            const parsed = JSON.parse(data);
            const msg = parsed.error?.message || data;
            reject(new Error(`Cloud Vision API error (${res.statusCode}): ${msg}`));
          } catch {
            reject(new Error(`Cloud Vision API error (${res.statusCode}): ${data}`));
          }
        }
      });
    });

    req.on('error', (err) => reject(new Error('Network error calling Cloud Vision: ' + err.message)));
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Cloud Vision request timed out (25s)'));
    });

    req.write(postData);
    req.end();
  });
}

async function recognize(imageBuffer, apiKey) {
  const startTime = Date.now();
  const key = (apiKey || process.env.GOOGLE_VISION_API_KEY || '').trim();

  if (!key) {
    throw new Error('Google Cloud Vision API Key is not set. Please enter your API key in Settings, or select PaddleOCR.');
  }

  const base64Image = imageBuffer.toString('base64');
  const text = await callVisionApi(base64Image, key);
  const durationMs = Date.now() - startTime;

  return {
    text,
    confidence: 98,
    durationMs,
    engine: 'vision'
  };
}

module.exports = {
  recognize
};
