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

    // GS1 DataBar Omnidirectional and Truncated, a GTIN-14 each: the smallest and largest numbers the symbol holds and
    // a spread between, which puts every character group to work.
    ...['0000000000000', '0952123454321', '0001234567890', '2401234567890', '5012345678900', '9999999999999'].map((digits, i) => ({
        id: `databar-omni-${i}`, format: 'DataBar', bcid: 'databaromni', options: '', data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),
    ...['0952123454321', '1234567890123'].map((digits, i) => ({
        id: `databar-truncated-${i}`, format: 'DataBar', bcid: 'databartruncated', options: '', data: `(01)${gtin(digits)}`, expected: `01${gtin(digits)}`, gs1: true,
    })),

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
    } else {
        // Matrix: one hex string per row of modules, most significant bit first, a set bit dark, the last digit
        // padded with light modules.
        entry.width = symbol.pixx;
        // Stacked symbols such as PDF417 come back with one bitmap row per symbol row while pixy is the rendered
        // height, rows times the row height multiplier; each row is repeated that many times so the grid is to
        // scale (PDF417 rows are at least three modules tall).
        const bitmapRows = symbol.pixs.length / symbol.pixx;
        const repeat = Math.round(symbol.pixy / bitmapRows);
        if (!Number.isInteger(bitmapRows) || repeat < 1 || bitmapRows * repeat !== symbol.pixy) {
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

            for (let r = 0; r < repeat; r++) rows.push(row);
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
