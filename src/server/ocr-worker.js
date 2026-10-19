const path = require('path');
const { createWorker } = require('tesseract.js');

const MODELS_DIR = path.resolve(__dirname, '../models');

let workerAuto = null;
let workerAra = null;
let isReady = false;

function postProcess(text) {
  if (!text) return '';
  let s = text;

  // 1. Strip invisible BiDi control characters (causes cursor jumping / reversed editing when pasted)
  s = s.replace(/[\u200E\u200F\u202A-\u202E\u2066-\u2069\uFEFF]/g, '');

  // 2. Normalize Windows/Unix newlines
  s = s.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

  // 3. Fix glued words ending with Taa Marbuta (ة): in Arabic, ة is ALWAYS the last letter of a word
  // e.g. "وسرعةفائقة" -> "وسرعة فائقة", "شاشةبدقة" -> "شاشة بدقة"
  s = s.replace(/([ة])([ء-ي])/g, '$1 $2');

  // 4. Fix "96" OCR hallucination for percent "%"
  // When Tesseract sees "100%" or "%100" on screen, the '%' symbol is often recognized as "96"
  s = s.replace(/96([0-9]+)/g, '$1%');
  s = s.replace(/([0-9]+)96/g, '$1%');
  s = s.replace(/([0-9]+)\s*96\b/g, '$1%');
  s = s.replace(/\b96\s*([0-9]+)/g, '$1%');

  // 5. Spacing between Arabic letters and digits/symbols
  s = s.replace(/([ء-ي])([0-9%٪])/g, '$1 $2');
  s = s.replace(/([0-9%٪])([ء-ي])/g, '$1 $2');

  // 6. Clean excess spaces and empty lines
  s = s.replace(/[ \t]+/g, ' ');
  s = s.replace(/\n{3,}/g, '\n\n');

  return s.trim();
}

async function initWorker() {
  console.log('[OCR-Worker] Initializing high-accuracy offline Tesseract workers from:', MODELS_DIR);
  try {
    // Primary Auto Worker: Arabic dictionary + Arabic full script + English
    workerAuto = await createWorker(['ara', 'Arabic', 'eng'], 1, {
      cachePath: MODELS_DIR,
      gzip: false
    });
    await workerAuto.setParameters({
      tessedit_pageseg_mode: '6',
      preserve_interword_spaces: '1'
    });

    // Dedicated Pure Arabic Worker: Highest accuracy without any Latin hallucinations
    workerAra = await createWorker(['ara', 'Arabic'], 1, {
      cachePath: MODELS_DIR,
      gzip: false
    });
    await workerAra.setParameters({
      tessedit_pageseg_mode: '6',
      preserve_interword_spaces: '1'
    });

    isReady = true;
    console.log('[OCR-Worker] Ready: Auto (Arabic+Script+English) & Pure Arabic workers online.');
  } catch (err) {
    console.error('[OCR-Worker] Initialization failure:', err);
    throw err;
  }
}

async function recognize(imageBuffer, lang = 'auto') {
  if (!isReady) {
    throw new Error('OCR worker is not ready yet');
  }

  const start = Date.now();
  let worker = workerAuto;
  if (lang === 'ara' && workerAra) {
    worker = workerAra;
  }

  const ret = await worker.recognize(imageBuffer);
  const duration = Date.now() - start;

  const rawText = (ret.data && ret.data.text) ? ret.data.text : '';
  const text = postProcess(rawText);

  return {
    text: text,
    confidence: ret.data ? ret.data.confidence : 0,
    durationMs: duration
  };
}

function getStatus() {
  return {
    ready: isReady,
    languages: ['ara', 'Arabic', 'eng'],
    modes: ['auto', 'ara', 'eng'],
    offline: true
  };
}

module.exports = {
  initWorker,
  recognize,
  getStatus
};
