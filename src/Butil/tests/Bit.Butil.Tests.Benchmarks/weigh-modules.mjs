// Measures what a consuming app actually downloads, per module.
//
// The line budget build.mjs enforces is a proxy: it keeps a source file from growing without anyone
// noticing. This is the number itself - the minified, compressed bytes of a module together with the
// dependencies its lazy-loaded file inlines, which is what the browser fetches when an app under
// lazy scripts first calls into that module.
//
// Minified with the same esbuild settings build.mjs uses for a Release build, so the figures here and
// the shipped artifacts agree by construction. Compressed both ways because a static host serves
// whichever the client accepts, and brotli - what every current browser asks for over HTTPS - is the
// one the budgets are written against.
//
// Reads the chunks (one module each, no dependencies) and the manifest that build.mjs writes into the
// project's intermediate folder, because those are the same inputs the publish-time bundler assembles
// a trimmed app's bundle from.
//
// Usage: node weigh-modules.mjs <path to Bit.Butil project folder>
// Output: CSV on stdout - module,ownMin,closureMin,gzip,brotli,depCount - then a TOTAL line.

import { readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { gzipSync, brotliCompressSync } from 'node:zlib';
import { createRequire } from 'node:module';

const projectDir = process.argv[2];
if (!projectDir) fail('usage: node weigh-modules.mjs <path to Bit.Butil project folder>');

// esbuild is a devDependency of the Bit.Butil project, not of this one: resolving it from there keeps
// a single installed copy and guarantees it is the very version build.mjs minified the shipped
// artifacts with.
const require = createRequire(join(projectDir, 'build.mjs'));
let esbuild;
try {
    esbuild = require('esbuild');
} catch {
    fail(`esbuild is not installed under ${projectDir}. Build Bit.Butil once (or run npm install there) first.`);
}

const chunksDir = join(projectDir, 'obj', 'butil-js', 'chunks');
const manifestPath = join(chunksDir, 'manifest.txt');
if (!existsSync(manifestPath)) {
    fail(`${manifestPath} is missing - build Bit.Butil first so build.mjs writes the chunks.`);
}

// `name=dep1,dep2`, already in dependency-first order.
const dependencies = new Map();
const order = [];
for (const line of readFileSync(manifestPath, 'utf8').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    const [name, rest] = trimmed.split('=');
    dependencies.set(name, rest ? rest.split(',').filter(Boolean) : []);
    order.push(name);
}

const minified = new Map();
for (const name of order) {
    const raw = readFileSync(join(chunksDir, `${name}.js`), 'utf8');
    minified.set(name, esbuild.transformSync(raw, { minify: true, target: 'es2019', legalComments: 'none' }).code);
}

// The closure laid out in the bundle's own order, which is the order the lazy module file and the
// publish-time bundler both use - so these bytes are the bytes that ship.
function closureOf(name) {
    const reached = new Set();
    const visit = current => {
        if (reached.has(current)) return;
        reached.add(current);
        for (const dependency of dependencies.get(current) ?? []) visit(dependency);
    };
    visit(name);
    return order.filter(module => reached.has(module));
}

const rows = order.map(name => {
    const closure = closureOf(name);
    const code = closure.map(module => minified.get(module)).join('');
    return {
        name,
        own: Buffer.byteLength(minified.get(name)),
        closure: Buffer.byteLength(code),
        gzip: gzipSync(code, { level: 9 }).length,
        brotli: brotliCompressSync(Buffer.from(code)).length,
        deps: closure.length - 1,
    };
});

rows.sort((a, b) => b.brotli - a.brotli);

console.log('module,ownMin,closureMin,gzip,brotli,depCount');
for (const row of rows) console.log(`${row.name},${row.own},${row.closure},${row.gzip},${row.brotli},${row.deps}`);

// The classic single bundle: what an app that has not opted into lazy scripts downloads once.
const everything = order.map(name => minified.get(name)).join('');
console.log(`TOTAL,${rows.length},${Buffer.byteLength(everything)},${gzipSync(everything, { level: 9 }).length},${brotliCompressSync(Buffer.from(everything)).length},0`);

function fail(message) {
    console.error(`weigh-modules: ${message}`);
    process.exit(1);
}
