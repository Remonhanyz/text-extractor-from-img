const https = require('https');

async function callGeminiApi(base64Image, apiKey) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      contents: [
        {
          parts: [
            {
              text: "Extract all text from this image with highest precision. Preserve exact Arabic, English words, punctuation, numbers, and paragraph formatting. Return ONLY the extracted text. Do not add explanations, conversational comments, or code block delimiters (```)."
            },
            {
              inline_data: {
                mime_type: "image/png",
                data: base64Image
              }
            }
          ]
        }
      ],
      generationConfig: {
        temperature: 0.1,
        maxOutputTokens: 4096
      }
    });

    const options = {
      hostname: 'generativelanguage.googleapis.com',
      port: 443,
      path: `/v1beta/models/gemini-1.5-flash:generateContent?key=${encodeURIComponent(apiKey)}`,
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
            const text = parsed.candidates?.[0]?.content?.parts?.[0]?.text || '';
            resolve(text.trim());
          } catch (e) {
            reject(new Error('Failed to parse Gemini response: ' + e.message));
          }
        } else {
          try {
            const parsed = JSON.parse(data);
            const msg = parsed.error?.message || data;
            reject(new Error(`Gemini API error (${res.statusCode}): ${msg}`));
          } catch {
            reject(new Error(`Gemini API error (${res.statusCode}): ${data}`));
          }
        }
      });
    });

    req.on('error', (err) => reject(new Error('Network error calling Gemini: ' + err.message)));
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Gemini API request timed out (25s)'));
    });

    req.write(postData);
    req.end();
  });
}

async function recognize(imageBuffer, apiKey) {
  const startTime = Date.now();
  const key = (apiKey || process.env.GEMINI_API_KEY || '').trim();

  if (!key) {
    throw new Error('Gemini API Key is not set. Please enter your free Google AI Studio key in Settings, or select PaddleOCR.');
  }

  const base64Image = imageBuffer.toString('base64');
  const text = await callGeminiApi(base64Image, key);
  const durationMs = Date.now() - startTime;

  return {
    text,
    confidence: 99,
    durationMs,
    engine: 'gemini'
  };
}

module.exports = {
  recognize
};
