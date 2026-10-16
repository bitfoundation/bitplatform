// Builds the JavaScript and CSS of one bit BlazorUI package, in the two shapes the package ships them in:
//
//   * the bundles under wwwroot (bit.blazorui.js, bit.blazorui.css, ...) an app references with a <script> or a
//     <link>, holding everything - exactly what a build and every development run serve;
//   * the pieces those bundles are made of - one chunk per TypeScript file and one per component stylesheet - and a
//     manifest of what each piece is, which the package carries for the publish-time trimming
//     (Bit.BlazorUI.Build's TrimBitBlazorUIAssets) to put a bundle back together from only the pieces an app can use.
//
// A bundle is written FROM its pieces, never next to them, so a trimmed bundle is a subset of the very text the full
// one is: the scripts are concatenated in the load order below, and every stylesheet is checked to compile to exactly
// its own prefix, chunks and suffix before anything is written.
//
//   node build-assets.mjs scripts --package <id> --tsconfig tsconfig.json --bundle scripts/x.js --out <dir> [--minify]
//   node build-assets.mjs styles  --package <id> --out <dir>
//                                 [--components Styles/components.scss [--preamble Styles/functions.scss]
//                                  --bundle Styles/x.scss=styles/x.css ...]   a bundle trimmed by its components
//                                 [--whole Styles/y.scss=styles/y.css ...]     a stylesheet linked whole (a preset)
//                                 [--icons Styles/z.scss=styles/z.css ...]     an icon font's, trimmed glyph by glyph
//
// Run from the package's own folder: the compilers are resolved from its node_modules, as its npm scripts would.
//
// Both commands say what a piece is through comments in its source, read here and written into the manifest:
//
//   // bit-trim: always              kept in every trimmed bundle
//   // bit-trim: owners BitNav ...    kept whenever an app can use one of these types; a name ending in * stands for every
//                                    type starting with it (BitFc* - the parts of a component family). A stylesheet is
//                                    kept for the component of its own file name unless it says otherwise.
//   // bit-trim: markup data-x ...    kept whenever the code an app can run - its own and the library's it reaches - or
//                                    the .html and .js of its projects hold one of these texts: for a script that acts on
//                                    what markup says (a data- attribute, a class, an event name) rather than being
//                                    called, and that any component, or the app's own markup, can say it
//   // bit-trim: on-demand           kept only when something calls into it (the default; it acknowledges that the
//                                    file runs code as it loads, which a script must say something about)
//
// A script is kept, too, when an interop identifier an app's code can reach names it ("BitBlazorUI.Utils.isRtl" is
// the script exporting Utils.isRtl), and when a script kept uses it. The bundle is written in the compiler's order of
// the files, except that a script is moved after every script it uses as it loads: what runs at load - a listener
// added to the page, an install() - finds what it calls already defined, and listeners added to one node at load keep
// the order their scripts load in.

import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath, pathToFileURL } from 'node:url';

const MANIFEST_VERSION = 1;
const NAMESPACE = 'BitBlazorUI';
const COMPONENTS_MARKER = '/*! bit-assets:components */';

class BuildError extends Error { }

const packageDir = process.cwd();
const require = createRequire(path.join(packageDir, 'package.json'));

let options = {};

// ---------------------------------------------------------------------------------------------------------------------
// Scripts
// ---------------------------------------------------------------------------------------------------------------------

function buildScripts() {
    const ts = require('typescript');
    const packageId = required('package');
    const tsconfigPath = path.resolve(packageDir, required('tsconfig'));
    const bundle = toPosix(required('bundle'));
    const outDir = path.resolve(packageDir, required('out'));
    const minify = options.minify === true;

    const configFile = ts.readConfigFile(tsconfigPath, ts.sys.readFile);
    if (configFile.error) throw new BuildError(formatDiagnostics(ts, [configFile.error]));

    const parsed = ts.parseJsonConfigFileContent(configFile.config, ts.sys, path.dirname(tsconfigPath), undefined, tsconfigPath);
    if (parsed.errors.length) throw new BuildError(formatDiagnostics(ts, parsed.errors));

    // One file per source instead of the single outFile the tsconfig names for editors and plain tsc runs. The text
    // of each is what the outFile would have held for it: references to what another file declares in the shared
    // namespace are written qualified (BitBlazorUI.Utils.x) either way, since the checker resolves them, not the
    // file layout. Nothing reaches the disk through the compiler - the emit below is captured in memory.
    const compilerOptions = { ...parsed.options, outFile: undefined, out: undefined, outDir: undefined, noEmit: false, declaration: false, sourceMap: false, inlineSourceMap: false, newLine: ts.NewLineKind.LineFeed, removeComments: false };

    const program = ts.createProgram({ rootNames: parsed.fileNames, options: compilerOptions });
    const diagnostics = ts.getPreEmitDiagnostics(program);
    const errors = diagnostics.filter(d => d.category === ts.DiagnosticCategory.Error);
    if (errors.length) throw new BuildError(formatDiagnostics(ts, errors));

    const checker = program.getTypeChecker();
    const roots = new Set(parsed.fileNames.map(f => path.resolve(f)));

    // The compiler's own order, which is the order the outFile concatenates them in and so the order the library
    // has always loaded them in. Only the files of this package: the d.ts files it compiles against (the DOM, the
    // declarations of another package's namespace) are not part of what it ships.
    const sources = program.getSourceFiles().filter(sf => !sf.isDeclarationFile && roots.has(path.resolve(sf.fileName)));

    const modules = sources.map(sf => ({ sf, name: path.basename(sf.fileName, '.ts'), file: toPosix(path.relative(packageDir, sf.fileName)) }));

    const byName = new Map();
    for (const m of modules) {
        if (byName.has(m.name)) throw new BuildError(`${m.file}: a second script named '${m.name}' (the first is ${byName.get(m.name).file}). A script's file name is its module name, which a csproj names to keep it, so it has to be unique in the package.`);
        byName.set(m.name, m);
    }

    const bySourceFile = new Map(modules.map(m => [m.sf, m]));

    for (const m of modules) {
        m.exports = collectExports(ts, m.sf);
        m.pragma = readPragma(m.sf.text, m.file);
        const refs = collectReferences(ts, checker, m.sf, bySourceFile);
        m.deps = refs.deps;
        m.loadDeps = refs.loadDeps;
        m.externals = refs.externals;
        m.loadTimeCalls = refs.loadTimeCalls;
    }

    // A file that runs code as it loads - a listener on the document, an install() - has to say when an app needs it,
    // because nothing calling into it is what keeps it otherwise: a delegated listener is reached through the markup
    // a component renders, which no interop identifier names.
    const unsaid = modules.filter(m => m.loadTimeCalls.length && !m.pragma).map(m => {
        const at = m.loadTimeCalls[0];
        return `${m.file}(${at.line},${at.column}): this script runs code as it loads ('${at.text}') but does not say when an app needs it. Add a '// bit-trim: always', '// bit-trim: owners <Component> ...' or '// bit-trim: on-demand' comment to it (see build-assets.mjs).`;
    });
    if (unsaid.length) throw new BuildError(unsaid.join('\nbuild-assets : error : '));

    // What runs as the bundle loads has to find what it uses already defined, so a script goes after every script it
    // uses as it loads, and otherwise keeps the compiler's order: a stable topological sort. (A reference made inside
    // a function waits until the function is called, by which time the whole bundle has loaded, and may point either
    // way.) A trimmed bundle keeps a subset of this order, which keeps it valid.
    modules.splice(0, modules.length, ...loadOrder(modules));

    const emitted = new Map();
    for (const m of modules) {
        let text = null;
        const result = program.emit(m.sf, (fileName, data) => { if (fileName.endsWith('.js')) text = data; });
        if (result.emitSkipped || text === null) throw new BuildError(`${m.file}: the compiler emitted nothing for it.`);
        emitted.set(m.name, text);
    }

    const esbuild = minify ? require('esbuild') : null;
    // The minifier is held to the target the tsconfig compiles to: left to its default it writes newer syntax (?? and ?.)
    // back into the script, which the engines that target is there for reject.
    const minifyTarget = esbuildTarget(ts, compilerOptions.target);

    const scriptsDir = path.join(outDir, 'scripts');
    resetDir(scriptsDir);

    // The compiler writes "use strict" at the head of every file it emits on its own, where the outFile has it once:
    // only at the very start of a script is it a directive, so it goes to the head of the bundle, once, and the
    // trimming puts it back there the same way.
    const strict = /^"use strict";\n/;
    const prologue = [...emitted.values()].some(text => strict.test(text.replace(/^\uFEFF/, ''))) ? '"use strict";\n' : '';

    const manifestModules = [];
    const parts = [prologue];
    for (const m of modules) {
        let code = emitted.get(m.name).replace(/^\uFEFF/, '').replace(strict, '');
        // A file of types alone (an interface, the declarations of a vendored library) compiles to nothing, which is no
        // piece of the bundle: nothing can depend on it, and a trimmed bundle never needs it.
        if (code.trim().length === 0) continue;
        if (esbuild) code = esbuild.transformSync(code, { minify: true, target: minifyTarget, loader: 'js', charset: 'utf8' }).code;
        if (!code.endsWith('\n')) code += '\n';

        const chunkFile = `scripts/${m.name}.js`;
        fs.writeFileSync(path.join(outDir, chunkFile), code);
        parts.push(code);

        manifestModules.push({
            name: m.name,
            file: chunkFile,
            source: m.file,
            exports: [...m.exports].sort(),
            deps: [...m.deps].sort(),
            loadDeps: [...m.loadDeps].sort(),
            externals: [...m.externals].sort(),
            always: m.pragma?.always === true,
            owners: m.pragma?.owners ?? [],
            markup: m.pragma?.markup ?? [],
        });
    }

    // What a script uses of a file that compiled to nothing is a type of it - the compiler resolved it to a declaration
    // that emits no code - so it is no dependency of the bundle either.
    const shipped = new Set(manifestModules.map(m => m.name));
    for (const m of manifestModules) {
        m.deps = m.deps.filter(d => shipped.has(d));
        m.loadDeps = m.loadDeps.filter(d => shipped.has(d));
    }

    reportCycles(manifestModules);

    writeBundle(path.join(packageDir, 'wwwroot', bundle), parts.join(''));

    writeJson(path.join(outDir, 'scripts.json'), {
        version: MANIFEST_VERSION,
        package: packageId,
        namespace: NAMESPACE,
        bundle,
        minified: minify,
        prologue,
        modules: manifestModules,
    });

    log(`${packageId}: ${bundle} written from ${modules.length} script modules${minify ? ' (minified)' : ''}.`);
}

// The members a file adds to the BitBlazorUI namespace, by their path under it: 'TextField' for a class, 'Utils.isRtl'
// for a function of a namespace that several files add to, 'Legacy.Utils' for a class of a nested namespace. These are
// what an interop identifier ("BitBlazorUI.Utils.isRtl") and another package's reference are resolved against.
function collectExports(ts, sf) {
    const exports = new Set();

    const visitModule = (decl, prefix) => {
        const name = decl.name.text;
        const here = prefix === null ? (name === NAMESPACE ? '' : null) : (prefix ? `${prefix}.${name}` : name);
        if (here === null) return;

        let body = decl.body;
        if (!body) return;
        if (ts.isModuleDeclaration(body)) return visitModule(body, here);

        for (const statement of body.statements) visitStatement(statement, here);
    };

    const isExported = node => ts.getCombinedModifierFlags(node) & ts.ModifierFlags.Export;
    const add = (prefix, name) => exports.add(prefix ? `${prefix}.${name}` : name);

    const visitStatement = (statement, prefix) => {
        if (ts.isModuleDeclaration(statement)) {
            if (prefix === null || isExported(statement)) visitModule(statement, prefix);
            return;
        }

        if (prefix === null || !isExported(statement)) return;

        if ((ts.isClassDeclaration(statement) || ts.isFunctionDeclaration(statement) || ts.isEnumDeclaration(statement)) && statement.name) {
            add(prefix, statement.name.text);
        } else if (ts.isVariableStatement(statement)) {
            for (const d of statement.declarationList.declarations) if (ts.isIdentifier(d.name)) add(prefix, d.name.text);
        }
    };

    for (const statement of sf.statements) visitStatement(statement, null);

    return exports;
}

// Every value another file of the package declares that this one uses (deps), the subset used while the bundle loads
// rather than inside a function (loadDeps), and what it uses of another package's namespace (externals, by path).
// Types are erased from the emitted script and are no dependency at all. So is a namespace: BitBlazorUI and Utils are
// containers several files add to, and the member reached through them is what decides the file.
function collectReferences(ts, checker, sf, bySourceFile) {
    const deps = new Set();
    const loadDeps = new Set();
    const externals = new Set();
    const loadTimeCalls = [];
    const self = bySourceFile.get(sf);

    const record = (node, loadTime) => {
        let symbol = ts.isShorthandPropertyAssignment(node.parent) && node.parent.name === node
            ? checker.getShorthandAssignmentValueSymbol(node.parent)
            : checker.getSymbolAtLocation(node);
        if (!symbol) return;
        if (symbol.flags & ts.SymbolFlags.Alias) symbol = checker.getAliasedSymbol(symbol);
        if (symbol.flags & (ts.SymbolFlags.Module | ts.SymbolFlags.TypeAlias | ts.SymbolFlags.Interface | ts.SymbolFlags.TypeParameter)) return;
        if (!symbol.declarations?.length) return;

        for (const declaration of symbol.declarations) {
            const file = declaration.getSourceFile();
            if (file === sf) continue;

            const owner = bySourceFile.get(file);
            if (owner) {
                deps.add(owner.name);
                if (loadTime) loadDeps.add(owner.name);
            } else if (file.isDeclarationFile && !isLibraryDeclaration(file.fileName)) {
                // Another package's namespace, as this one declares it to compile against it: kept by its path,
                // and resolved against that package's own exports when an app is published.
                const qualified = checker.getFullyQualifiedName(symbol);
                if (qualified.startsWith(`${NAMESPACE}.`)) externals.add(qualified.substring(NAMESPACE.length + 1));
            }
        }
    };

    const isStatic = node => (ts.getCombinedModifierFlags(node) & ts.ModifierFlags.Static) !== 0;

    const visit = (node, loadTime) => {
        if (ts.isTypeNode(node) || ts.isInterfaceDeclaration(node) || ts.isTypeAliasDeclaration(node)) return;
        if (ts.isHeritageClause(node) && node.token === ts.SyntaxKind.ImplementsKeyword) return;
        if (node.kind === ts.SyntaxKind.DeclareKeyword) return;
        if ((ts.isModuleDeclaration(node) || ts.isClassDeclaration(node) || ts.isFunctionDeclaration(node) || ts.isVariableStatement(node) || ts.isEnumDeclaration(node))
            && (ts.getCombinedModifierFlags(node) & ts.ModifierFlags.Ambient)) return;

        if (ts.isFunctionLike(node) && !ts.isClassStaticBlockDeclaration(node)) loadTime = false;
        else if (ts.isPropertyDeclaration(node) && !isStatic(node)) loadTime = false;

        if (loadTime && (ts.isCallExpression(node) || ts.isTaggedTemplateExpression(node))) {
            const { line, character } = sf.getLineAndCharacterOfPosition(node.getStart(sf));
            loadTimeCalls.push({ line: line + 1, column: character + 1, text: node.getText(sf).split('\n')[0].substring(0, 80) });
        }

        if (ts.isIdentifier(node)) record(node, loadTime);

        ts.forEachChild(node, child => visit(child, loadTime));
    };

    ts.forEachChild(sf, child => visit(child, true));

    deps.delete(self.name);
    loadDeps.delete(self.name);

    return { deps, loadDeps, externals, loadTimeCalls };
}

function esbuildTarget(ts, target) {
    const name = ts.ScriptTarget[target ?? ts.ScriptTarget.ES5];
    if (!name) throw new BuildError(`the tsconfig's target (${target}) is not one the compiler knows.`);
    return /^(latest|esnext)$/i.test(name) ? 'esnext' : name.toLowerCase();
}

function loadOrder(modules) {
    const byName = new Map(modules.map(m => [m.name, m]));
    const placed = new Set();
    const order = [];
    const visiting = [];

    const place = m => {
        if (placed.has(m.name)) return;
        if (visiting.includes(m.name)) {
            const cycle = [...visiting.slice(visiting.indexOf(m.name)), m.name].map(n => byName.get(n).file);
            throw new BuildError(`these scripts use each other as they load, so no order of the bundle gives each what it needs: ${cycle.join(' -> ')}. Move one of the uses into a function.`);
        }

        visiting.push(m.name);
        for (const dep of [...m.loadDeps].sort((a, b) => modules.indexOf(byName.get(a)) - modules.indexOf(byName.get(b)))) place(byName.get(dep));
        visiting.pop();

        placed.add(m.name);
        order.push(m);
    };

    for (const m of modules) place(m);
    return order;
}

function isLibraryDeclaration(fileName) {
    return toPosix(fileName).includes('/node_modules/');
}

// Two scripts that use each other are kept together or not at all, which is a coarser unit than either of them. Said,
// not refused: the files of a component family legitimately call back and forth.
function reportCycles(modules) {
    const byName = new Map(modules.map(m => [m.name, m]));
    let index = 0;
    const indices = new Map(), low = new Map(), stack = [], onStack = new Set(), cycles = [];

    const strongConnect = name => {
        indices.set(name, index); low.set(name, index); index++;
        stack.push(name); onStack.add(name);

        for (const dep of byName.get(name).deps) {
            if (!indices.has(dep)) { strongConnect(dep); low.set(name, Math.min(low.get(name), low.get(dep))); }
            else if (onStack.has(dep)) low.set(name, Math.min(low.get(name), indices.get(dep)));
        }

        if (low.get(name) === indices.get(name)) {
            const component = [];
            let member;
            do { member = stack.pop(); onStack.delete(member); component.push(member); } while (member !== name);
            if (component.length > 1) cycles.push(component.sort());
        }
    };

    for (const m of modules) if (!indices.has(m.name)) strongConnect(m.name);

    for (const cycle of cycles) log(`  scripts kept together (they use each other): ${cycle.join(', ')}`);
}

// ---------------------------------------------------------------------------------------------------------------------
// Styles
// ---------------------------------------------------------------------------------------------------------------------

function buildStyles() {
    const sass = require('sass');
    const packageId = required('package');
    const outDir = path.resolve(packageDir, required('out'));
    const componentsPath = options.components ? path.resolve(packageDir, options.components) : null;
    const trimmable = list(options.bundle).map(parseBundleArg);
    const whole = list(options.whole).map(parseBundleArg);

    if (trimmable.length && !componentsPath) throw new BuildError(`--bundle needs --components: a bundle is trimmed by the components it imports.`);

    const stylesDir = path.join(outDir, 'styles');
    resetDir(stylesDir);

    const chunks = componentsPath ? readComponentImports(componentsPath) : [];

    // What a bundle imports ahead of its components file and a component may use without importing it itself - the
    // functions, mixins and $ aliases. Only what writes no CSS of its own can stand here: anything it wrote would land
    // in every chunk, and the check below that the chunks add up to the bundle would say so.
    const preamble = list(options.preamble).map(p => `@import "${toPosix(path.relative(path.dirname(componentsPath ?? packageDir), path.resolve(packageDir, p)))}";`).join('');

    const importer = scssImporter(componentsPath);
    const compileOptions = { style: 'compressed', charset: false, sourceMap: false, silenceDeprecations: ['import'], loadPaths: [packageDir] };

    // Each component compiled on its own, the way the components file imports it, so that its CSS can be kept or left
    // out of a bundle without touching any other's. What one of them leans on from another (a variable, an @extend)
    // fails right here rather than in an app.
    for (const chunk of chunks) {
        chunk.css = sass.compileString(`${preamble}@import "${chunk.import}";`, { ...compileOptions, url: pathToFileURL(componentsPath), importer: importer.plain }).css;
        chunk.file = `styles/chunks/${chunk.name}.css`;
        fs.mkdirSync(path.dirname(path.join(outDir, chunk.file)), { recursive: true });
        fs.writeFileSync(path.join(outDir, chunk.file), chunk.css);
    }

    const bundles = [];

    for (const bundle of trimmable) {
        const full = sass.compile(bundle.entry, compileOptions).css;

        let prefix = '', suffix = '';
        if (path.resolve(bundle.entry) !== componentsPath) {
            const base = sass.compileString(fs.readFileSync(bundle.entry, 'utf8'), { ...compileOptions, url: pathToFileURL(bundle.entry), importer: importer.withoutComponents }).css;
            const at = base.indexOf(COMPONENTS_MARKER);
            if (at < 0 || base.indexOf(COMPONENTS_MARKER, at + 1) >= 0) {
                throw new BuildError(`${rel(bundle.entry)}: expected it to import ${rel(componentsPath)} exactly once.`);
            }
            prefix = base.substring(0, at);
            suffix = base.substring(at + COMPONENTS_MARKER.length);
        }

        const composed = prefix + chunks.map(c => c.css).join('') + suffix;
        if (composed !== full) throw new BuildError(describeMismatch(bundle, full, composed));

        const name = path.basename(bundle.output, '.css');
        const prefixFile = `styles/${name}.prefix.css`;
        const suffixFile = `styles/${name}.suffix.css`;
        fs.writeFileSync(path.join(outDir, prefixFile), prefix);
        fs.writeFileSync(path.join(outDir, suffixFile), suffix);

        writeBundle(path.join(packageDir, 'wwwroot', bundle.output), withCharset(full));

        bundles.push({ bundle: bundle.output, prefix: prefixFile, suffix: suffixFile });
    }

    checkKeyframes(chunks, bundles.map(b => fs.readFileSync(path.join(outDir, b.prefix), 'utf8') + fs.readFileSync(path.join(outDir, b.suffix), 'utf8')));

    for (const bundle of whole) {
        const full = sass.compile(bundle.entry, compileOptions).css;
        writeBundle(path.join(packageDir, 'wwwroot', bundle.output), withCharset(full));
    }

    const iconSets = list(options.icons).map(parseBundleArg).map(bundle => buildIconSet(sass, compileOptions, bundle, outDir));

    writeJson(path.join(outDir, 'styles.json'), {
        version: MANIFEST_VERSION,
        package: packageId,
        bundles,
        chunks: chunks.map(c => ({ name: c.name, file: c.file, source: c.source, always: c.pragma?.always === true, owners: c.pragma?.owners ?? (c.pragma?.always ? [] : [c.name]) })),
        icons: iconSets,
    });

    const written = trimmable.length + whole.length + iconSets.length;
    log(`${packageId}: ${written} stylesheet(s) written${trimmable.length ? `, ${trimmable.length} of them from ${chunks.length} component chunks` : ''}${iconSets.length ? `, ${iconSets.reduce((n, s) => n + s.count, 0)} icon rules` : ''}.`);
}

// An icon font's stylesheet: one rule per glyph (.bit-icon--Add::before { content: "\E710" }) between the @font-face and
// the base class. Kept as the stylesheet around the glyphs plus one line per glyph, so the trimming can keep the glyphs
// an app names - the names it passes are string literals (BitIconName.Add is a const), which is what it reads.
function buildIconSet(sass, compileOptions, bundle, outDir) {
    const prefixClass = options['icon-class'] ?? 'bit-icon--';
    const full = sass.compile(bundle.entry, compileOptions).css;
    const rule = new RegExp(`\\.${escapeRegExp(prefixClass)}([A-Za-z0-9_-]+)::?before\\{[^{}]*\\}`, 'g');

    const glyphs = [...full.matchAll(rule)];
    if (!glyphs.length) throw new BuildError(`${rel(bundle.entry)}: no '.${prefixClass}<Name>::before' rule in it.`);

    const start = glyphs[0].index;
    const last = glyphs[glyphs.length - 1];
    const end = last.index + last[0].length;
    const prefix = full.substring(0, start);
    const suffix = full.substring(end);

    const names = new Set();
    for (const g of glyphs) {
        if (names.has(g[1])) throw new BuildError(`${rel(bundle.entry)}: the glyph '${g[1]}' is declared twice.`);
        names.add(g[1]);
    }

    if (prefix + glyphs.map(g => g[0]).join('') + suffix !== full) {
        throw new BuildError(`${rel(bundle.entry)}: its glyph rules are not one unbroken run, so the stylesheet cannot be put back together from a subset of them.`);
    }

    writeBundle(path.join(packageDir, 'wwwroot', bundle.output), withCharset(full));

    const name = path.basename(bundle.output, '.css');
    const file = `styles/${name}.icons.json`;
    writeJson(path.join(outDir, file), { prefix, suffix, glyphs: Object.fromEntries(glyphs.map(g => [g[1], g[0]])) });

    return { bundle: bundle.output, file, classPrefix: prefixClass, count: glyphs.length };
}

function escapeRegExp(text) {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// An animation is named by its keyframes, which live in one stylesheet: a component running keyframes another component
// declares would lose them in every app that keeps it and not the other. The bundle's own part is always kept, so
// keyframes declared there (bit-spin, bit-fade-in) are anyone's.
function checkKeyframes(chunks, bases) {
    const declared = css => new Set([...css.matchAll(/@(?:-webkit-)?keyframes\s+([A-Za-z0-9_-]+)/g)].map(m => m[1]));
    const shared = new Set(bases.flatMap(css => [...declared(css)]));
    const owner = new Map();
    for (const chunk of chunks) for (const name of declared(chunk.css)) if (!owner.has(name)) owner.set(name, chunk.name);

    const problems = [];
    for (const chunk of chunks) {
        const own = declared(chunk.css);
        for (const match of chunk.css.matchAll(/animation(?:-name)?\s*:([^;}]*)/g)) {
            for (const word of match[1].split(/[\s,]+/)) {
                const from = owner.get(word);
                if (from && from !== chunk.name && !own.has(word) && !shared.has(word)) {
                    problems.push(`${chunk.source}: runs the keyframes '${word}' of ${from}, which a bundle trimmed down to ${chunk.name} would not have. Declare them in the component itself, or in the bundle's general stylesheet.`);
                }
            }
        }
    }

    if (problems.length) throw new BuildError([...new Set(problems)].join('\nbuild-assets : error : '));
}

function parseBundleArg(value) {
    const at = value.lastIndexOf('=');
    if (at <= 0) throw new BuildError(`'${value}': expected <source.scss>=<path under wwwroot>.`);
    return { entry: path.resolve(packageDir, value.substring(0, at)), output: toPosix(value.substring(at + 1)) };
}

function readComponentImports(componentsPath) {
    const text = stripScssComments(fs.readFileSync(componentsPath, 'utf8'));
    const chunks = [];
    const seen = new Map();

    for (const match of text.matchAll(/@import\s+(["'])([^"']+)\1\s*;/g)) {
        const importPath = match[2];
        const source = resolveScss(path.resolve(path.dirname(componentsPath), importPath));
        if (!source) throw new BuildError(`${rel(componentsPath)}: cannot find '${importPath}'.`);

        const name = path.basename(source).replace(/^_/, '').replace(/\.(scss|sass|css)$/, '');
        if (seen.has(name)) throw new BuildError(`${rel(componentsPath)}: two components named '${name}' (${seen.get(name)} and ${rel(source)}). A component stylesheet's file name is its chunk name.`);
        seen.set(name, rel(source));

        chunks.push({ name, import: importPath, source: rel(source), pragma: readPragma(fs.readFileSync(source, 'utf8'), rel(source)) });
    }

    return chunks;
}

// Resolves loads the way the sass CLI does (relative to the importing file, then the package folder), and can stand
// an empty marker in for the components file - which is how a bundle's own part, before and after its components, is
// compiled on its own.
function scssImporter(componentsPath) {
    const make = replaceComponents => ({
        canonicalize(url, context) {
            if (/^[a-z][a-z0-9+.-]*:/i.test(url) && !url.startsWith('file:')) return null;
            const target = url.startsWith('file:') ? fileURLToPath(url) : url;
            const bases = [];
            if (context.containingUrl?.protocol === 'file:') bases.push(path.dirname(fileURLToPath(context.containingUrl)));
            bases.push(packageDir);
            for (const base of bases) {
                const resolved = resolveScss(path.resolve(base, target));
                if (resolved) return pathToFileURL(resolved);
            }
            return null;
        },
        load(canonicalUrl) {
            const file = fileURLToPath(canonicalUrl);
            if (replaceComponents && componentsPath && path.resolve(file) === componentsPath) return { contents: COMPONENTS_MARKER, syntax: 'scss' };
            const syntax = file.endsWith('.sass') ? 'indented' : file.endsWith('.css') ? 'css' : 'scss';
            return { contents: fs.readFileSync(file, 'utf8'), syntax };
        },
    });

    return { plain: make(false), withoutComponents: make(true) };
}

function resolveScss(target) {
    const dir = path.dirname(target);
    const base = path.basename(target);
    const candidates = /\.(scss|sass|css)$/.test(base)
        ? [target, path.join(dir, `_${base}`)]
        : ['.scss', '.sass', '.css'].flatMap(ext => [`${target}${ext}`, path.join(dir, `_${base}${ext}`)]).concat([path.join(target, 'index.scss'), path.join(target, '_index.scss')]);

    return candidates.find(c => fs.existsSync(c) && fs.statSync(c).isFile()) ?? null;
}

function stripScssComments(text) {
    return text.replace(/\/\*[\s\S]*?\*\//g, '').replace(/(^|[^:])\/\/.*$/gm, '$1');
}

// The sass CLI marks a compressed stylesheet holding anything beyond ASCII with a byte order mark (and an expanded one
// with @charset): with neither, a stylesheet served without a charset is read in the encoding of the page that links
// it. Done here, once per bundle, because a chunk never stands at the start of one.
function withCharset(css) {
    return /[^\x00-\x7F]/.test(css) ? `\uFEFF${css}` : css;
}

function describeMismatch(bundle, full, composed) {
    let at = 0;
    while (at < full.length && full[at] === composed[at]) at++;
    const show = s => JSON.stringify(s.substring(Math.max(0, at - 80), at + 80));
    return `${rel(bundle.entry)}: compiled whole, it is not the same as its parts compiled one by one, so a trimmed copy of it would not be a subset of it. `
        + `They part at character ${at}:\n  whole: ${show(full)}\n  parts: ${show(composed)}\n`
        + `A component stylesheet that leans on another one - a variable it sets, a selector it @extends - or on the bundle around it does this.`;
}

// ---------------------------------------------------------------------------------------------------------------------
// Shared
// ---------------------------------------------------------------------------------------------------------------------

function readPragma(text, file) {
    let pragma = null;
    for (const match of text.matchAll(/\/\/\s*bit-trim:\s*(.*)$/gm)) {
        const words = match[1].trim().split(/[\s,]+/).filter(Boolean);
        const [kind, ...rest] = words;
        pragma ??= {};
        if (kind === 'always' && rest.length === 0) pragma.always = true;
        else if (kind === 'on-demand' && rest.length === 0) pragma.onDemand = true;
        else if (kind === 'owners' && rest.length) pragma.owners = [...new Set([...(pragma.owners ?? []), ...rest])];
        else if (kind === 'markup' && rest.length) pragma.markup = [...new Set([...(pragma.markup ?? []), ...rest])];
        else throw new BuildError(`${file}: '// bit-trim: ${match[1].trim()}' is not one of 'always', 'on-demand', 'owners <Type> ...' or 'markup <text> ...'.`);
    }

    if (pragma && pragma.always && (pragma.owners || pragma.onDemand || pragma.markup)) throw new BuildError(`${file}: 'always' says everything there is to say; drop the other bit-trim comments.`);
    return pragma;
}

function writeBundle(file, text) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    // No source map is written - it would point at sources the package does not carry - so one an older build left
    // beside the bundle would only describe a stylesheet that no longer exists.
    fs.rmSync(`${file}.map`, { force: true });
    // Rewritten only when it changed, so the content a static web asset was fingerprinted from stays byte for byte what
    // it was; touched either way, so MSBuild sees the target's outputs as newer than the inputs that just ran it.
    if (fs.existsSync(file) && fs.readFileSync(file, 'utf8') === text) {
        const now = new Date();
        fs.utimesSync(file, now, now);
        return;
    }
    fs.writeFileSync(file, text);
}

function writeJson(file, value) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, JSON.stringify(value, null, 2) + '\n');
}

function resetDir(dir) {
    fs.rmSync(dir, { recursive: true, force: true });
    fs.mkdirSync(dir, { recursive: true });
}

function formatDiagnostics(ts, diagnostics) {
    return ts.formatDiagnostics(diagnostics, {
        getCanonicalFileName: f => f,
        getCurrentDirectory: () => packageDir,
        getNewLine: () => '\n',
    }).trimEnd();
}

function parseArgs(argv) {
    const result = {};
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (!arg.startsWith('--')) throw new BuildError(`unexpected argument '${arg}'.`);
        const key = arg.substring(2);
        const next = argv[i + 1];
        const value = next === undefined || next.startsWith('--') ? true : (i++, next);
        if (key in result) result[key] = [].concat(result[key], value);
        else result[key] = value;
    }
    return result;
}

function required(key) {
    const value = options[key];
    if (typeof value !== 'string' || !value) throw new BuildError(`--${key} is required.`);
    return value;
}

function list(value) {
    return value === undefined ? [] : [].concat(value);
}

function rel(file) {
    return toPosix(path.relative(packageDir, file));
}

function toPosix(p) {
    return p.replace(/\\/g, '/');
}

function log(message) {
    console.log(`build-assets: ${message}`);
}

function fail(message) {
    // MSBuild reads "error" lines of this shape as errors of the build, so the message lands in the error list. The
    // compiler's own diagnostics already have the shape, with their file and position.
    for (const line of message.split('\n')) {
        console.error(/^\S.*\(\d+,\d+\): error /.test(line) || line.startsWith('build-assets : error : ') || /^\s/.test(line) ? line : `build-assets : error : ${line}`);
    }
    process.exit(1);
}

try {
    const [command, ...rest] = process.argv.slice(2);
    options = parseArgs(rest);

    if (command === 'scripts') buildScripts();
    else if (command === 'styles') buildStyles();
    else throw new BuildError(`unknown command '${command ?? ''}': expected 'scripts' or 'styles'.`);
} catch (e) {
    fail(e instanceof BuildError ? e.message : (e?.stack ?? String(e)));
}
