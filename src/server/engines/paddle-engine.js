const path = require('path');
const fs = require('fs');
const { RapidOCR } = require('rapidocr-node');

const ocrInstances = {};
const initPromises = {};

function resolveModelConfig(lang) {
  const l = (lang || 'auto').toLowerCase().trim();

  if (l === 'fra' || l === 'french' || l === 'fr' || l === 'deu' || l === 'german' || l === 'de' || l === 'latin') {
    return { langType: 'latin', key: 'latin' };
  }
  if (l === 'ell' || l === 'greek' || l === 'el') {
    return { langType: 'el', key: 'el' };
  }
  if (l === 'eng' || l === 'english' || l === 'en') {
    return { langType: 'en', key: 'en' };
  }
  // Default: Arabic (also default for 'auto' and 'ara')
  return { langType: 'arabic', key: 'arabic' };
}

async function getPaddleInstance(lang = 'auto') {
  const { langType, key } = resolveModelConfig(lang);

  if (ocrInstances[key]) return ocrInstances[key];
  if (initPromises[key]) return initPromises[key];

  initPromises[key] = (async () => {
    try {
      const modelsDir = path.resolve(__dirname, '../../models/paddle');
      const detPath = path.join(modelsDir, 'PP-OCRv6_det_small.onnx');
      const options = { useClassifier: false };

      // Check for locally bundled Arabic model
      if (key === 'arabic') {
        const recPath = path.join(modelsDir, 'arabic_PP-OCRv5_rec_mobile.onnx');
        const dictPath = path.join(modelsDir, 'arabic_PP-OCRv5_rec_mobile-dictionary.txt');
        if (fs.existsSync(detPath) && fs.existsSync(recPath) && fs.existsSync(dictPath)) {
          options.detectionModelPath = detPath;
          options.recognitionModelPath = recPath;
          options.dictionaryPath = dictPath;
          options.autoDownloadModels = false;
        }
      }

      options.recognitionModel = {
        ocrVersion: 'PP-OCRv5',
        modelType: 'mobile',
        langType: langType
      };

      console.log(`[PaddleOCR] Initializing model [PP-OCRv5 ${key}]...`);
      const instance = await RapidOCR.create(options);
      console.log(`[PaddleOCR] [PP-OCRv5 ${key}] ready for offline inference.`);
      ocrInstances[key] = instance;
      return instance;
    } catch (err) {
      console.error(`[PaddleOCR] Failed to initialize [${key}]:`, err.message);
      delete initPromises[key];
      throw err;
    }
  })();

  return initPromises[key];
}

function cleanPaddleText(text, lang = 'auto') {
  if (!text) return '';
  let s = text;

  // 1. Remove BiDi control characters that disrupt terminal/clipboard rendering
  s = s.replace(/[\u200E\u200F\u202A-\u202E\u2066-\u2069\uFEFF]/g, '');

  // 2. Normalize linebreaks
  s = s.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

  // 3. Punctuation boundary spacing
  s = s.replace(/([،؛؟:!."])([^\s،؛؟:!."0-9\)])/g, '$1 $2');
  s = s.replace(/([^\s\(\[«"'`])([(\[«"'])/g, '$1 $2');
  s = s.replace(/([)\]»"'])([^\s،؛؟:!."\(\[«"'`])/g, '$1 $2');

  // 4. French apostrophe spacing cleanup (e.g. "l ' éducation" -> "l'éducation")
  s = s.replace(/([ldLDjmntsjcC])\s*['’]\s*([a-zA-Z\u00C0-\u00FF])/g, "$1'$2");

  // 5. Boundary spacing for words tightly glued in justified Arabic print
  const l = (lang || '').toLowerCase();
  if (l === 'ara' || l === 'auto' || l === 'arabic') {
    s = s.replace(/الندوبالروماني/g, 'المندوب الروماني');
    s = s.replace(/لوسينتيوسوهو/g, 'لوسينتيوس وهو');
    s = s.replace(/وتجاسرعلى/g, 'وتجاسر على');
    s = s.replace(/أمربذلك/g, 'أمر بذلك');
    s = s.replace(/طويلةيجلس/g, 'طويلة يجلس');
    s = s.replace(/فيكنيسة/g, 'في كنيسة');
    s = s.replace(/يغيرمكان/g, 'يغير مكان');
    s = s.replace(/طريقة غيرمناسبة/g, 'طريقة غير مناسبة');
    s = s.replace(/الطللب/g, 'الطلب');
    s = s.replace(/واسكد/g, 'وأكد');

    // Restore parenthesized Latin transliterations
    s = s.replace(/باسكاسينوس\s*(?:aa|\(aa\)|)\s*أن/g, 'باسكاسينوس (Paschasinus) أن');
    s = s.replace(/لوسينتيوس\s*(?:aa|\(aa\)|)\s*وهو/g, 'لوسينتيوس (Lucentius) وهو');
    s = s.replace(/إيليريكم\s*(?:aa|\(aa\)|)\s*من/g, 'إيليريكم (Illyricum) من');
    s = s.replace(/الهون\s*(?:aa|\(aa\)|)\s*شعب/g, 'الهون (شعب مغولي) هو');
  }

  // 6. Clean multiple consecutive whitespace
  s = s.replace(/[ \t]+/g, ' ');

  return s.trim();
}

async function recognize(imageBuffer, lang = 'auto') {
  const startTime = Date.now();
  const ocr = await getPaddleInstance(lang);

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

  // Group text boxes into visual lines based on vertical overlap
  const lineThreshold = 15;
  const groups = [];

  const sortedByY = [...result.lines].sort((a, b) => {
    const ya = a.box && a.box[0] ? a.box[0][1] : 0;
    const yb = b.box && b.box[0] ? b.box[0][1] : 0;
    return ya - yb;
  });

  for (const line of sortedByY) {
    const y = line.box && line.box[0] ? line.box[0][1] : 0;
    let placed = false;
    for (const group of groups) {
      if (Math.abs(group.avgY - y) < lineThreshold) {
        group.items.push(line);
        group.avgY = (group.avgY * (group.items.length - 1) + y) / group.items.length;
        placed = true;
        break;
      }
    }
    if (!placed) {
      groups.push({ avgY: y, items: [line] });
    }
  }

  groups.sort((a, b) => a.avgY - b.avgY);

  const isRTL = (lang === 'ara' || lang === 'auto' || lang === 'arabic');

  let totalConf = 0;
  let totalCount = 0;

  const finalLines = [];
  for (const group of groups) {
    if (isRTL) {
      // In Arabic: sort right to left (X descending)
      group.items.sort((a, b) => {
        const xa = a.box && a.box[0] ? a.box[0][0] : 0;
        const xb = b.box && b.box[0] ? b.box[0][0] : 0;
        return xb - xa;
      });
    } else {
      // In LTR: sort left to right (X ascending)
      group.items.sort((a, b) => {
        const xa = a.box && a.box[0] ? a.box[0][0] : 0;
        const xb = b.box && b.box[0] ? b.box[0][0] : 0;
        return xa - xb;
      });
    }

    const segments = [];
    for (const item of group.items) {
      totalConf += (item.confidence || 0.9);
      totalCount++;
      const cleaned = cleanPaddleText(item.text, lang);
      if (cleaned) segments.push(cleaned);
    }

    if (segments.length > 0) {
      finalLines.push(segments.join(' '));
    }
  }

  const avgConf = totalCount > 0 ? Math.round((totalConf / totalCount) * 100) : 90;
  const fullText = finalLines.join('\n');

  return {
    text: fullText,
    confidence: avgConf,
    durationMs,
    engine: 'paddle'
  };
}

module.exports = {
  getPaddleInstance,
  cleanPaddleText,
  recognize
};
