// Verifies that every "BitButil.x.y" identifier the C# side invokes actually exists on the
// compiled JavaScript bundle - and, when the folder of lazy-loadable modules is given, that each call
// site also resolves against just its own module file loaded on its own, which is exactly what a
// lazy-scripts consumer does (see build.mjs and BitButil.UseLazyScripts).
//
// Nothing else catches this class of mistake: a renamed or misspelled JS function compiles fine on
// both sides and only fails at runtime, in the browser, on the one code path that calls it. Loading
// the real bundle and walking every call site is cheap enough to run on every CI build.
//
// Usage: node verify-interop-contract.mjs <bundle.js> <csharp-source-root> [<modules-folder>]

import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, extname } from 'node:path';
import { createContext, runInContext } from 'node:vm';

const [, , bundlePath, sourceRoot, modulesDir] = process.argv;

if (!bundlePath || !sourceRoot) {
    console.error('Usage: node verify-interop-contract.mjs <bundle.js> <csharp-source-root>');
    process.exit(2);
}

// --- Load the bundle -------------------------------------------------------------------------

// The scripts are written for a browser, so they need a window to attach themselves to and enough of
// the DOM for their module-level code (a handful of modules register a 'pagehide' listener at load).
// Nothing here has to *work* - the modules are only being registered, not called. A fresh sandbox per
// evaluation, so a lazy module cannot lean on something an earlier one left on the window.
function createSandbox() {
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
    return sandbox;
}

function evaluate(scriptPath) {
    const sandbox = createSandbox();
    try {
        runInContext(readFileSync(scriptPath, 'utf8'), sandbox);
    } catch (error) {
        console.error(`${scriptPath} failed to evaluate: ${error.message}`);
        process.exit(1);
    }
    if (!sandbox.BitButil) {
        console.error(`${scriptPath} evaluated without defining BitButil.`);
        process.exit(1);
    }
    return sandbox.BitButil;
}

const butil = evaluate(bundlePath);

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

function resolves(root, call) {
    const parts = call.split('.').slice(1); // drop the leading "BitButil"
    let current = root;
    for (const part of parts) {
        if (current == null || !(part in current)) return false;
        current = current[part];
    }
    return current !== undefined;
}

const missing = [];
for (const [call, where] of callSites) {
    if (!resolves(butil, call)) missing.push({ call, where });
}

if (missing.length > 0) {
    console.error(`${missing.length} of ${callSites.size} interop call sites do not resolve against the bundle:`);
    for (const { call, where } of missing) console.error(`  ${call}  (${where})`);
    process.exit(1);
}

console.log(`OK - all ${callSites.size} interop call sites resolve against ${bundlePath}.`);

// --- Lazy modules ------------------------------------------------------------------------------
// In lazy mode the C# side import()s <modules>/<second segment of the identifier>.js and nothing else, so
// every call site must resolve with only that one file evaluated - which also proves the file is really
// self-contained (carries the utils/events/... it depends on).

if (modulesDir) {
    const modules = new Map(); // module name -> BitButil as defined by that file alone
    const missingLazy = [];
    for (const [call, where] of callSites) {
        const module = call.split('.')[1];
        if (!modules.has(module)) {
            const file = join(modulesDir, `${module}.js`);
            if (!existsSync(file)) { missingLazy.push({ call, where, reason: `no such module file: ${file}` }); continue; }
            modules.set(module, evaluate(file));
        }
        if (!resolves(modules.get(module), call)) missingLazy.push({ call, where, reason: `not defined by ${module}.js on its own` });
    }

    if (missingLazy.length > 0) {
        console.error(`${missingLazy.length} of ${callSites.size} interop call sites do not resolve in lazy mode:`);
        for (const { call, where, reason } of missingLazy) console.error(`  ${call}  (${where}): ${reason}`);
        process.exit(1);
    }

    console.log(`OK - all ${callSites.size} interop call sites resolve against their own lazy module (${modules.size} module files under ${modulesDir}).`);
}
