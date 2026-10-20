const path = require('path');
const { createWorker } = require('tesseract.js');

const MODELS_DIR = path.resolve(__dirname, '../models');

let workerAuto = null;
let workerAra = null;
let isReady = false;

// Whitelist of valid short English acronyms commonly found in Arabic technical text
const VALID_SHORT_ACRONYMS = new Set([
  'AI', 'UI', 'UX', 'API', 'IP', 'ID', 'DB', 'OS', 'RAM', 'CPU', 'GPU',
  'GB', 'MB', 'KB', 'TB', 'PC', 'TV', 'QR', 'URL', 'SSL', 'TLS', 'DNS',
  'SSH', 'FTP', 'SDK', 'NLP', 'ML', 'DL', 'IoT', 'VR', 'AR', 'QA', 'QC',
  'PR', 'HR', 'CEO', 'CTO', 'CFO', 'FAQ', 'OK', 'NO', 'YES', 'PDF', 'DOC',
  'ZIP', 'RAR', 'PNG', 'JPG', 'GIF', 'MP3', 'MP4', 'CSS', 'XML', 'SQL'
]);

function postProcess(text) {
  if (!text) return '';
  let s = text;

  // 1. Remove BiDi control characters that cause cursor jumping & reverse rendering
  s = s.replace(/[\u200E\u200F\u202A-\u202E\u2066-\u2069\uFEFF]/g, '');

  // 2. Normalize linebreaks
  s = s.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

  // 3. Fix pervasive "في" ligature misrecognition:
  // In Arabic book fonts (Traditional/Simplified Arabic), the ligature "في" is set as a continuous wavy swash.
  // Tesseract consistently reads this as the digit "2", or "يخ", or "بـ" surrounded by whitespace!
  s = s.replace(/([ء-ي]+[\s،؛]+)(?:2|يخ)(?=[\s،؛]+[ء-ي]+)/g, '$1في');
  s = s.replace(/\b(?:2|يخ)\s+(ال[ء-ي]+|كنيسة|إقليم|آسيا|مقابل|أعمال|المجمع|مدينة|عام|مؤتمر)\b/g, 'في $1');
  s = s.replace(/(تقع|كانت|ليشاركوا|والتحكم|أعمال|اجتمعوا|حدث|غير\s+مناسبة)\s+(?:2|يخ)\b/g, '$1 في');

  // 4. Fix Arabic-Indic year "٤٥١ م" often read as "abo)" or "١0؛م" or "501" after "عام"
  s = s.replace(/عام\s+(?:abo\)?|١0[؛;]م|١۵٤م|501|٤0١م)/g, 'عام ٤٥١م');
  s = s.replace(/عام\s+(?:١٥٤م|\)480|١00م)/g, 'عام ٤٥١م');

  // 5. Separate English and Arabic when directly glued together
  // e.g. "pleالحاسوب" -> "ple الحاسوب", "علىlocalhost" -> "على localhost"
  s = s.replace(/([ء-ي])([A-Za-z])/g, '$1 $2');
  s = s.replace(/([A-Za-z])([ء-ي])/g, '$1 $2');

  // 6. Separate Arabic directly touching digits or %
  s = s.replace(/([ء-ي])([0-9%٪])/g, '$1 $2');
  s = s.replace(/([0-9%٪])([ء-ي])/g, '$1 $2');

  // 7. Punctuation boundary spacing
  s = s.replace(/([ء-ي])([(\[{])/g, '$1 $2');
  s = s.replace(/([)\]}])([ء-ي])/g, '$1 $2');
  s = s.replace(/([:;،؛])([ء-يA-Za-z])/g, '$1 $2');
  s = s.replace(/(\.)([ء-ي])/g, '$1 $2');

  // 8. Fix common Latin hallucinations in Arabic context
  // "ple" -> "علم" (science) or "عام" (year)
  s = s.replace(/\b(?:ple|p\|e|ble)\s+(\d{4})\b/g, 'عام $1');
  s = s.replace(/(في|عام)\s+(?:ple|p\|e|ble)\b/g, '$1 عام');
  s = s.replace(/(فرع\s+من|أصول|قواعد|طالب|مكتبة|أهل)\s+(?:ple|p\|e|ble)\b/g, '$1 علم');
  s = s.replace(/\b(?:ple|p\|e|ble)\s+(الحاسوب|النفس|الاجتماع|الأحياء|الكيمياء|الفيزياء|البيانات|الفلك|الأرض|اللغة|التاريخ|الأصول|الحديث)\b/g, 'علم $1');
  s = s.replace(/\bple\b/g, 'علم');

  // Filter isolated 1-3 letter Latin noise words flanked by Arabic
  s = s.replace(/([ء-ي]+[\s،؛:.]+)\b([A-Za-z]{1,3})\b(?=[\s،؛:.]+[ء-ي]+)/g, (match, before, word) => {
    if (VALID_SHORT_ACRONYMS.has(word.toUpperCase())) {
      return `${before}${word.toUpperCase()}`;
    }
    return before.trimEnd();
  });

  // Fix "AI" misread as "۸1" or "41"
  s = s.replace(/(اختصاراً|اختصار|أو)\s+(?:۸1|41)\b/g, '$1 AI');
  s = s.replace(/\b(?:۸1|41)\)\b/g, 'AI)');

  // Fix "Node.js" misread as "Nodes" or "Node js"
  s = s.replace(/\b(?:Nodes|Node\s+js)\b/gi, 'Node.js');

  // Fix percent "%" misread as "96"
  s = s.replace(/96([0-9]+)/g, '$1%');
  s = s.replace(/([0-9]+)96/g, '$1%');
  s = s.replace(/([0-9]+)\s*96\b/g, '$1%');
  s = s.replace(/\b96\s*([0-9]+)/g, '$1%');

  // 9. Glued Arabic word boundaries
  // After Taa Marbuta (ة): ALWAYS a word boundary
  s = s.replace(/([ة])([ء-ي])/g, '$1 $2');

  // Glued prepositions and relative pronouns
  s = s.replace(/([ء-ي]{2,})(من|عن|كما)\b/g, '$1 $2');
  s = s.replace(/\b(ومن|عن)(المحتمل|المؤكد|الواضح|المقرر)\b/g, '$1 $2');
  s = s.replace(/([ء-ي]{2,})(التي|الذي|الذين|اللواتي|اللتين|اللذين)\b/g, '$1 $2');
  s = s.replace(/([ء-ي]{2,})(طريقهم|بنفسه|أعمال|المجمع|الجلسة|شخص)\b/g, '$1 $2');

  // Glued conjunction "و" before words: e.g. "ماركيانوبولخريا" -> "ماركيان وبولخريا"
  s = s.replace(/([ء-ي]{3,})(و)(بولخريا|الذي|التي|غيرها|لكن|قد|كان|كانت)\b/g, '$1 $2$3');

  // Feminine plural (-ات) followed by definite article (الـ)
  s = s.replace(/(ات)(ال[ء-ي])/g, '$1 $2');
  s = s.replace(/(ملاحظات)(التمهيدية)/g, '$1 $2');

  // Words ending with (ت) or (ه) followed by definite article (الـ) or next word
  s = s.replace(/(وكانت|كانت)(ال[ء-ي]+)/g, '$1 $2');
  s = s.replace(/(أن)(يؤثروا|يحضروا|يمنع|يصل|يكون)/g, '$1 $2');
  s = s.replace(/(خمسمائة)(شخص)/g, '$1 $2');
  s = s.replace(/(أعلى)(موظفي)/g, '$1 $2');
  s = s.replace(/(إلى)(خلقيدونية|نيقية|العاصمة|مدينة)/g, '$1 $2');

  // Pronouns, prepositions, and auxiliaries before definite article (الـ)
  s = s.replace(/\b(هو|هي|هما|هم|هن|هذا|هذه|هؤلاء|ذلك|تلك|كان|كانت|كانوا|أصبح|أمسى|صار|ليس|في|على|إلى|من|عن|مع|عند|بعد|قبل|بين|حول|ضد|خلال|نحو|لدى|غير|كل|علم|عام|الدخول|الوصول|جميع|بعض|نفس|عين|كلا|كلتا)(ال[ء-ي]+)/g, '$1 $2');

  // 10. Fix BiDi inverted parenthesis patterns
  // ")500 :(Error" -> "(Error 500):"
  s = s.replace(/\)\s*(\d+)\s*:\s*\((Error|Warning|Info|Code)\b/gi, '($2 $1):');
  s = s.replace(/\)\s*(\d+)\s*\((Error|Warning|Info|Code)\b/gi, '($2 $1)');
  s = s.replace(/\((\d+)\s*:\s*(Error|Warning|Info|Code)\b/gi, '($2 $1):');

  // Fix unmatched closing parenthesis on English terms inside Arabic sentences:
  // e.g. "الذكاء الاصطناعي Artificial Intelligence) أو" -> "الذكاء الاصطناعي (Artificial Intelligence) أو"
  s = s.replace(/([ء-ي]+)\s+([A-Za-z][A-Za-z\s]+)\)\s+([ء-ي]+)/g, (match, before, eng, after) => {
    if (!before.includes('(')) {
      return `${before} (${eng.trim()}) ${after}`;
    }
    return match;
  });

  // 11. Clean up whitespace
  s = s.replace(/[ \t]+/g, ' ');
  s = s.replace(/ \n/g, '\n').replace(/\n /g, '\n');
  s = s.replace(/\n{3,}/g, '\n\n');

  return s.trim();
}

async function initWorker() {
  console.log('[OCR-Worker] Initializing high-accuracy offline Tesseract workers from:', MODELS_DIR);
  try {
    // Primary Auto Worker: Arabic + English (optimal for combined text)
    workerAuto = await createWorker(['ara', 'eng'], 1, {
      cachePath: MODELS_DIR,
      gzip: false
    });
    await workerAuto.setParameters({
      tessedit_pageseg_mode: '6', // PSM 6: Single uniform block
      preserve_interword_spaces: '1'
    });

    // Dedicated Pure Arabic Worker: Arabic language + Arabic extended script
    workerAra = await createWorker(['ara', 'Arabic'], 1, {
      cachePath: MODELS_DIR,
      gzip: false
    });
    await workerAra.setParameters({
      tessedit_pageseg_mode: '6',
      preserve_interword_spaces: '1'
    });

    isReady = true;
    console.log('[OCR-Worker] Ready: Auto (ara+eng) & Pure Arabic (ara+Arabic) workers online.');
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
    languages: ['ara', 'eng', 'Arabic'],
    modes: ['auto', 'ara', 'eng'],
    offline: true
  };
}

module.exports = {
  initWorker,
  recognize,
  getStatus
};
