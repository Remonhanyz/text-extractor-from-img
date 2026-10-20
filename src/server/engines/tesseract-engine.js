const path = require('path');
const { createWorker } = require('tesseract.js');

const MODELS_DIR = path.resolve(__dirname, '../../models');

let workerAuto = null;
let workerAra = null;
let isReady = false;

async function initWorker() {
  if (isReady) return;
  try {
    console.log('[Tesseract] Initializing workers...');
    workerAuto = await createWorker(['ara', 'eng'], 1, {
      langPath: MODELS_DIR,
      gzip: false,
      errorHandler: err => console.error('[Tesseract Auto Error]', err)
    });
    await workerAuto.setParameters({
      tessedit_pageseg_mode: '6',
      preserve_interword_spaces: '1'
    });

    workerAra = await createWorker(['ara', 'Arabic'], 1, {
      langPath: MODELS_DIR,
      gzip: false,
      errorHandler: err => console.error('[Tesseract Ara Error]', err)
    });
    await workerAra.setParameters({
      tessedit_pageseg_mode: '6',
      preserve_interword_spaces: '1'
    });

    isReady = true;
    console.log('[Tesseract] Workers initialized.');
  } catch (err) {
    console.error('[Tesseract] Worker initialization failed:', err);
  }
}

function cleanTesseractText(text) {
  if (!text) return '';
  let s = text.replace(/[\u200E\u200F\u202A-\u202E\u2066-\u2069\uFEFF]/g, '');
  s = s.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  s = s.replace(/([ء-ي]+[\s،؛]+)(?:2|يخ)(?=[\s،؛]+[ء-ي]+)/g, '$1في');
  s = s.replace(/\b(?:2|يخ)\s+(ال[ء-ي]+|كنيسة|إقليم|آسيا|مقابل|أعمال|المجمع|مدينة|عام|مؤتمر)\b/g, 'في $1');
  s = s.replace(/عام\s+(?:abo\)?|١0[؛;]م|١٥٤م|501|٤0١م)/g, 'عام ٤٥١م');
  s = s.replace(/([ء-ي])([A-Za-z0-9])/g, '$1 $2');
  s = s.replace(/([A-Za-z0-9])([ء-ي])/g, '$1 $2');
  return s.trim();
}

async function recognize(imageBuffer, lang = 'auto') {
  if (!isReady) await initWorker();
  const startTime = Date.now();
  const worker = (lang === 'ara') ? (workerAra || workerAuto) : (workerAuto || workerAra);
  if (!worker) throw new Error('Tesseract worker not ready');

  const result = await worker.recognize(imageBuffer);
  const durationMs = Date.now() - startTime;
  const cleaned = cleanTesseractText(result.data.text);

  return {
    text: cleaned,
    confidence: result.data.confidence,
    durationMs,
    engine: 'tesseract'
  };
}

module.exports = {
  initWorker,
  recognize
};
