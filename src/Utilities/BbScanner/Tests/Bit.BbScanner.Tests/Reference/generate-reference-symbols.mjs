// Generates ReferenceSymbols.json: symbols rendered by an encoder this library has nothing to do with.
//
// The decoder tests otherwise read only what the test kit's own encoders write, so a mistake made identically on
// both sides - a wrong table entry, a misplaced module, a mis-specified mask - would pass every round trip. These
// fixtures come from BWIPP (Barcode Writer in Pure PostScript, MIT licensed), through its JavaScript port bwip-js,
// which is widely used to print production labels. They are module grids and bar widths, not pictures, so the
// tests render them at any scale and push them through the same camera transforms as everything else.
//
// The fixtures are checked in; regenerating them is only needed to add a case. bwip-js is deliberately not a
// dependency of anything in the repository:
//
//     npm install --prefix <some scratch folder> bwip-js
//     node generate-reference-symbols.mjs <scratch folder>/node_modules/bwip-js/dist/bwip-js-node.js > ReferenceSymbols.json

import { createRequire } from 'node:module';
import { resolve } from 'node:path';

const require = createRequire(import.meta.url);
const bwipjs = require(resolve(process.argv[2]));

// bwip-js hands BWIPP the UTF-8 bytes of the text it is given, unless the options say 'binarytext', in which case
// every character is taken as one byte. Text cases are therefore plain strings; byte cases use bytes() and say
// binarytext.

/** A string of arbitrary bytes, for byte compaction and Base 256; the case must also say binarytext. */
const bytes = values => String.fromCharCode(...values);

const GS = '\u001d';

/** Thirteen digits and their GTIN check digit: weights three and one from the right. */
const gtin = digits => {
    let sum = 0;
    for (let i = 0; i < digits.length; i++) sum += Number(digits[i]) * ((digits.length - i) % 2 === 1 ? 3 : 1);
    return digits + ((10 - (sum % 10)) % 10);
};

// Each case: an id, the BbBarcodeFormat it must decode as, the BWIPP encoder and its options, the data handed to
// the encoder, and what the decoder must report. `gs1` is the expected BbBarcodeResultMetadata.IsGs1, and
// `decoderOptions` names the BbScannerOptions a case needs beyond the defaults.
const cases = [
    // PDF417: every compaction mode, a spread of shapes and every security level.
    { id: 'pdf417-text', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4', data: 'PDF417 reference symbol, bit platform', expected: 'PDF417 reference symbol, bit platform' },
    { id: 'pdf417-numeric', format: 'Pdf417', bcid: 'pdf417', options: 'columns=3', data: '12345678901234567890123456789012345678901234', expected: '12345678901234567890123456789012345678901234' },
    { id: 'pdf417-mixed-punctuation', format: 'Pdf417', bcid: 'pdf417', options: 'columns=5', data: 'Hello, World! 123 abc DEF @#$%&*()[]{}<>;:', expected: 'Hello, World! 123 abc DEF @#$%&*()[]{}<>;:' },
    { id: 'pdf417-utf8-bytes', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4', data: 'Grüße aus Köln – ☃ 東京', expected: 'Grüße aus Köln – ☃ 東京' },
    { id: 'pdf417-binary', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129]), expectedBytes: [0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129] },
    // Byte compaction of every length class: a multiple of six bytes latches with 924, anything else with 901, whose
    // last group of one to five bytes is one codeword a byte - five of them looking just like a packed group.
    { id: 'pdf417-bytes-5', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([11, 48, 85, 122, 159]), expectedBytes: [11, 48, 85, 122, 159] },
    { id: 'pdf417-bytes-11', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125]), expectedBytes: [11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125] },
    { id: 'pdf417-bytes-13', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199]), expectedBytes: [11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199] },
    { id: 'pdf417-bytes-17', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199, 236, 17, 54, 91]), expectedBytes: [11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199, 236, 17, 54, 91] },
    { id: 'pdf417-bytes-18', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 binarytext', data: bytes([11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199, 236, 17, 54, 91, 128]), expectedBytes: [11, 48, 85, 122, 159, 196, 233, 14, 51, 88, 125, 162, 199, 236, 17, 54, 91, 128] },
    { id: 'pdf417-eci-utf8', format: 'Pdf417', bcid: 'pdf417', options: 'columns=4 parsefnc', data: '^ECI000026Ελληνικά and Русский', expected: 'Ελληνικά and Русский', eci: 26 },
    { id: 'pdf417-security-0', format: 'Pdf417', bcid: 'pdf417', options: 'columns=2 eclevel=0', data: 'LEVEL0', expected: 'LEVEL0' },
    { id: 'pdf417-security-4', format: 'Pdf417', bcid: 'pdf417', options: 'columns=6 eclevel=4', data: 'Security level four keeps reading through damage.', expected: 'Security level four keeps reading through damage.' },
    { id: 'pdf417-security-8', format: 'Pdf417', bcid: 'pdf417', options: 'columns=12 eclevel=8', data: 'MAX', expected: 'MAX' },
    { id: 'pdf417-wide', format: 'Pdf417', bcid: 'pdf417', options: 'columns=12', data: 'A wide symbol with twelve data columns and enough text to fill several rows of them.', expected: 'A wide symbol with twelve data columns and enough text to fill several rows of them.' },
    { id: 'pdf417-tall', format: 'Pdf417', bcid: 'pdf417', options: 'columns=1', data: 'TALL1', expected: 'TALL1' },
    {
        id: 'pdf417-driving-licence', format: 'Pdf417', bcid: 'pdf417', options: 'columns=10 eclevel=5',
        data: '@\n\u001e\rANSI 636000090002DL00410278ZV03190008DLDAQT64235789\nDCSSAMPLE\nDDEN\nDACMICHAEL\nDDFN\nDADJOHN\nDDGN\nDCUJR\nDCAD\nDCBK\nDCDPH\nDBD06062019\nDBB06061986\nDBA12102024\nDBC1\nDAU068 in\nDAYBRO\nDAG2300 WEST BROAD STREET\nDAIRICHMOND\nDAJVA\nDAK232690000  \nDCF2424244747474786102204\nDCGUSA\nDCK123456789\nDDAF\nDDB06062018\nDDC06062020\nDDD1\r',
        expected: '@\n\u001e\rANSI 636000090002DL00410278ZV03190008DLDAQT64235789\nDCSSAMPLE\nDDEN\nDACMICHAEL\nDDFN\nDADJOHN\nDDGN\nDCUJR\nDCAD\nDCBK\nDCDPH\nDBD06062019\nDBB06061986\nDBA12102024\nDBC1\nDAU068 in\nDAYBRO\nDAG2300 WEST BROAD STREET\nDAIRICHMOND\nDAJVA\nDAK232690000  \nDCF2424244747474786102204\nDCGUSA\nDCK123456789\nDDAF\nDDB06062018\nDDC06062020\nDDD1\r',
    },

    // Compact (truncated) PDF417: the right row indicator dropped and the stop pattern cut to one bar, for a narrower
    // symbol; the left row indicators carry the structure alone.
    { id: 'pdf417compact-text', format: 'Pdf417', bcid: 'pdf417compact', options: 'columns=3', data: 'Compact PDF417, bit platform', expected: 'Compact PDF417, bit platform' },
    { id: 'pdf417compact-numeric', format: 'Pdf417', bcid: 'pdf417compact', options: 'columns=2', data: '314159265358979323846264338327950288', expected: '314159265358979323846264338327950288' },
    { id: 'pdf417compact-wide', format: 'Pdf417', bcid: 'pdf417compact', options: 'columns=8 eclevel=3', data: 'A compact symbol eight columns wide, at security level three, for a ticket.', expected: 'A compact symbol eight columns wide, at security level three, for a ticket.' },
    { id: 'pdf417compact-bytes', format: 'Pdf417', bcid: 'pdf417compact', options: 'columns=4 binarytext', data: bytes([0, 7, 64, 128, 200, 255, 13, 10, 31, 99]), expectedBytes: [0, 7, 64, 128, 200, 255, 13, 10, 31, 99] },
    { id: 'pdf417compact-one-column', format: 'Pdf417', bcid: 'pdf417compact', options: 'columns=1', data: 'ONE', expected: 'ONE' },

    // MicroPDF417: one to four columns between row address patterns, no start or stop pattern, its error correction
    // fixed by the symbol's size.
    { id: 'micropdf417-1col', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=1', data: 'MICRO 1', expected: 'MICRO 1' },
    { id: 'micropdf417-2col', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=2', data: 'MicroPDF417, two columns', expected: 'MicroPDF417, two columns' },
    { id: 'micropdf417-3col', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=3', data: 'MicroPDF417 in three columns, with a centre row address pattern.', expected: 'MicroPDF417 in three columns, with a centre row address pattern.' },
    { id: 'micropdf417-4col', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=4', data: 'MicroPDF417 four columns wide: the largest variant carries the most text of all, here some ninety characters long.', expected: 'MicroPDF417 four columns wide: the largest variant carries the most text of all, here some ninety characters long.' },
    { id: 'micropdf417-numeric', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=2', data: '0123456789012345678901234567890123456789', expected: '0123456789012345678901234567890123456789' },
    { id: 'micropdf417-bytes', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=3 binarytext', data: bytes([0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129]), expectedBytes: [0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129] },
    { id: 'micropdf417-utf8', format: 'MicroPdf417', bcid: 'micropdf417', options: 'columns=4', data: 'Grüße aus Köln ☃', expected: 'Grüße aus Köln ☃' },

    // QR Code: versions with and without version information, every error correction level and every mode.
    { id: 'qr-v1-numeric', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=L version=1', data: '01234567890123456789', expected: '01234567890123456789' },
    { id: 'qr-v2-alphanumeric', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=M version=2', data: 'HTTPS://BITPLATFORM.DEV/BBSCANNER', expected: 'HTTPS://BITPLATFORM.DEV/BBSCANNER' },
    { id: 'qr-v5-byte-q', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=Q version=5', data: 'https://bitplatform.dev/scanner?ref=reference&v=5', expected: 'https://bitplatform.dev/scanner?ref=reference&v=5' },
    { id: 'qr-v7-version-info', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=H version=7', data: 'Version seven is the first to carry version information blocks.', expected: 'Version seven is the first to carry version information blocks.' },
    { id: 'qr-v10-mixed-modes', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=M version=10', data: 'ORDER 0012345678901234 shipped to Example Street 42, Springfield; tracking 9988776655443322', expected: 'ORDER 0012345678901234 shipped to Example Street 42, Springfield; tracking 9988776655443322' },
    { id: 'qr-v25', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=L version=25', data: 'x'.repeat(400), expected: 'x'.repeat(400) },
    { id: 'qr-v40', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=L version=40', data: 'Version forty. '.repeat(60), expected: 'Version forty. '.repeat(60) },
    { id: 'qr-utf8', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=M', data: 'Grüße – 東京 – ☃', expected: 'Grüße – 東京 – ☃' },
    { id: 'qr-eci-utf8', format: 'QrCode', bcid: 'qrcode', options: 'eclevel=M parsefnc', data: '^ECI000026Ελληνικά and Русский', expected: 'Ελληνικά and Русский', eci: 26 },
    { id: 'qr-gs1', format: 'QrCode', bcid: 'gs1qrcode', options: '', data: '(01)09521234543213(10)LOT42(17)261231', expected: '0109521234543213' + '10LOT42' + GS + '17261231', gs1: true },

    // Data Matrix: square and rectangular shapes, and the encodation BWIPP picks for each kind of content.
    { id: 'dm-10x10', format: 'DataMatrix', bcid: 'datamatrix', options: 'version=10x10', data: '123456', expected: '123456' },
    { id: 'dm-c40', format: 'DataMatrix', bcid: 'datamatrix', options: '', data: 'UPPERCASE TEXT ENCODES IN C40 MODE 0123456789', expected: 'UPPERCASE TEXT ENCODES IN C40 MODE 0123456789' },
    { id: 'dm-text', format: 'DataMatrix', bcid: 'datamatrix', options: '', data: 'lowercase text encodes in text mode abcdefghijkl', expected: 'lowercase text encodes in text mode abcdefghijkl' },
    { id: 'dm-x12', format: 'DataMatrix', bcid: 'datamatrix', options: '', data: 'X12*DATA>WITH*SEPARATORS>AND*STARS>X12*X12*', expected: 'X12*DATA>WITH*SEPARATORS>AND*STARS>X12*X12*' },
    { id: 'dm-edifact', format: 'DataMatrix', bcid: 'datamatrix', options: '', data: 'EDIFACT:+?\'@/.EDIFACT:+?\'@/.EDIFACT', expected: 'EDIFACT:+?\'@/.EDIFACT:+?\'@/.EDIFACT' },
    { id: 'dm-base256', format: 'DataMatrix', bcid: 'datamatrix', options: 'binarytext', data: bytes([0xd0, 0x9f, 0xd1, 0x80, 0xd0, 0xb8, 0xd0, 0xb2, 0xd0, 0xb5, 0xd1, 0x82, 0x20, 0xf0, 0x9f, 0x8c, 0x8d]), expected: 'Привет 🌍' },
    { id: 'dm-rect-8x32', format: 'DataMatrix', bcid: 'datamatrix', options: 'version=8x32', data: 'RECT8X32', expected: 'RECT8X32' },
    { id: 'dm-rect-16x48', format: 'DataMatrix', bcid: 'datamatrix', options: 'version=16x48', data: 'Rectangular sixteen by forty-eight', expected: 'Rectangular sixteen by forty-eight' },
    { id: 'dm-64x64', format: 'DataMatrix', bcid: 'datamatrix', options: 'version=64x64', data: 'Four data regions a side. '.repeat(8), expected: 'Four data regions a side. '.repeat(8) },
    { id: 'dm-144x144', format: 'DataMatrix', bcid: 'datamatrix', options: 'version=144x144', data: 'The largest symbol. '.repeat(40), expected: 'The largest symbol. '.repeat(40) },
    { id: 'dm-gs1', format: 'DataMatrix', bcid: 'gs1datamatrix', options: '', data: '(01)09521234543213(10)LOT42(17)261231', expected: '0109521234543213' + '10LOT42' + GS + '17261231', gs1: true },

    // Code 128: each code set, shifts between them, and GS1-128.
    { id: 'code128-b', format: 'Code128', bcid: 'code128', options: '', data: 'Hello, World!', expected: 'Hello, World!' },
    { id: 'code128-c', format: 'Code128', bcid: 'code128', options: '', data: '12345678901234567890', expected: '12345678901234567890' },
    { id: 'code128-a-controls', format: 'Code128', bcid: 'code128', options: 'parse', data: 'TAB^009SEP^029END', expected: 'TAB\tSEP' + GS + 'END' },
    { id: 'code128-mixed', format: 'Code128', bcid: 'code128', options: '', data: 'ABC123456789def', expected: 'ABC123456789def' },
    { id: 'gs1-128', format: 'Code128', bcid: 'gs1-128', options: '', data: '(01)09521234543213(10)ABC123(15)261231', expected: '0109521234543213' + '10ABC123' + GS + '15261231', gs1: true },

    // Code 39, plain and with the options that change how it reads.
    { id: 'code39', format: 'Code39', bcid: 'code39', options: '', data: 'CODE39 TEST-123', expected: 'CODE39 TEST-123' },
    { id: 'code39-check', format: 'Code39', bcid: 'code39', options: 'includecheck', data: 'CHECKED-42', expected: 'CHECKED-42', decoderOptions: ['Code39CheckDigit'] },
    { id: 'code39-full-ascii', format: 'Code39', bcid: 'code39ext', options: '', data: 'Full ASCII: a+b', expected: 'Full ASCII: a+b', decoderOptions: ['Code39ExtendedMode'] },

    // EAN and UPC.
    { id: 'ean13', format: 'Ean13', bcid: 'ean13', options: '', data: '4006381333931', expected: '4006381333931' },
    { id: 'ean13-isbn', format: 'Ean13', bcid: 'ean13', options: '', data: '9780201379624', expected: '9780201379624' },
    { id: 'ean8', format: 'Ean8', bcid: 'ean8', options: '', data: '96385074', expected: '96385074' },
    { id: 'upca', format: 'UpcA', bcid: 'upca', options: '', data: '036000291452', expected: '036000291452' },
    { id: 'upce', format: 'UpcE', bcid: 'upce', options: '', data: '01234565', expected: '01234565' },

    // Interleaved 2 of 5, including the ITF-14 shipping carton code.
    { id: 'itf', format: 'Itf', bcid: 'interleaved2of5', options: '', data: '1234567890', expected: '1234567890' },
    { id: 'itf14', format: 'Itf', bcid: 'itf14', options: '', data: '15400141288763', expected: '15400141288763' },

    // EAN and UPC with the two and five digit add-ons of magazines and books.
    { id: 'ean13-addon5', format: 'Ean13', bcid: 'ean13', options: '', data: '9780201379624 51299', expected: '9780201379624', extension: '51299' },
    { id: 'ean13-addon2', format: 'Ean13', bcid: 'ean13', options: '', data: '4006381333931 05', expected: '4006381333931', extension: '05', decoderOptions: ['RequireEanExtension'] },
    { id: 'upca-addon5', format: 'UpcA', bcid: 'upca', options: '', data: '036000291452 12345', expected: '036000291452', extension: '12345' },
    { id: 'upce-addon2', format: 'UpcE', bcid: 'upce', options: '', data: '01234565 12', expected: '01234565', extension: '12' },

    // Codabar: digits, the six symbols, and the start and stop characters kept when asked for.
    { id: 'codabar', format: 'Codabar', bcid: 'rationalizedCodabar', options: '', data: 'A0123456789B', expected: '0123456789' },
    { id: 'codabar-symbols', format: 'Codabar', bcid: 'rationalizedCodabar', options: '', data: 'C$-:/.+D', expected: '$-:/.+' },
    { id: 'codabar-library', format: 'Codabar', bcid: 'rationalizedCodabar', options: '', data: 'A31234000123457A', expected: '31234000123457' },
    { id: 'codabar-start-stop', format: 'Codabar', bcid: 'rationalizedCodabar', options: '', data: 'D40156B', expected: 'D40156B', decoderOptions: ['CodabarStartStop'] },

    // Aztec Code: compact and full range symbols across every codeword size (6, 8, 10 and 12 bits, which change at
    // 3, 9 and 23 layers), every character mode, bytes, ECI and GS1.
    { id: 'aztec-compact-1', format: 'Aztec', bcid: 'azteccode', options: 'format=compact layers=1', data: 'BBSCAN', expected: 'BBSCAN' },
    { id: 'aztec-compact-2', format: 'Aztec', bcid: 'azteccode', options: 'format=compact layers=2', data: 'Compact, two layers', expected: 'Compact, two layers' },
    { id: 'aztec-compact-3', format: 'Aztec', bcid: 'azteccode', options: 'format=compact layers=3', data: 'Three layers of eight bit codewords', expected: 'Three layers of eight bit codewords' },
    { id: 'aztec-compact-4', format: 'Aztec', bcid: 'azteccode', options: 'format=compact layers=4', data: 'The largest compact symbol holds this sentence and some more.', expected: 'The largest compact symbol holds this sentence and some more.' },
    { id: 'aztec-full-1', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=1', data: 'FULL1', expected: 'FULL1' },
    { id: 'aztec-full-4', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=4', data: 'Full range, four layers, 0123456789', expected: 'Full range, four layers, 0123456789' },
    { id: 'aztec-full-8', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=8', data: 'Eight layers is the last of the eight bit codewords. '.repeat(3), expected: 'Eight layers is the last of the eight bit codewords. '.repeat(3) },
    { id: 'aztec-full-12', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=12', data: 'Ten bit codewords and a reference grid line either side. '.repeat(6), expected: 'Ten bit codewords and a reference grid line either side. '.repeat(6) },
    { id: 'aztec-full-23', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=23', data: 'Twelve bit codewords from layer 23. '.repeat(40), expected: 'Twelve bit codewords from layer 23. '.repeat(40) },
    { id: 'aztec-full-32', format: 'Aztec', bcid: 'azteccode', options: 'format=full layers=32', data: 'The largest Aztec symbol, 151 modules. '.repeat(60), expected: 'The largest Aztec symbol, 151 modules. '.repeat(60) },
    { id: 'aztec-modes', format: 'Aztec', bcid: 'azteccode', options: '', data: 'Hello, World! 123 abc DEF @#$%&*()[]{}<>;:\t~|_^\r\nend. 3.14, 2.71: done', expected: 'Hello, World! 123 abc DEF @#$%&*()[]{}<>;:\t~|_^\r\nend. 3.14, 2.71: done' },
    { id: 'aztec-binary', format: 'Aztec', bcid: 'azteccode', options: 'binarytext', data: bytes([0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129, 65, 66]), expectedBytes: [0, 1, 2, 3, 250, 251, 252, 253, 254, 255, 128, 129, 65, 66] },
    { id: 'aztec-utf8', format: 'Aztec', bcid: 'azteccode', options: '', data: 'Grüße aus Köln – ☃ 東京', expected: 'Grüße aus Köln – ☃ 東京' },
    { id: 'aztec-eci-utf8', format: 'Aztec', bcid: 'azteccode', options: 'parsefnc', data: '^ECI000026Ελληνικά and Русский', expected: 'Ελληνικά and Русский', eci: 26 },
    { id: 'aztec-gs1', format: 'Aztec', bcid: 'azteccode', options: 'parsefnc', data: '^FNC10109521234543213' + '10LOT42^FNC1' + '17261231', expected: '0109521234543213' + '10LOT42' + GS + '17261231', gs1: true },
    { id: 'aztec-boarding-pass', format: 'Aztec', bcid: 'azteccode', options: '', data: 'M1DESMARAIS/LUC       EABC123 YULFRAAC 0834 326J001A0025 100', expected: 'M1DESMARAIS/LUC       EABC123 YULFRAAC 0834 326J001A0025 100' },
    { id: 'aztec-eclevel-50', format: 'Aztec', bcid: 'azteccode', options: 'eclevel=50', data: 'Half of this symbol is error correction.', expected: 'Half of this symbol is error correction.' },
    // Aztec Runes: the bare compact core, its mode message carrying a number from 0 to 255.
    { id: 'aztec-rune-0', format: 'Aztec', bcid: 'aztecrune', options: '', data: '0', expected: '0' },
    { id: 'aztec-rune-25', format: 'Aztec', bcid: 'aztecrune', options: '', data: '25', expected: '25' },
    { id: 'aztec-rune-170', format: 'Aztec', bcid: 'aztecrune', options: '', data: '170', expected: '170' },
    { id: 'aztec-rune-255', format: 'Aztec', bcid: 'aztecrune', options: '', data: '255', expected: '255' },

    // GS1 DataBar Omnidirectional and Truncated, a GTIN-14 each: the smallest and largest numbers the symbol holds and
    // a spread between, which puts every character group to work.
    ...['0000000000000', '0952123454321', '0001234567890', '2401234567890', '5012345678900', '9999999999999'].map((digits, i) => ({
        id: `databar-omni-${i}`, format: 'DataBar', bcid: 'databaromni', options: '', data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),
    ...['0952123454321', '1234567890123'].map((digits, i) => ({
        id: `databar-truncated-${i}`, format: 'DataBar', bcid: 'databartruncated', options: '', data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),
    // The stacked forms: the left half on one row and the right half on the next, which no single line crosses both of.
    ...['0952123454321', '2401234567890', '9999999999999'].map((digits, i) => ({
        id: `databar-stacked-${i}`, format: 'DataBar', bcid: 'databarstacked', options: '', rowHeights: [5, 1, 7], data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),
    ...['0952123454321', '5012345678900', '0000000000000'].map((digits, i) => ({
        id: `databar-stackedomni-${i}`, format: 'DataBar', bcid: 'databarstackedomni', options: '', rowHeights: [33, 1, 1, 1, 33], data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),

    // GS1 DataBar Limited: a GTIN-14 starting with 0 or 1, from the smallest to the largest it holds.
    ...['0000000000000', '0952123454321', '1501234567890', '1999999999999', '0123456789012', '1000000000001'].map((digits, i) => ({
        id: `databar-limited-${i}`, format: 'DataBarLimited', bcid: 'databarlimited', options: '', data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),

    // GS1 DataBar Expanded: any GS1 element string, through every encodation method - the compressed forms for a GTIN
    // with a weight, a price or a date, a GTIN followed by anything, and the general method. An element whose AI is not
    // one of the predefined lengths - 8005 is fixed, but not predefined - is followed by a group separator unless it is
    // the last.
    { id: 'databar-expanded-weight-kg', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)90614141000015(3103)000123', expected: '0190614141000015' + '3103000123', gs1: true },
    { id: 'databar-expanded-weight-lb', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)90614141000015(3202)009999', expected: '0190614141000015' + '3202009999', gs1: true },
    { id: 'databar-expanded-price', format: 'DataBarExpanded', bcid: 'databarexpanded', options: 'dontlint', data: '(01)90614141000015(3922)795', expected: '0190614141000015' + '3922795', gs1: true },
    { id: 'databar-expanded-price-currency', format: 'DataBarExpanded', bcid: 'databarexpanded', options: 'dontlint', data: '(01)90614141000015(3932)9781234', expected: '0190614141000015' + '39329781234', gs1: true },
    { id: 'databar-expanded-weight-date', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)90614141000015(3102)001750(11)100312', expected: '0190614141000015' + '3102001750' + '11100312', gs1: true },
    { id: 'databar-expanded-weight-lb-date', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)90614141000015(3203)012345(17)251231', expected: '0190614141000015' + '3203012345' + '17251231', gs1: true },
    { id: 'databar-expanded-gtin-lot', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)09521234543213(10)LOT42(17)261231', expected: '0109521234543213' + '10LOT42' + GS + '17261231', gs1: true },
    { id: 'databar-expanded-gtin-text', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)09521234543213(21)aZ-/%*+,.:;=?_"!&\'<>', expected: '0109521234543213' + '21aZ-/%*+,.:;=?_"!&\'<>', gs1: true },
    { id: 'databar-expanded-general', format: 'DataBarExpanded', bcid: 'databarexpanded', options: 'dontlint', data: '(10)ABC123(21)XYZ', expected: '10ABC123' + GS + '21XYZ', gs1: true },
    { id: 'databar-expanded-numeric', format: 'DataBarExpanded', bcid: 'databarexpanded', options: 'dontlint', data: '(8005)123456(7001)1234567890123', expected: '8005123456' + GS + '70011234567890123', gs1: true },
    { id: 'databar-expanded-long', format: 'DataBarExpanded', bcid: 'databarexpanded', options: '', data: '(01)09521234543213(10)ABCDEFGHIJKL(11)241231(21)SERIAL0123456789', expected: '0109521234543213' + '10ABCDEFGHIJKL' + GS + '11241231' + '21SERIAL0123456789', gs1: true },
    // Expanded Stacked: the same symbol characters in rows of a set number of segments, each row 34 modules tall and
    // three modules of separator between them.
    ...[
        ['(01)90614141000015(3103)000123', '0190614141000015' + '3103000123', 4],
        ['(01)09521234543213(10)LOT42(17)261231', '0109521234543213' + '10LOT42' + GS + '17261231', 4],
        ['(01)09521234543213(10)ABCDEFGHIJKL(11)241231(21)SERIAL0123456789', '0109521234543213' + '10ABCDEFGHIJKL' + GS + '11241231' + '21SERIAL0123456789', 6],
        ['(01)09521234543213(3103)000123(15)991231(10)A1', '0109521234543213' + '3103000123' + '15991231' + '10A1', 2],
    ].map(([data, expected, segments], i) => ({
        id: `databar-expandedstacked-${i}`, format: 'DataBarExpanded', bcid: 'databarexpandedstacked', options: `segments=${segments}`, data, expected, gs1: true,
        rowHeights: rows => Array.from({ length: rows }, (_, r) => r % 4 === 0 ? 34 : 1),
    })),

    // Micro QR: every version and error correction level - M1 numeric only, with its four bit last codeword as M3 has,
    // M2 alphanumeric, M3 and M4 bytes - from the smallest symbol, 11 modules square, to the largest, 17.
    { id: 'microqr-m1', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M1', data: '12345', expected: '12345' },
    { id: 'microqr-m2-l', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M2 eclevel=L', data: 'ABC123', expected: 'ABC123' },
    { id: 'microqr-m2-m', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M2 eclevel=M', data: '012345', expected: '012345' },
    { id: 'microqr-m3-l', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M3 eclevel=L', data: 'Micro QR', expected: 'Micro QR' },
    { id: 'microqr-m3-m', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M3 eclevel=M', data: '1234567890123', expected: '1234567890123' },
    { id: 'microqr-m4-l', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M4 eclevel=L', data: 'MICRO QR CODE 2005', expected: 'MICRO QR CODE 2005' },
    { id: 'microqr-m4-m', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M4 eclevel=M', data: 'bitplatform', expected: 'bitplatform' },
    { id: 'microqr-m4-q', format: 'MicroQrCode', bcid: 'microqrcode', options: 'version=M4 eclevel=Q', data: 'M4Q 123', expected: 'M4Q 123' },

    // MaxiCode: the structured carrier message of modes 2 (a numeric postal code) and 3 (an alphanumeric one), reported
    // the way the carrier wrote it; a standard symbol (mode 4) in every code set, with numeric shifts, an ECI and bytes;
    // and enhanced error correction (mode 5).
    { id: 'maxicode-mode2', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=2 parse', data: '[)>^03001^02996152382802^029840^029001^0291Z00004951^029UPSN^02906X610^029159^0291234567^0291/1^029^029Y^029634 ALPHA DR^029PITTSBURGH^029PA^030^004', expected: '[)>\u001e01\u001d96152382802\u001d840\u001d001\u001d1Z00004951\u001dUPSN\u001d06X610\u001d159\u001d1234567\u001d1/1\u001d\u001dY\u001d634 ALPHA DR\u001dPITTSBURGH\u001dPA\u001e\u0004' },
    { id: 'maxicode-mode3', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=3 parse', data: '[)>^03001^02996B1050^029056^029999^0291Z00004951^029UPSN^02906X610^029159^0291234567^0291/1^029^029Y^029RUE DE LA LOI^029BRUXELLES^030^004', expected: '[)>\u001e01\u001d96B1050\u001d056\u001d999\u001d1Z00004951\u001dUPSN\u001d06X610\u001d159\u001d1234567\u001d1/1\u001d\u001dY\u001dRUE DE LA LOI\u001dBRUXELLES\u001e\u0004' },
    { id: 'maxicode-mode4', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=4', data: 'MaxiCode reference symbol, bit platform: {Lower} and UPPER #42', expected: 'MaxiCode reference symbol, bit platform: {Lower} and UPPER #42' },
    { id: 'maxicode-numeric', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=4', data: '123456789012345678901234567890123456789012345678901234567890', expected: '123456789012345678901234567890123456789012345678901234567890' },
    { id: 'maxicode-latin1', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=4', data: 'Grüße aus Köln, ½ €', expected: 'Grüße aus Köln, ½ €' },
    { id: 'maxicode-eci', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=4 parsefnc', data: '^ECI000026Ελληνικά', expected: 'Ελληνικά', eci: 26 },
    { id: 'maxicode-mode5', format: 'MaxiCode', bcid: 'maxicode', options: 'mode=5', data: 'Enhanced error correction: 68 data codewords', expected: 'Enhanced error correction: 68 data codewords' },

    // Code 93 always carries its two check characters; BWIPP only writes them when asked to.
    { id: 'code93', format: 'Code93', bcid: 'code93', options: 'includecheck', data: 'CODE 93 TEST', expected: 'CODE 93 TEST' },
    { id: 'code93-digits', format: 'Code93', bcid: 'code93', options: 'includecheck', data: '1234567890', expected: '1234567890' },
    { id: 'code93-full-ascii', format: 'Code93', bcid: 'code93ext', options: 'includecheck', data: 'Hello, World!', expected: 'Hello, World!' },
];

const symbols = [];
for (const c of cases) {
    const rendered = bwipjs.raw(c.bcid, c.data, c.options);
    const symbol = rendered[0];
    const entry = { id: c.id, format: c.format, encoder: `${c.bcid} ${c.options}`.trim() };
    if (c.expected !== undefined) entry.expected = c.expected;
    if (c.expectedBytes !== undefined) entry.expectedBytes = Buffer.from(c.expectedBytes).toString('hex');
    if (c.gs1) entry.gs1 = true;
    if (c.extension) entry.extension = c.extension;
    if (c.eci) entry.eci = c.eci;
    if (c.decoderOptions) entry.decoderOptions = c.decoderOptions;

    if (symbol.sbs) {
        // Linear: element widths, bar first.
        entry.widths = Array.from(symbol.sbs, w => Math.round(w));
    } else if (c.bcid === 'maxicode') {
        // MaxiCode comes back as the dark modules of its 30 by 33 hexagonal grid, numbered row by row; it is stored as
        // that grid, the bullseye left for the renderer to draw.
        const dark = new Set(symbol.pixs);
        entry.width = 30;
        entry.rows = [];
        for (let y = 0; y < 33; y++) {
            let row = '';
            for (let x = 0; x < 32; x += 4) {
                let nibble = 0;
                for (let b = 0; b < 4; b++) nibble = (nibble << 1) | (x + b < 30 && dark.has((y * 30) + x + b) ? 1 : 0);
                row += nibble.toString(16);
            }

            entry.rows.push(row);
        }
    } else {
        // Matrix: one hex string per row of modules, most significant bit first, a set bit dark, the last digit
        // padded with light modules.
        entry.width = symbol.pixx;
        // Stacked symbols such as PDF417 come back with one bitmap row per symbol row while pixy is the rendered
        // height, rows times the row height multiplier; each row is repeated that many times so the grid is to
        // scale (PDF417 rows are at least three modules tall).
        const bitmapRows = symbol.pixs.length / symbol.pixx;
        // The stacked DataBar forms have rows of their own heights - the rows of symbol characters and the separators
        // between them - given outright, or by how many rows the symbol came out with.
        const heights = typeof c.rowHeights === 'function'
            ? c.rowHeights(bitmapRows)
            : c.rowHeights ?? Array(bitmapRows).fill(Math.round(symbol.pixy / bitmapRows));
        if (!Number.isInteger(bitmapRows) || heights.length !== bitmapRows || heights.some(h => h < 1) || heights.reduce((a, b) => a + b, 0) !== symbol.pixy) {
            throw new Error(`${c.id}: ${symbol.pixs.length} modules do not tile ${symbol.pixx} x ${symbol.pixy}`);
        }

        const rows = [];
        for (let y = 0; y < bitmapRows; y++) {
            let row = '';
            for (let x = 0; x < symbol.pixx; x += 4) {
                let nibble = 0;
                for (let b = 0; b < 4; b++) {
                    const dark = x + b < symbol.pixx && symbol.pixs[(y * symbol.pixx) + x + b];
                    nibble = (nibble << 1) | (dark ? 1 : 0);
                }

                row += nibble.toString(16);
            }

            for (let r = 0; r < heights[y]; r++) rows.push(row);
        }

        entry.rows = rows;
    }

    symbols.push(entry);
}

// With a byte order mark, as the repository's .editorconfig asks of every file.
process.stdout.write('\uFEFF' + JSON.stringify({
    generator: `BWIPP ${bwipjs.BWIPP_VERSION} via bwip-js ${bwipjs.BWIPJS_VERSION}`,
    symbols,
}, null, 1) + '\n');
