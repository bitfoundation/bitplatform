// Assembles the Bit.Butil JavaScript outputs from the per-file JavaScript that tsc emits.
//
// One TypeScript file under Scripts/ is one "module" (clipboard.ts -> the BitButil.clipboard namespace),
// and this script turns those into three things:
//
//   1. wwwroot/bit-butil.js            The classic single bundle: every module, once, in dependency order.
//   2. wwwroot/modules/<name>.js       One self-contained file per module for lazy loading: the module
//                                      plus everything it depends on, so a consumer can `import()` just
//                                      the one file and call into it.
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
// `butil.<name>` / `BitButil.<name>` reference to another module's namespace is a dependency. Only
// call-time references exist today (a module never touches another during its own initialization), so
// order inside a file only matters for readability, but the manifest keeps dependency-first order anyway.
//
// Usage: node build.mjs [--minify] [--intermediate <dir>]
//   --intermediate   the project's intermediate folder (MSBuild's BaseIntermediateOutputPath); default obj/

import { readFileSync, readdirSync, writeFileSync, mkdirSync, rmSync, existsSync } from 'node:fs';
import { join, basename, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import * as esbuild from 'esbuild';

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
        code = esbuild.transformSync(code, { minify: true, target: 'es2019', legalComments: 'none' }).code;
    }
    chunks.set(name, code);
}

const concat = moduleNames => moduleNames.map(name => chunks.get(name)).join('');

// --- Write everything ----------------------------------------------------------------------------

rmSync(modulesOutDir, { recursive: true, force: true });
rmSync(packOutDir, { recursive: true, force: true });
mkdirSync(modulesOutDir, { recursive: true });
mkdirSync(chunksOutDir, { recursive: true });

const everything = ordered(sources);
writeFileSync(join(wwwroot, 'bit-butil.js'), concat(everything));

for (const name of sources) {
    writeFileSync(join(modulesOutDir, `${name}.js`), concat(ordered([name])));
    writeFileSync(join(chunksOutDir, `${name}.js`), chunks.get(name));
}

// One line per module, dependency-first order, `name=dep1,dep2`. Consumed by the publish-time bundler
// (Bit.Butil.Build) and checked by the test projects; keep the format that simple.
writeFileSync(join(chunksOutDir, 'manifest.txt'),
    everything.map(name => `${name}=${dependencies.get(name).join(',')}`).join('\n') + '\n');

console.log(`bit-butil: ${sources.length} modules -> bundle, ${sources.length} lazy modules, ${sources.length} chunks${minify ? ' (minified)' : ''}`);

function fail(message) {
    console.error(`bit-butil build: ${message}`);
    process.exit(1);
}
