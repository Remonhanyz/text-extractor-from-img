const path = require('path');
const fs = require('fs');

const paddleEngine = require('./engines/paddle-engine');
const geminiEngine = require('./engines/gemini-engine');
const cloudVisionEngine = require('./engines/cloud-vision-engine');
const tesseractEngine = require('./engines/tesseract-engine');

const CONFIG_PATH = path.resolve(__dirname, '../../data/config.json');

function loadConfig() {
  try {
    if (fs.existsSync(CONFIG_PATH)) {
      const raw = fs.readFileSync(CONFIG_PATH, 'utf8').replace(/^\uFEFF/, '');
      return JSON.parse(raw);
    }
  } catch (err) {
    console.error('[OCR-Worker] Could not read config:', err.message);
  }
  return {};
}

async function initWorker() {
  console.log('[OCR-Worker] Initializing default OCR engines...');
  // Warm up PaddleOCR in background
  try {
    await paddleEngine.getPaddleInstance();
  } catch (err) {
    console.warn('[OCR-Worker] PaddleOCR warmup note:', err.message);
  }
}

async function recognize(imageBuffer, options = {}) {
  const config = loadConfig();

  // Determine engine
  let engine = (options.engine || config.ocrEngine || 'paddle').toLowerCase().trim();
  const lang = (options.lang || config.ocrLanguage || 'auto').toLowerCase().trim();
  const geminiKey = options.geminiApiKey || config.geminiApiKey || process.env.GEMINI_API_KEY || '';
  const visionKey = options.cloudVisionApiKey || config.cloudVisionApiKey || process.env.GOOGLE_VISION_API_KEY || '';

  console.log(`[OCR-Worker] Processing with engine=[${engine}], lang=[${lang}]`);

  try {
    if (engine === 'gemini') {
      return await geminiEngine.recognize(imageBuffer, geminiKey);
    } else if (engine === 'vision' || engine === 'cloud-vision') {
      return await cloudVisionEngine.recognize(imageBuffer, visionKey);
    } else if (engine === 'tesseract') {
      return await tesseractEngine.recognize(imageBuffer, lang);
    } else {
      // Default: PaddleOCR
      return await paddleEngine.recognize(imageBuffer, lang);
    }
  } catch (err) {
    console.error(`[OCR-Worker] Engine [${engine}] failed: ${err.message}`);
    // If a cloud engine failed (e.g. invalid key or offline), fallback to local PaddleOCR
    if (engine === 'gemini' || engine === 'vision' || engine === 'cloud-vision') {
      console.log('[OCR-Worker] Falling back to local PaddleOCR engine...');
      const fallbackResult = await paddleEngine.recognize(imageBuffer, lang);
      fallbackResult.fallbackFrom = engine;
      fallbackResult.fallbackReason = err.message;
      return fallbackResult;
    }
    throw err;
  }
}

function getStatus() {
  const config = loadConfig();
  return {
    ready: true,
    defaultEngine: config.ocrEngine || 'paddle',
    defaultLanguage: config.ocrLanguage || 'auto',
    hasGeminiKey: Boolean(config.geminiApiKey || process.env.GEMINI_API_KEY),
    hasVisionKey: Boolean(config.cloudVisionApiKey || process.env.GOOGLE_VISION_API_KEY)
  };
}

module.exports = {
  initWorker,
  recognize,
  getStatus,
  loadConfig
};
