// Verifies that every "BitButil.x.y" identifier the C# side invokes actually exists on the
// compiled JavaScript bundle.
//
// Nothing else catches this class of mistake: a renamed or misspelled JS function compiles fine on
// both sides and only fails at runtime, in the browser, on the one code path that calls it. Loading
// the real bundle and walking every call site is cheap enough to run on every CI build.
//
// Usage: node verify-interop-contract.mjs <bundle.js> <csharp-source-root>

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, extname } from 'node:path';
import { createContext, runInContext } from 'node:vm';

const [, , bundlePath, sourceRoot] = process.argv;

if (!bundlePath || !sourceRoot) {
    console.error('Usage: node verify-interop-contract.mjs <bundle.js> <csharp-source-root>');
    process.exit(2);
}

// --- Load the bundle -------------------------------------------------------------------------

// The bundle is written for a browser, so it needs a window to attach itself to and enough of the
// DOM for its module-level code (a handful of modules register a 'pagehide' listener at load).
// Nothing here has to *work* - the modules are only being registered, not called.
const noop = () => { };
const sandbox = {};
sandbox.window = sandbox;
sandbox.globalThis = sandbox;
sandbox.self = sandbox;
sandbox.addEventListener = noop;
sandbox.removeEventListener = noop;
sandbox.navigator = { mediaDevices: {}, locks: {} };
sandbox.location = {};
sandbox.matchMedia = () => ({ matches: false, media: '', addEventListener: noop });
sandbox.document = { addEventListener: noop, removeEventListener: noop, createElement: () => ({}), documentElement: {} };
sandbox.performance = { now: () => 0 };
sandbox.crypto = { randomUUID: () => '', subtle: {} };
sandbox.URL = { createObjectURL: () => '', revokeObjectURL: noop };
sandbox.Blob = function () { };
sandbox.Response = function () { };
sandbox.console = console;
sandbox.localStorage = {};
sandbox.sessionStorage = {};

createContext(sandbox);

try {
    runInContext(readFileSync(bundlePath, 'utf8'), sandbox);
} catch (error) {
    console.error(`The bundle at ${bundlePath} failed to evaluate: ${error.message}`);
    process.exit(1);
}

const butil = sandbox.BitButil;
if (!butil) {
    console.error(`The bundle at ${bundlePath} evaluated without defining BitButil.`);
    process.exit(1);
}

// --- Collect the call sites ------------------------------------------------------------------

function* csharpFiles(dir) {
    for (const entry of readdirSync(dir)) {
        // obj/bin hold generated copies of the same sources; walking them doubles the work and
        // reports every finding twice.
        if (entry === 'obj' || entry === 'bin' || entry === 'node_modules') continue;
        const path = join(dir, entry);
        if (statSync(path).isDirectory()) yield* csharpFiles(path);
        else if (extname(path) === '.cs') yield path;
    }
}

// A call site is a string literal, so an identifier inside an XML doc comment (<see cref="..."/>)
// would otherwise be picked up as one. Dropping comment lines is enough to tell them apart.
const identifier = /"(BitButil\.[A-Za-z0-9_.]+)"/g;
const callSites = new Map();

for (const file of csharpFiles(sourceRoot)) {
    const lines = readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, index) => {
        if (line.trimStart().startsWith('//') || line.trimStart().startsWith('///')) return;
        for (const match of line.matchAll(identifier)) {
            if (!callSites.has(match[1])) callSites.set(match[1], `${file}:${index + 1}`);
        }
    });
}

// --- Check ------------------------------------------------------------------------------------

const missing = [];

for (const [call, where] of callSites) {
    const parts = call.split('.').slice(1); // drop the leading "BitButil"
    let current = butil;
    let resolved = true;

    for (const part of parts) {
        if (current == null || !(part in current)) { resolved = false; break; }
        current = current[part];
    }

    if (resolved === false || current === undefined) missing.push({ call, where });
}

if (missing.length > 0) {
    console.error(`${missing.length} of ${callSites.size} interop call sites do not resolve against the bundle:`);
    for (const { call, where } of missing) console.error(`  ${call}  (${where})`);
    process.exit(1);
}

console.log(`OK - all ${callSites.size} interop call sites resolve against ${bundlePath}.`);
