// Measures what a consuming app actually downloads, per module.
//
// The line budget build.mjs enforces is a proxy: it keeps a source file from growing without anyone
// noticing. This is the number itself - the minified, compressed bytes of a module together with the
// dependencies its lazy-loaded file inlines, which is what the browser fetches when an app under
// lazy scripts first calls into that module.
//
// Measured off the shipped shapes rather than re-derived from the manifest: wwwroot/modules/<name>.js
// *is* a module's closure (the Manual harness checks it byte-for-byte against what the publish-time
// bundler assembles), obj/butil-js/chunks/<name>.js is the module alone, and wwwroot/bit-butil.js is
// the classic bundle. Minified with the very options build.mjs uses - imported from the project, not
// restated here - so the figures and the artifacts agree by construction whichever way either changes.
// Compressed both ways because a static host serves whichever the client accepts, and brotli - what
// every current browser asks for over HTTPS - is the one the budgets are written against.
//
// Usage: node weigh-modules.mjs <path to Bit.Butil project folder>
// Output: CSV on stdout - module,ownMin,closureMin,gzip,brotli,depCount - then a TOTAL line.

import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join, basename } from 'node:path';
import { pathToFileURL } from 'node:url';
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

const { MINIFY_OPTIONS } = await import(pathToFileURL(join(projectDir, 'minify-options.mjs')).href);

const chunksDir = join(projectDir, 'obj', 'butil-js', 'chunks');
const modulesDir = join(projectDir, 'wwwroot', 'modules');
const bundlePath = join(projectDir, 'wwwroot', 'bit-butil.js');
if (!existsSync(chunksDir) || !existsSync(modulesDir) || !existsSync(bundlePath)) {
    fail(`${projectDir} has no build outputs - build Bit.Butil first so build.mjs writes the chunks, the lazy module files and the bundle.`);
}

// Idempotent on an already-minified Release output, so the figure is the same whichever
// configuration built the tree last.
const minified = path => esbuild.transformSync(readFileSync(path, 'utf8'), MINIFY_OPTIONS).code;

// Every chunk opens with a guard naming the namespace it registers, so counting the guards in a
// lazy file is counting the modules it carries - the dependency count without walking anything.
// Matched on the `BitButil&&window.BitButil.<key>)` pair the guard tests, which survives minification
// (esbuild folds the early return into `if(!(...)){`) as well as the unminified `if(...)return;`.
const guards = code => (code.match(/BitButil&&window\.BitButil\.[A-Za-z0-9_$]+\)/g) ?? []).length;

const names = readdirSync(chunksDir).filter(file => file.endsWith('.js')).map(file => basename(file, '.js')).sort();

const rows = names.map(name => {
    const own = minified(join(chunksDir, `${name}.js`));
    const closure = minified(join(modulesDir, `${name}.js`));
    return {
        name,
        own: Buffer.byteLength(own),
        closure: Buffer.byteLength(closure),
        gzip: gzipSync(closure, { level: 9 }).length,
        brotli: brotliCompressSync(Buffer.from(closure)).length,
        deps: Math.max(guards(closure) - 1, 0),
    };
});

rows.sort((a, b) => b.brotli - a.brotli);

console.log('module,ownMin,closureMin,gzip,brotli,depCount');
for (const row of rows) console.log(`${row.name},${row.own},${row.closure},${row.gzip},${row.brotli},${row.deps}`);

// The classic single bundle: what an app that has not opted into lazy scripts downloads once.
const everything = minified(bundlePath);
console.log(`TOTAL,${rows.length},${Buffer.byteLength(everything)},${gzipSync(everything, { level: 9 }).length},${brotliCompressSync(Buffer.from(everything)).length},0`);

function fail(message) {
    console.error(`weigh-modules: ${message}`);
    process.exit(1);
}
