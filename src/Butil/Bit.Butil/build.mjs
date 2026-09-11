// Assembles the Bit.Butil JavaScript outputs from the per-file JavaScript that tsc emits.
//
// One TypeScript file under Scripts/ is one "module" (clipboard.ts -> the BitButil.clipboard namespace),
// and this script turns those into three things:
//
//   1. wwwroot/bit-butil.js            The classic single bundle: every module, once, in dependency order.
//   2. wwwroot/modules/<name>.js       One self-contained file per module for lazy loading: the module
//                                      plus everything it depends on, so a consumer can `import()` just
//                                      the one file and call into it. Self-contained on purpose: a lazy
//                                      app pays one request per module it touches and never one per
//                                      dependency (the E2E suite asserts exactly that), at the price
//                                      that two siblings of a split family each carry the family's
//                                      base module again. Bytes were chosen over round trips there.
//   3. obj/butil-js/chunks/<name>.js   The raw building blocks (one module each, no dependencies) plus
//      obj/butil-js/chunks/manifest.txt the dependency manifest. These ship inside the NuGet package so a
//                                      consumer's publish can assemble a bundle holding only the modules
//                                      its trimmed app still calls (see buildTransitive/Bit.Butil.targets).
//
// Every chunk is wrapped so that it is safe to evaluate more than once and safe to evaluate as an ES
// module: `if (window.BitButil.<key>) return;` makes a module that is already registered a no-op (the
// self-contained files above overlap - element.js and window.js both carry utils - and re-running a
// module would reset its private state such as listener registries), and the sources attach to
// `window.BitButil` explicitly rather than relying on a top-level `var` becoming a global, which does not
// happen inside a module. That is what lets one artifact serve both a `<script>` tag and `import()`.
//
// Dependencies are discovered from the TypeScript sources rather than declared by hand: any
// `butil.<name>` / `BitButil.<name>` reference to another module's namespace is a dependency. The order
// inside a file is load-bearing: a module may register a hook with the module it depends on while it
// initializes (webAudioNodes -> webAudio.onDispose, webAudioMedia -> webAudioNodes.onRelease,
// performanceVitals -> performance.onStopRetained), which throws unless the dependency has already run.
// Every file written here is dependency-first, and the publish-time bundler concatenates in the
// manifest's order for the same reason - anything assembling chunks by hand has to keep that order too.
//
// Usage: node build.mjs [--minify] [--intermediate <dir>]
//   --intermediate   the project's intermediate folder (MSBuild's BaseIntermediateOutputPath); default obj/

import { readFileSync, readdirSync, writeFileSync, renameSync, mkdirSync, rmSync, existsSync } from 'node:fs';
import { join, basename, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import * as esbuild from 'esbuild';
import { MINIFY_OPTIONS } from './minify-options.mjs';

const root = dirname(fileURLToPath(import.meta.url));
const scriptsDir = join(root, 'Scripts');
const wwwroot = join(root, 'wwwroot');
const modulesOutDir = join(wwwroot, 'modules');
const minify = process.argv.includes('--minify');
const intermediateArg = process.argv.indexOf('--intermediate');
const intermediateDir = intermediateArg >= 0 ? process.argv[intermediateArg + 1] : join(root, 'obj');
const compiledDir = join(intermediateDir, 'scripts');       // tsc outDir (tsconfig.json; overridden by the csproj)
const packOutDir = join(intermediateDir, 'butil-js');
const chunksOutDir = join(packOutDir, 'chunks');

// The version/namespace prelude. Every module depends on it so `BitButil.version` is always there.
const PRELUDE = 'butil';

// --- Discover the modules and their dependencies from the TypeScript sources -------------------

const sources = readdirSync(scriptsDir)
    .filter(file => file.endsWith('.ts') && !file.endsWith('.d.ts'))
    .map(file => basename(file, '.ts'))
    .sort();

const names = new Set(sources);
const keys = new Map();          // module name -> the BitButil.<key> it registers (guards the chunk)
const dependencies = new Map();  // module name -> sorted array of module names it references

for (const name of sources) {
    const source = readFileSync(join(scriptsDir, `${name}.ts`), 'utf8');

    // What the module registers on BitButil - `butil.clipboard = {` - is what the guard checks for.
    const registered = [...source.matchAll(/\b(?:butil|BitButil)\.([A-Za-z0-9_]+)\s*=(?!=)/g)].map(match => match[1]);
    const key = name === PRELUDE ? 'version' : registered.find(candidate => candidate === name);
    if (!key) {
        fail(`${name}.ts must register its namespace as \`butil.${name} = {...}\` (found: ${registered.join(', ') || 'nothing'}). ` +
            'The C# side derives the module file from the identifier it invokes, so the file name and the namespace have to agree.');
    }
    keys.set(name, key);

    const referenced = new Set();
    for (const match of source.matchAll(/\b(?:butil|BitButil)\.([A-Za-z0-9_]+)\b/g)) {
        const target = match[1];
        if (target !== name && names.has(target)) referenced.add(target);
    }
    if (name !== PRELUDE) referenced.add(PRELUDE);
    dependencies.set(name, [...referenced].sort());
}

// --- Module size budget --------------------------------------------------------------------------

// A module is the unit a trimmed app downloads: the publish-time bundler keeps a module whole or
// keeps none of it, so every feature parked in one is paid for by every app that calls any other
// feature in it. That makes size here a user-facing number, not a style preference - hence a budget
// the build enforces rather than a convention someone has to remember.
//
// Over WARN, split the module along its feature seams (see the crypto*, webAudio* and element*
// families for the shape: one module per coherent group, shared state in a small module of its own
// that the others depend on). Over FAIL, the build stops.
const SIZE_WARN_LINES = 250;
const SIZE_FAIL_LINES = 400;

// The one module exempted from the budget, and why: its length is pattern tables, not features. One
// call reads all of them - browser, engine, system, device - so there is nothing to split off that
// a caller would not immediately download again. It is already the far side of a split (userAgent
// holds the Client Hints members, which is what most callers want) and exists precisely so that
// its weight is only downloaded by an app that asks for UserAgent.Extract(). Weight is what the
// budget is really about, and by that measure it now sits just outside the ten heaviest rather
// than first.
const SIZE_EXEMPT = new Set(['userAgentParser']);

const oversized = [];
for (const name of sources) {
    // Counted the way an editor numbers them: a file ending in a newline (which .editorconfig asks
    // for) has no extra empty line after it, so a split on line breaks over-counts by one there.
    const lines = readFileSync(join(scriptsDir, `${name}.ts`), 'utf8').replace(/\r?\n$/, '').split(/\r?\n/).length;
    if (SIZE_EXEMPT.has(name)) continue;
    if (lines > SIZE_FAIL_LINES) oversized.push(`${name}.ts (${lines} lines)`);
    else if (lines > SIZE_WARN_LINES) {
        console.warn(`bit-butil build: ${name}.ts is ${lines} lines (budget ${SIZE_WARN_LINES}); consider splitting it - ` +
            'every app calling any part of this module downloads all of it.');
    }
}
if (oversized.length > 0) {
    fail(`these modules are over the ${SIZE_FAIL_LINES}-line budget and have to be split: ${oversized.join(', ')}. ` +
        'A module is downloaded whole or not at all, so an app calling one of its functions pays for every other one.');
}

// Dependency-first order for a set of modules, deterministic (alphabetical among peers).
function ordered(roots) {
    const result = [];
    const seen = new Set();
    const visiting = new Set();
    const visit = name => {
        if (seen.has(name)) return;
        if (visiting.has(name)) fail(`circular dependency through ${name}`);
        visiting.add(name);
        for (const dependency of dependencies.get(name)) visit(dependency);
        visiting.delete(name);
        seen.add(name);
        result.push(name);
    };
    for (const name of [...roots].sort()) visit(name);
    return result;
}

// --- Build the chunks ----------------------------------------------------------------------------

const chunks = new Map();
for (const name of sources) {
    const compiled = join(compiledDir, `${name}.js`);
    if (!existsSync(compiled)) fail(`${compiled} is missing - run tsc first.`);

    // See the header comment for why every chunk is wrapped and guarded.
    let code = `(function(){if(window.BitButil&&window.BitButil.${keys.get(name)})return;\n${readFileSync(compiled, 'utf8').trimEnd()}\n})();\n`;
    if (minify) {
        code = esbuild.transformSync(code, MINIFY_OPTIONS).code;
    }
    chunks.set(name, code);
}

const concat = moduleNames => moduleNames.map(name => chunks.get(name)).join('');

// --- Write everything ----------------------------------------------------------------------------

rmSync(packOutDir, { recursive: true, force: true });
mkdirSync(modulesOutDir, { recursive: true });
mkdirSync(chunksOutDir, { recursive: true });

// Every output is written to a temporary file and renamed into place, because a plain write truncates its
// target first: an interrupted run would leave a half-written file newer than its inputs, which the MSBuild
// Inputs/Outputs check in Bit.Butil.csproj would then take for an up-to-date build. A rename is atomic
// within a volume, so an output holds either the previous run's content or this one's, never neither.
//
// The temporary lives under obj/ instead of beside its target, and wwwroot/modules is pruned after the run
// instead of emptied before it, because `dotnet watch` dies the moment a file *appears* under a watched
// project's wwwroot (dotnet/roslyn#84062): a .tmp next to the bundle, or every module recreated in a
// just-emptied directory, took the demo watcher down on every rebuild that reached this script. Renaming
// over a file that is already there is an update, which the watcher survives. obj/ and wwwroot both sit
// under the project directory, so the rename stays within one volume and stays atomic.
const scratch = join(packOutDir, 'write.tmp');

function write(path, contents) {
    writeFileSync(scratch, contents);
    renameSync(scratch, path);
}

const everything = ordered(sources);
write(join(wwwroot, 'bit-butil.js'), concat(everything));

for (const name of sources) {
    // The closure, but laid out in the bundle's own order rather than in the order a walk from this
    // module happens to reach it. Both orders are dependency-first, so either would run - but the
    // publish-time bundler assembles a module's closure in manifest order, and these two files are
    // compared byte-for-byte (the Manual harness checks exactly that). Ordering the closure the same
    // way keeps them equal by construction instead of by coincidence: a module whose dependency is
    // alphabetically before a dependency of its own (trustedTypes -> sanitizer, ahead of utils) comes
    // out in a different order from a per-module walk.
    const closure = new Set(ordered([name]));
    write(join(modulesOutDir, `${name}.js`), concat(everything.filter(module => closure.has(module))));
    write(join(chunksOutDir, `${name}.js`), chunks.get(name));
}

// One line per module, dependency-first order, `name=dep1,dep2`. Consumed by the publish-time bundler
// (Bit.Butil.Build) and checked by the test projects; keep the format that simple.
write(join(chunksOutDir, 'manifest.txt'),
    everything.map(name => `${name}=${dependencies.get(name).join(',')}`).join('\n') + '\n');

// Whatever a previous run left behind that this one did not produce: a module whose Scripts/*.ts was
// renamed or deleted, or a .tmp from before these temporaries moved out of wwwroot. Pruning the stale
// files afterwards is what lets every surviving module be written as an update rather than an addition.
const expected = new Set(sources.map(name => `${name}.js`));
for (const file of readdirSync(modulesOutDir)) {
    if (!expected.has(file)) rmSync(join(modulesOutDir, file));
}
for (const file of readdirSync(wwwroot)) {
    if (file.endsWith('.tmp')) rmSync(join(wwwroot, file));
}

console.log(`bit-butil: ${sources.length} modules -> bundle, ${sources.length} lazy modules, ${sources.length} chunks${minify ? ' (minified)' : ''}`);

function fail(message) {
    console.error(`bit-butil build: ${message}`);
    process.exit(1);
}
