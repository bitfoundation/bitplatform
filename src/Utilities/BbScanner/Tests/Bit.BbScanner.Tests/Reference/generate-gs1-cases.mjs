// Writes Gs1Cases.json: element strings, one AI each, with what BWIPP's GS1 syntax checker says about them.
//
// The GS1 element string parser (Bit.BbScanner/Gs1) carries its own table of Application Identifiers and its own
// checks. Tests written next to it can only repeat its author's reading of the GS1 General Specifications; BWIPP
// (Barcode Writer in Pure PostScript, MIT licensed) checks every element string it encodes against the GS1 Barcode
// Syntax Dictionary, so it is an oracle written by someone else. For every AI in the library's table this script
// builds a value that should pass and a handful of mutations that may not - one character short or long, a character
// outside the character set, a wrong check digit, an impossible date - and records BWIPP's verdict and, for a
// rejection, its error code. Gs1ReferenceTests replays them.
//
// The AIs are read from BbGs1Dictionary.cs itself, so a new AI there gets cases the next time this runs. Like the
// symbol fixtures, the output is checked in and bwip-js is not a dependency:
//
//     npm install --prefix <some scratch folder> bwip-js
//     node generate-gs1-cases.mjs <scratch folder>/node_modules/bwip-js/dist/bwip-js-node.js > Gs1Cases.json

import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const bwipjs = require(resolve(process.argv[2]));
const here = dirname(fileURLToPath(import.meta.url));
const dictionary = readFileSync(resolve(here, '../../../Bit.BbScanner/Gs1/BbGs1Dictionary.cs'), 'utf8');

// A small deterministic generator, so that regenerating with the same bwip-js writes the same file.
let seed = 20261111;
const random = n => {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff;
    return seed % n;
};
const pick = text => text[random(text.length)];

const DIGITS = '0123456789';
const CSET82 = '!"%&\'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz';
const CSET39 = '#-/0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';
const CSET64 = '-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz';
const CSET32 = '23456789ABCDEFGHJKLMNPQRSTUVWXYZ';
const PRIMES = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83];

/** Every AI of the table with its format string, the ranges and loops of BbGs1Dictionary.Build expanded. */
function readTable() {
    const table = [];
    for (const [, ai, , format] of dictionary.matchAll(/\bAdd\("(\d+)", "([^"]*)", "([^"]*)"/g)) {
        table.push({ ai, format });
    }

    for (const [, first, last, , format] of dictionary.matchAll(/AddRange\("(\d+)", "(\d+)", "([^"]*)", "([^"]*)"/g)) {
        for (let digit = Number(first.at(-1)); digit <= Number(last.at(-1)); digit++) {
            table.push({ ai: first.slice(0, -1) + digit, format });
        }
    }

    // The measures: a three digit prefix and its title, run from 0 to 5 decimal places.
    for (const [, prefix] of dictionary.matchAll(/\("(3\d\d)", "[^"]*"\)/g)) {
        for (let digit = 0; digit <= 5; digit++) {
            table.push({ ai: prefix + digit, format: 'N6' });
        }
    }

    for (let ai = 91; ai <= 99; ai++) {
        table.push({ ai: String(ai), format: 'X..90' });
    }

    return table;
}

function parseFormat(format) {
    return format.split(' ').map(part => {
        const optional = part.startsWith('[');
        if (optional) part = part.slice(1, -1);
        const [shape, ...checks] = part.split(',');
        const lengths = shape.slice(1);
        const range = lengths.indexOf('..');
        const min = range < 0 ? Number(lengths) : (range === 0 ? 1 : Number(lengths.slice(0, range)));
        const max = range < 0 ? Number(lengths) : Number(lengths.slice(range + 2));
        return { charset: shape[0], min, max, optional, checks };
    });
}

const two = n => String(n).padStart(2, '0');
const date = (withZeroDay) => two(20 + random(10)) + two(1 + random(12)) + (withZeroDay && random(4) === 0 ? '00' : two(1 + random(28)));

function checkDigit(digits) {
    let sum = 0;
    for (let i = 0; i < digits.length; i++) sum += Number(digits[i]) * ((digits.length - i) % 2 === 1 ? 3 : 1);
    return String((10 - (sum % 10)) % 10);
}

function checkPair(data) {
    let sum = 0;
    for (let i = 0; i < data.length; i++) sum += CSET82.indexOf(data[i]) * PRIMES[data.length - 1 - i];
    sum %= 1021;
    return CSET32[sum >> 5] + CSET32[sum & 31];
}

function iban() {
    const bban = 'WEST' + Array.from({ length: 14 }, () => pick(DIGITS)).join('');
    const digits = [...(bban + 'GB00')].map(c => (c >= 'A' ? String(c.charCodeAt(0) - 55) : c)).join('');
    let remainder = 0;
    for (const c of digits) remainder = (remainder * 10 + Number(c)) % 97;
    return 'GB' + two(98 - remainder) + bban;
}

/** A value meant to be valid for one component, or null when the component has a check this script cannot meet. */
function component(c) {
    const checks = c.checks;
    const length = c.min === c.max ? c.min : Math.max(c.min, Math.min(c.max, c.min + 1 + random(Math.min(8, c.max - c.min))));
    const has = name => checks.includes(name);

    if (has('yymmd0')) return date(true);
    if (has('yymmdd')) return date(false);
    if (has('yyyymmdd')) return '19' + date(false);
    if (has('yymmddhh')) return date(false) + two(random(24));
    if (has('yymmddhhmi')) return date(false) + two(random(24)) + two(random(60));
    if (has('yyyymmddhhmi')) return '19' + date(false) + two(random(24)) + two(random(60));
    if (has('hhmi')) return two(random(24)) + two(random(60));
    if (has('mmoptss')) return two(random(60)) + two(random(60));
    if (has('yesno')) return pick('01');
    if (has('iso5218')) return pick('0129');
    if (has('zero')) return '0';
    if (has('hyphen')) return '-';
    if (has('slash')) return '/';
    if (has('winding')) return pick('019');
    if (has('iso3166') || has('iso3166list')) return '276'.repeat(Math.max(1, Math.floor(length / 3)));
    if (has('iso4217')) return '978';
    if (has('iso3166alpha2')) return 'DE';
    if (has('pieceoftotal')) return '0102';
    if (has('latitude')) return '0' + Array.from({ length: 9 }, () => pick(DIGITS)).join('');
    if (has('longitude')) return '1' + Array.from({ length: 9 }, () => pick(DIGITS)).join('');
    if (has('iban')) return iban();

    const charset = { N: DIGITS, X: CSET82.replace(/[()%]/g, ''), Y: CSET39, Z: CSET64 }[c.charset];
    let value = Array.from({ length }, () => pick(charset)).join('');
    if (has('nonzero') || has('nozeroprefix')) value = '1' + value.slice(1);
    if (has('gcppos1')) value = '12' + value.slice(2);
    if (has('csum')) value = value.slice(0, -1) + checkDigit(value.slice(0, -1));
    if (has('csumalpha')) value = value.slice(0, -2) + checkPair(value.slice(0, -2));
    return value;
}

/** One character changed at <at>: a digit for a digit, a letter otherwise. */
function change(value, at) {
    const c = value[at];
    const replacement = /\d/.test(c) ? String((Number(c) + 1) % 10) : (c === 'A' ? 'B' : 'A');
    return value.slice(0, at) + replacement + value.slice(at + 1);
}

function verdict(ai, value) {
    try {
        bwipjs.toSVG({ bcid: 'gs1-128', text: `(${ai})${value}` });
        return { valid: true };
    } catch (error) {
        const [code] = error.message.split(':');
        return { valid: false, error: code.replace(/^bwipp\./, '').replace(/#\d+$/, '') };
    }
}

const cases = [];
for (const { ai, format } of readTable()) {
    const components = parseFormat(format);
    const parts = components.map(component);
    const valid = parts.join('');
    const candidates = new Set([valid]);

    // Optional components left off; one character short and one too many; a character outside every set; the last
    // character changed, which breaks a check digit or pair; the first changed, which breaks a date or a prefix.
    const required = components.filter(c => !c.optional).length;
    if (required < components.length) candidates.add(parts.slice(0, required).join(''));
    if (valid.length > 1) candidates.add(valid.slice(0, -1));
    candidates.add(valid + (components.at(-1).charset === 'N' ? '1' : 'A'));
    candidates.add(valid.slice(0, -1) + '#');
    candidates.add(valid.slice(0, -1) + ' ');
    candidates.add(change(valid, valid.length - 1));
    candidates.add(change(valid, 0));
    if (valid.length > 3) candidates.add(change(valid, 2));
    if (components[0].checks.some(check => /yy|hh|mm/.test(check))) {
        candidates.add('99' + '13' + valid.slice(4));
        candidates.add(valid.slice(0, 2) + '0230' + valid.slice(6));
    }

    if (format.includes('pcenc')) {
        candidates.add('AB%20CD');
        candidates.add('AB%2');
        candidates.add('AB%GG');
    }

    for (const value of candidates) {
        if (value.length === 0 || value.includes('(')) continue;
        cases.push({ ai, value, ...verdict(ai, value) });
    }
}

const { BWIPP_VERSION, BWIPJS_VERSION } = bwipjs;
process.stdout.write(JSON.stringify({
    generator: `BWIPP ${BWIPP_VERSION} via bwip-js ${BWIPJS_VERSION}`,
    cases,
}, null, 1) + '\n');
