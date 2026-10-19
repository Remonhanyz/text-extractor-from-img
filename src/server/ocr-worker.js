const path = require('path');
const { createWorker } = require('tesseract.js');

const MODELS_DIR = path.resolve(__dirname, '../models');
let worker = null;
let isReady = false;

async function initWorker() {
  console.log('[OCR-Worker] Initializing offline Tesseract worker with models in:', MODELS_DIR);
  try {
    worker = await createWorker(['ara', 'eng'], 1, {
      cachePath: MODELS_DIR,
      gzip: false
    });
    isReady = true;
    console.log('[OCR-Worker] Worker ready for Arabic and English OCR.');
  } catch (err) {
    console.error('[OCR-Worker] Initialization failure:', err);
    throw err;
  }
}

async function recognize(imageBuffer) {
  if (!isReady || !worker) {
    throw new Error('OCR worker is not ready yet');
  }

  const start = Date.now();
  const ret = await worker.recognize(imageBuffer);
  const duration = Date.now() - start;

  let text = (ret.data && ret.data.text) ? ret.data.text.trim() : '';
  text = text.replace(/\r\n/g, '\n').replace(/\n{3,}/g, '\n\n');

  return {
    text: text,
    durationMs: duration
  };
}

function getStatus() {
  return {
    ready: isReady,
    languages: ['ara', 'eng'],
    offline: true
  };
}

module.exports = {
  initWorker,
  recognize,
  getStatus
};
