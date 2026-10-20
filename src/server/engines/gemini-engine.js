const https = require('https');

const CANDIDATE_MODELS = [
  'gemini-1.5-flash',
  'gemini-1.5-flash-latest',
  'gemini-2.0-flash',
  'gemini-1.5-pro'
];

async function callSingleModel(base64Image, apiKey, modelName) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      contents: [
        {
          parts: [
            {
              text: "Extract all text from this image with highest precision. Preserve exact Arabic, Greek, French, German, and English words, accented characters (é, è, ê, ë, à, â, î, ï, ô, û, ù, ç, ä, ö, ü, ß), Greek letters (α, β, γ, δ, ε, ζ, η, θ, ι, κ, λ, μ, ν, ξ, ο, π, ρ, σ, τ, υ, φ, χ, ψ, ω), punctuation, numbers, and paragraph formatting. Return ONLY the extracted text. Do not add conversational comments or code block delimiters (```)."
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
      path: `/v1beta/models/${modelName}:generateContent?key=${encodeURIComponent(apiKey)}`,
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
            const err = new Error(`Gemini API error (${res.statusCode}): ${msg}`);
            err.statusCode = res.statusCode;
            reject(err);
          } catch {
            const err = new Error(`Gemini API error (${res.statusCode}): ${data}`);
            err.statusCode = res.statusCode;
            reject(err);
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

async function callGeminiApi(base64Image, apiKey) {
  let lastError = null;

  for (const model of CANDIDATE_MODELS) {
    try {
      return await callSingleModel(base64Image, apiKey, model);
    } catch (err) {
      lastError = err;
      if (err.statusCode === 404) {
        continue;
      }
      throw err;
    }
  }

  throw lastError || new Error('All Gemini model endpoints failed');
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
