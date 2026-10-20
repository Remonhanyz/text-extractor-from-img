const path = require('path');
const fs = require('fs');
const { RapidOCR } = require('rapidocr-node');

let paddleInstance = null;
let initPromise = null;

async function getPaddleInstance() {
  if (paddleInstance) return paddleInstance;
  if (initPromise) return initPromise;

  initPromise = (async () => {
    try {
      const modelsDir = path.resolve(__dirname, '../../models/paddle');
      const detPath = path.join(modelsDir, 'PP-OCRv6_det_small.onnx');
      const recPath = path.join(modelsDir, 'arabic_PP-OCRv5_rec_mobile.onnx');
      const dictPath = path.join(modelsDir, 'arabic_PP-OCRv5_rec_mobile-dictionary.txt');

      const options = {
        useClassifier: false
      };

      if (fs.existsSync(detPath) && fs.existsSync(recPath) && fs.existsSync(dictPath)) {
        options.detectionModelPath = detPath;
        options.recognitionModelPath = recPath;
        options.dictionaryPath = dictPath;
        options.autoDownloadModels = false;
        options.recognitionModel = {
          ocrVersion: 'PP-OCRv5',
          modelType: 'mobile',
          langType: 'arabic'
        };
      } else {
        options.recognitionModel = {
          ocrVersion: 'PP-OCRv5',
          modelType: 'mobile',
          langType: 'arabic'
        };
      }

      console.log('[PaddleOCR] Initializing PaddleOCR (PP-OCRv5 Arabic)...');
      paddleInstance = await RapidOCR.create(options);
      console.log('[PaddleOCR] Ready for offline inference.');
      return paddleInstance;
    } catch (err) {
      console.error('[PaddleOCR] Failed to initialize:', err.message);
      initPromise = null;
      throw err;
    }
  })();

  return initPromise;
}

function cleanPaddleText(text) {
  if (!text) return '';
  let s = text;
  // Remove BiDi control characters that cause terminal/cursor issues
  s = s.replace(/[\u200E\u200F\u202A-\u202E\u2066-\u2069\uFEFF]/g, '');
  // Normalize linebreaks
  s = s.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  // Punctuation boundary spacing
  s = s.replace(/([:;،؛])([ء-يA-Za-z0-9])/g, '$1 $2');
  s = s.replace(/\s+/g, ' ');
  return s.trim();
}

async function recognize(imageBuffer, lang = 'auto') {
  const startTime = Date.now();
  const ocr = await getPaddleInstance();

  const result = await ocr.recognize(imageBuffer);
  const durationMs = Date.now() - startTime;

  if (!result || !result.lines || result.lines.length === 0) {
    return {
      text: '',
      confidence: 0,
      durationMs,
      engine: 'paddle'
    };
  }

  // Sort lines top to bottom (bounding box y-coordinate)
  const sortedLines = [...result.lines].sort((a, b) => {
    const ya = a.box && a.box[0] ? a.box[0][1] : 0;
    const yb = b.box && b.box[0] ? b.box[0][1] : 0;
    return ya - yb;
  });

  let totalConf = 0;
  const cleanedLines = sortedLines.map(l => {
    totalConf += (l.confidence || 0.9);
    return cleanPaddleText(l.text);
  }).filter(line => line.length > 0);

  const avgConf = Math.round((totalConf / sortedLines.length) * 100);
  const fullText = cleanedLines.join('\n');

  return {
    text: fullText,
    confidence: avgConf,
    durationMs,
    engine: 'paddle'
  };
}

module.exports = {
  getPaddleInstance,
  recognize
};
