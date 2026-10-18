// Builds the JavaScript and CSS of one bit BlazorUI package, in the shapes the package ships them in:
//
//   * the bundles under wwwroot (bit.blazorui.js, bit.blazorui.css, ...) an app references with a <script> or a
//     <link>, holding everything - exactly what a build and every development run serve;
//   * the pieces those bundles are made of - one chunk per TypeScript file and one per component stylesheet - and a
//     manifest of what each piece is, which the package carries for the publish-time trimming
//     (Bit.BlazorUI.Build's TrimBitBlazorUIAssets) to put a bundle back together from only the pieces an app can use;
//   * the lazy scripts (BitBlazorUILazyScripts) under wwwroot/scripts: one ES module per script piece
//     (scripts/modules/<name>.js), importing the pieces of its package it uses, which the .NET side import()s the first
//     time something calls into it; a boot module (scripts/<bundle>.boot.js) importing the pieces that act on the page
//     without being called - the 'always' and 'markup' ones below; and the package's JavaScript initializer
//     (scripts/<package>.lib.module.js), which Blazor imports as the app starts and which imports the boot module.
//
// A bundle is written FROM its pieces, never next to them, so a trimmed bundle is a subset of the very text the full
// one is: the scripts are concatenated in the load order below, and every stylesheet is checked to compile to exactly
// its own prefix, chunks and suffix before anything is written. A module file is its piece too, behind the imports
// of what it uses and a guard that leaves it out where the package's bundle already ran.
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
// A stylesheet says 'always' or 'owners' only: it is kept for components, and no markup is read for it.
//
// A script runs code as it loads when its top level - or a function invoked right where it is written - calls a
// function, constructs anything but a plain container (a Map, a Set), or assigns to the platform (window.onresize = ...).
//
// A script is kept, too, when an interop identifier an app's code can reach names it ("BitBlazorUI.Utils.isRtl" is
// the script exporting Utils.isRtl), and when a script kept uses it. The bundle is written in the compiler's order of
// the files, except that a script is moved after every script it uses as it loads - what its top level uses, and what
// the functions it calls there use in turn: what runs at load - a listener added to the page, an install() - finds what
// it calls already defined, and listeners added to one node at load keep the order their scripts load in (the manifest
// lists each script's, as "window keydown capture").
//
// As lazy scripts each file is a module of its own, so what one script reads of another is what that one adds to the
// namespace: a script using a declaration another file makes at its top level, outside the namespace - a global of the
// page in the bundle, a binding of its own module as a lazy script - fails the build.
//
// The lazy scripts read one more comment, of what a script exports:
//
//   // bit-lazy: teardown Callouts.clear Utils.disposeFocusTrap ...
//                                    each a function that only undoes what the script itself did - a listener it added, a
//                                    registry entry, a class it set - so a call of it in a page the script never ran in
//                                    has nothing to undo: the .NET side skips the call rather than download the script for
//                                    it (a component disposed before it ever called into its script)

import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath, pathToFileURL } from 'node:url';

// 2: the lazy scripts (each script's module, whether it runs code as it loads, whether it boots, its teardown functions)
// and a bundle prologue that registers the bundle.
const MANIFEST_VERSION = 2;
const LAZY_MANIFEST_VERSION = 1;
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
    const caches = { files: new Map(), listeners: new Map() };

    for (const m of modules) {
        m.exports = collectExports(ts, m.sf);
        m.pragma = readPragma(m.sf.text, m.file, SCRIPT_PRAGMAS);
        m.teardown = readTeardowns(m.sf.text, m.file, m.exports);
        const refs = collectReferences(ts, checker, m.sf, bySourceFile, caches);
        m.deps = refs.deps;
        m.loadDeps = refs.loadDeps;
        m.externals = refs.externals;
        m.loadTimeCalls = refs.loadTimeCalls;
        m.listeners = refs.listeners;
        m.globals = refs.globals;
    }

    // A script reads of another only what that one adds to the namespace: as lazy scripts each file is a module, whose
    // top-level declarations are its own, so a name the bundle shares as a global of the page is undefined there.
    const shared = modules.flatMap(m => [...new Map(m.globals.map(g => [g.name, g])).values()].map(g =>
        `${m.file}(${g.line},${g.column}): uses '${g.name}', which ${g.owner} declares at its top level, outside the ${NAMESPACE} namespace: as a lazy script each file is a module of its own, which no other script can read a top-level declaration of. Declare it in the namespace, exported, for another script to use.`));
    if (shared.length) throw new BuildError(shared.join('\nbuild-assets : error : '));

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
    // trimming puts it back there the same way. After it, the bundle says that it ran, which is what keeps a module
    // file of the lazy scripts from running a piece a second time in a page that loaded the bundle as well.
    const strict = /^"use strict";\n/;
    const prologue = ([...emitted.values()].some(text => strict.test(text.replace(/^\uFEFF/, ''))) ? '"use strict";\n' : '')
        + `${namespaceOfWindow()}(${NAMESPACE}.$bundles || (${NAMESPACE}.$bundles = {}))[${JSON.stringify(packageId)}] = true;\n`;

    const manifestModules = [];
    const codes = new Map();
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
        codes.set(m.name, code);

        const always = m.pragma?.always === true;
        const markup = m.pragma?.markup ?? [];

        manifestModules.push({
            name: m.name,
            file: chunkFile,
            source: m.file,
            module: `${LAZY_MODULES_DIR}${m.name}.js`,
            exports: [...m.exports].sort(),
            deps: [...m.deps].sort(),
            loadDeps: [...m.loadDeps].sort(),
            externals: [...m.externals].sort(),
            always,
            owners: m.pragma?.owners ?? [],
            markup,
            // What the lazy scripts import as the app starts rather than when something calls into it: a script acting on
            // the page without being called, which nothing calling it would ever load.
            boot: always || markup.length > 0,
            loads: m.loadTimeCalls.length > 0,
            teardown: m.teardown,
            listeners: m.listeners,
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

    const lazy = writeLazyScripts(packageId, bundle, manifestModules, codes);

    writeJson(path.join(outDir, 'scripts.json'), {
        version: MANIFEST_VERSION,
        package: packageId,
        namespace: NAMESPACE,
        bundle,
        minified: minify,
        prologue,
        lazy,
        modules: manifestModules,
    });

    // What the .NET side of the lazy scripts reads, embedded into the package's assembly (see Bit.BlazorUI.Assets.targets):
    // which script exports what, what each imports, what it needs of another package, and which of its functions only
    // undo what it did. Kept apart from scripts.json, which the publish reads and an app never downloads.
    writeJson(path.join(outDir, 'lazy-scripts.json'), {
        version: LAZY_MANIFEST_VERSION,
        package: packageId,
        boot: lazy.boot,
        modules: manifestModules.map(m => ({ name: m.name, path: m.module, boot: m.boot, exports: m.exports, deps: m.deps, externals: m.externals, teardown: m.teardown })),
    }, { compact: true });

    log(`${packageId}: ${bundle} written from ${modules.length} script modules${minify ? ' (minified)' : ''}, and as ${manifestModules.length} lazy modules${lazy.boot ? `, ${manifestModules.filter(m => m.boot).length} of them booting` : ''}.`);
}

// Where the module files of the lazy scripts live under the package's wwwroot.
const LAZY_MODULES_DIR = 'scripts/modules/';

// The statement every script of the namespace starts its scope with when it runs as a module or after a bundle that may
// not have created the namespace yet: the global, created when it is not there (window, since an ES module's own
// top-level var would not be one).
function namespaceOfWindow() {
    return `var ${NAMESPACE} = window.${NAMESPACE} || (window.${NAMESPACE} = {});\n`;
}

/**
 * The lazy scripts of a package: one module per piece, a boot module importing the pieces that act without being called,
 * and the JavaScript initializer importing the boot module as the app starts. Written under wwwroot/scripts, beside the
 * bundle, and each one only when it changed, so what a static web asset was fingerprinted from stays what it was.
 */
function writeLazyScripts(packageId, bundle, modules, codes) {
    const wwwroot = path.join(packageDir, 'wwwroot');
    const order = new Map(modules.map((m, i) => [m.name, i]));
    const byOrder = names => [...names].sort((a, b) => order.get(a) - order.get(b));

    // A script the app starts with is loaded by no .NET call, so nothing would import what it needs of another package
    // first (the .NET side does that for every other module, from the externals below).
    const crossing = modules.filter(m => m.boot && m.externals.length);
    if (crossing.length) {
        throw new BuildError(crossing.map(m => `${m.source}: is loaded as the app starts ('always' or 'markup') but uses ${m.externals.join(', ')} of another package, which the lazy scripts load only for what calls into it. Move what it uses into this package, or make it a script that is called.`).join('\nbuild-assets : error : '));
    }

    const written = new Set();
    for (const m of modules) {
        // Its own package's scripts it uses, in the bundle's order, so what runs as they load runs in the order it does
        // in the bundle; the ones of another package are imported by the .NET side, which knows where each package is.
        const imports = byOrder(m.deps).map(d => `import "./${d}.js";\n`).join('');
        const text = imports
            + namespaceOfWindow()
            + `if (!${NAMESPACE}.$bundles || !${NAMESPACE}.$bundles[${JSON.stringify(packageId)}]) {\n`
            + codes.get(m.name)
            + '}\n';

        writeIfChanged(path.join(wwwroot, m.module), text);
        written.add(path.basename(m.module));
    }

    // A module of a script that no longer exists would still be served, and imported by nothing.
    const modulesDir = path.join(wwwroot, LAZY_MODULES_DIR);
    for (const file of fs.existsSync(modulesDir) ? fs.readdirSync(modulesDir) : []) {
        if (!written.has(file)) fs.rmSync(path.join(modulesDir, file), { force: true });
    }

    const booting = modules.filter(m => m.boot);
    const boot = booting.length ? bundle.replace(/\.js$/, '.boot.js') : null;
    const initializer = booting.length ? `${path.posix.dirname(bundle)}/${packageId}.lib.module.js` : null;

    for (const stale of [bundle.replace(/\.js$/, '.boot.js'), `${path.posix.dirname(bundle)}/${packageId}.lib.module.js`]) {
        if (stale !== boot && stale !== initializer) fs.rmSync(path.join(wwwroot, stale), { force: true });
    }

    if (boot) {
        writeIfChanged(path.join(wwwroot, boot), bootModuleText(boot, booting.map(m => m.module)));

        // Blazor imports a file named after the package and ending in .lib.module.js as the app starts, and awaits its
        // callbacks before it renders anything (a JavaScript initializer). It is never trimmed - Blazor's own list of them
        // names it by its fingerprint - so what it imports is the boot module, which the publish rewrites with the scripts
        // an app keeps. Not where the package's bundle ran already (a page keeping both shapes, the <script> tag ahead of
        // Blazor's start): the bundle holds the boot scripts, and their modules would only be fetched to be left out.
        const bundled = `window.${NAMESPACE} && window.${NAMESPACE}.$bundles && window.${NAMESPACE}.$bundles[${JSON.stringify(packageId)}]`;
        writeIfChanged(path.join(wwwroot, initializer),
            '// The JavaScript initializer of the lazy scripts of ' + packageId + ' (BitBlazorUILazyScripts): Blazor imports it as the\n'
            + '// app starts, and it runs the scripts that act on the page without being called. Everything else is imported by\n'
            + '// the .NET side the first time something calls into it.\n'
            + `function boot() { return ${bundled} ? undefined : import("./${path.posix.relative(path.posix.dirname(initializer), boot)}"); }\n`
            + 'export function beforeWebStart() { return boot(); }\n'
            + 'export function beforeStart() { return boot(); }\n');
    }

    return { modules: LAZY_MODULES_DIR, boot, initializer };
}

// The boot module: one import per script the app starts with, in the bundle's order. The publish writes it again from the
// scripts an app keeps (Bit.BlazorUI.Build's AssetComposer), in this very shape.
function bootModuleText(boot, modulePaths) {
    return modulePaths.map(p => `import "./${path.posix.relative(path.posix.dirname(boot), p)}";\n`).join('');
}

// The functions of a script that only undo what it did (see '// bit-lazy: teardown' above): each one a path a member of the
// script's exports starts (Callouts.clear of the class Callouts, Utils.disposeFocusTrap of the namespace function itself),
// declared in the script by that last name.
function readTeardowns(text, file, exports) {
    const names = [];
    for (const match of text.matchAll(/\/\/\s*bit-lazy:\s*(.*)$/gm)) {
        const [kind, ...rest] = match[1].trim().split(/[\s,]+/).filter(Boolean);
        if (kind !== 'teardown' || rest.length === 0) throw new BuildError(`${file}: '// bit-lazy: ${match[1].trim()}' is not 'teardown <function> ...'.`);

        for (const name of rest) {
            const owner = [...exports].some(e => name === e || name.startsWith(`${e}.`));
            const member = name.substring(name.lastIndexOf('.') + 1);
            const declared = new RegExp(`(^|[^\\w$.])(function\\s+|static\\s+(async\\s+)?)?${escapeRegExp(member)}\\s*(\\(|=\\s*(async\\s*)?(\\(|function))`, 'm').test(text);
            if (!owner || !declared) throw new BuildError(`${file}: '// bit-lazy: teardown ${name}' names no function this script exports.`);
            names.push(name);
        }
    }
    return [...new Set(names)].sort();
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
//
// What runs as the bundle loads is more than the file's own top level: a function it calls there runs then too - an
// immediately invoked one, or one of another file, and everything that one uses in turn - so the files a call at load
// reaches through the functions it runs are load dependencies as well.
function collectReferences(ts, checker, sf, bySourceFile, caches) {
    const deps = new Set();
    const loadDeps = new Set();
    const externals = new Set();
    const loadTimeCalls = [];
    const listeners = [];
    const globals = [];
    const self = bySourceFile.get(sf);

    const symbolOf = node => resolveValueSymbol(ts, checker, node);

    const record = (node, loadTime) => {
        const symbol = symbolOf(node);
        if (!symbol) return;

        for (const declaration of symbol.declarations) {
            const file = declaration.getSourceFile();
            if (file === sf) continue;

            const owner = bySourceFile.get(file);
            if (owner) {
                if (!emitsCode(ts, declaration)) continue;
                deps.add(owner.name);
                if (loadTime) loadDeps.add(owner.name);
                if (isTopLevel(ts, declaration)) {
                    const { line, character } = sf.getLineAndCharacterOfPosition(node.getStart(sf));
                    globals.push({ line: line + 1, column: character + 1, name: node.text, owner: owner.file });
                }
            } else if (file.isDeclarationFile && !isLibraryDeclaration(file.fileName)) {
                // Another package's namespace, as this one declares it to compile against it: kept by its path,
                // and resolved against that package's own exports when an app is published.
                const qualified = checker.getFullyQualifiedName(symbol);
                if (qualified.startsWith(`${NAMESPACE}.`)) externals.add(qualified.substring(NAMESPACE.length + 1));
            }
        }
    };

    // The files whose code a call made at load runs - the declarations the callee names, and what they use - and the
    // listeners it adds to the page as it runs.
    const recordRun = callee => {
        const symbol = symbolOf(callee);
        if (!symbol) return;
        for (const declaration of symbol.declarations) {
            for (const name of filesRunBy(ts, checker, declaration, bySourceFile, caches.files)) loadDeps.add(name);
            listeners.push(...listenersRunBy(ts, checker, declaration, bySourceFile, caches.listeners, new Set()));
        }
    };

    const isStatic = node => (ts.getCombinedModifierFlags(node) & ts.ModifierFlags.Static) !== 0;

    const flag = node => {
        const { line, character } = sf.getLineAndCharacterOfPosition(node.getStart(sf));
        loadTimeCalls.push({ line: line + 1, column: character + 1, text: node.getText(sf).split('\n')[0].substring(0, 80) });
    };

    const visit = (node, loadTime) => {
        // The base class of a class is a value, evaluated where the class is declared - which ts.isTypeNode, true for
        // the node wrapping an extends clause's expression, would pass over with the type arguments around it.
        if (ts.isExpressionWithTypeArguments(node) && isClassExtends(ts, node)) {
            visit(node.expression, loadTime);
            return;
        }
        if (ts.isTypeNode(node) || ts.isInterfaceDeclaration(node) || ts.isTypeAliasDeclaration(node)) return;
        if (ts.isHeritageClause(node) && node.token === ts.SyntaxKind.ImplementsKeyword) return;
        if (node.kind === ts.SyntaxKind.DeclareKeyword) return;
        if ((ts.isModuleDeclaration(node) || ts.isClassDeclaration(node) || ts.isFunctionDeclaration(node) || ts.isVariableStatement(node) || ts.isEnumDeclaration(node))
            && (ts.getCombinedModifierFlags(node) & ts.ModifierFlags.Ambient)) return;

        // A function runs when it is called - except one called right where it is written, which runs right then.
        if (ts.isFunctionLike(node) && !ts.isClassStaticBlockDeclaration(node) && !isImmediatelyInvoked(ts, node)) loadTime = false;
        else if (ts.isPropertyDeclaration(node) && !isStatic(node)) loadTime = false;

        if (loadTime) {
            if (ts.isCallExpression(node) || ts.isTaggedTemplateExpression(node)) {
                flag(node);
                const listener = ts.isCallExpression(node) ? describeListener(ts, sf, node) : null;
                if (listener) listeners.push(listener);
                recordRun(calleeName(ts, ts.isCallExpression(node) ? node.expression : node.tag));
            } else if (ts.isNewExpression(node)) {
                // A constructor runs as much as a function does; a plain container of the platform's has nothing to say.
                if (!isPureConstructor(ts, checker, node)) flag(node);
                recordRun(calleeName(ts, node.expression));
            } else if (isAssignment(ts, node) && assignsToThePlatform(ts, checker, node.left)) {
                // window.onresize = ..., document.onkeydown = ...: a handler set on the page, as much as one added to it.
                flag(node);
            }
        }

        if (ts.isIdentifier(node)) record(node, loadTime);

        ts.forEachChild(node, child => visit(child, loadTime));
    };

    ts.forEachChild(sf, child => visit(child, true));

    deps.delete(self.name);
    loadDeps.delete(self.name);

    return { deps, loadDeps, externals, loadTimeCalls, listeners, globals };
}

// Whether a declaration stands at the top level of its file, outside the namespace: a global of the page in the bundle,
// which every script of it can read, but a binding of its own module as a lazy script, which no other script can.
function isTopLevel(ts, declaration) {
    for (let node = declaration.parent; node; node = node.parent) {
        if (ts.isModuleDeclaration(node)) return false;
        if (ts.isSourceFile(node)) return true;
    }
    return false;
}

// The symbol of a value a name refers to, through an import alias; null for a type, a namespace (a container several
// files add to) or a name the compiler declares nowhere.
function resolveValueSymbol(ts, checker, node) {
    if (!node || !ts.isIdentifier(node)) return null;
    let symbol = ts.isShorthandPropertyAssignment(node.parent) && node.parent.name === node
        ? checker.getShorthandAssignmentValueSymbol(node.parent)
        : checker.getSymbolAtLocation(node);
    if (!symbol) return null;
    if (symbol.flags & ts.SymbolFlags.Alias) symbol = checker.getAliasedSymbol(symbol);
    if (symbol.flags & (ts.SymbolFlags.Module | ts.SymbolFlags.TypeAlias | ts.SymbolFlags.Interface | ts.SymbolFlags.TypeParameter)) return null;
    if (!symbol.declarations?.length) return null;
    return symbol;
}

// The files of the package whose code runs when the given declaration runs: everything its body uses, and through every
// function or class of the package it names, what those use in turn - more than a call may reach, never less.
function filesRunBy(ts, checker, declaration, bySourceFile, cache) {
    if (cache.has(declaration)) return cache.get(declaration);

    const files = new Set();
    const seen = new Set();
    const pending = [declaration];

    while (pending.length) {
        const current = pending.pop();
        if (seen.has(current)) continue;
        seen.add(current);

        const file = current.getSourceFile();
        const owner = bySourceFile.get(file);
        if (!owner || !emitsCode(ts, current)) continue;
        files.add(owner.name);

        const code = codeRunBy(ts, current);
        if (!code) continue;

        const walk = node => {
            if (ts.isExpressionWithTypeArguments(node) && isClassExtends(ts, node)) return walk(node.expression);
            if (ts.isTypeNode(node) || ts.isInterfaceDeclaration(node) || ts.isTypeAliasDeclaration(node)) return;
            if (ts.isIdentifier(node)) {
                const symbol = resolveValueSymbol(ts, checker, node);
                for (const d of symbol?.declarations ?? []) {
                    if (!bySourceFile.has(d.getSourceFile()) || !emitsCode(ts, d)) continue;
                    files.add(bySourceFile.get(d.getSourceFile()).name);
                    if (codeRunBy(ts, d)) pending.push(d);
                }
            }
            ts.forEachChild(node, walk);
        };
        walk(code);
    }

    cache.set(declaration, files);
    return files;
}

// The listeners a declaration adds to the page when it is called or constructed: the addEventListener calls its code makes
// as it runs - not one inside a function of its own, a handler that runs later - and those of what it calls in turn.
function listenersRunBy(ts, checker, declaration, bySourceFile, cache, active) {
    if (cache.has(declaration)) return cache.get(declaration);
    if (active.has(declaration) || !bySourceFile.has(declaration.getSourceFile()) || !emitsCode(ts, declaration)) return [];

    const code = codeRunBy(ts, declaration);
    if (!code) return [];

    // A class runs its constructor and its fields' initializers when constructed; its methods wait to be called.
    const roots = ts.isClassLike(code)
        ? code.members.flatMap(m => ts.isConstructorDeclaration(m) && m.body ? [m.body] : ts.isPropertyDeclaration(m) && m.initializer && !(ts.getCombinedModifierFlags(m) & ts.ModifierFlags.Static) ? [m.initializer] : [])
        : [ts.isFunctionLike(code) ? code.body : code].filter(Boolean);

    active.add(declaration);
    const sf = declaration.getSourceFile();
    const found = [];
    const run = callee => {
        const symbol = resolveValueSymbol(ts, checker, callee);
        for (const d of symbol?.declarations ?? []) found.push(...listenersRunBy(ts, checker, d, bySourceFile, cache, active));
    };
    const walk = node => {
        if (ts.isFunctionLike(node) && !isImmediatelyInvoked(ts, node)) return;
        if (ts.isClassLike(node)) return;
        if (ts.isCallExpression(node)) {
            const listener = describeListener(ts, sf, node);
            if (listener) found.push(listener);
            run(calleeName(ts, node.expression));
        } else if (ts.isNewExpression(node)) {
            run(calleeName(ts, node.expression));
        }
        ts.forEachChild(node, walk);
    };
    for (const root of roots) walk(root);
    active.delete(declaration);

    cache.set(declaration, found);
    return found;
}

// The code that runs when a declaration is called or constructed: a function's or a method's own, a variable's function,
// a class's (its constructor, its field initializers, its base class). Null for a declaration that runs nothing.
function codeRunBy(ts, declaration) {
    if (ts.isFunctionDeclaration(declaration) || ts.isMethodDeclaration(declaration) || ts.isConstructorDeclaration(declaration)
        || ts.isGetAccessorDeclaration(declaration) || ts.isSetAccessorDeclaration(declaration)
        || ts.isFunctionExpression(declaration) || ts.isArrowFunction(declaration)) return declaration.body ?? null;
    if (ts.isClassDeclaration(declaration) || ts.isClassExpression(declaration)) return declaration;
    if ((ts.isVariableDeclaration(declaration) || ts.isPropertyDeclaration(declaration) || ts.isPropertyAssignment(declaration)) && declaration.initializer) {
        let init = declaration.initializer;
        while (ts.isParenthesizedExpression(init)) init = init.expression;
        if (ts.isFunctionExpression(init) || ts.isArrowFunction(init) || ts.isClassExpression(init)) return init;
    }
    return null;
}

// Whether a declaration is code the script runs, rather than a type a reference is checked against: the members of an
// interface or a type literal, and what a 'declare' says exists elsewhere (a vendored library, the platform), emit nothing.
function emitsCode(ts, declaration) {
    if (ts.isMethodSignature(declaration) || ts.isPropertySignature(declaration) || ts.isCallSignatureDeclaration(declaration)
        || ts.isConstructSignatureDeclaration(declaration) || ts.isIndexSignatureDeclaration(declaration)) return false;
    return (ts.getCombinedModifierFlags(declaration) & ts.ModifierFlags.Ambient) === 0;
}

function isClassExtends(ts, node) {
    const clause = node.parent;
    return ts.isHeritageClause(clause) && clause.token === ts.SyntaxKind.ExtendsKeyword && !ts.isInterfaceDeclaration(clause.parent);
}

// (function () { ... })(), (() => { ... })(), and the same through .call(this) or .apply(this, ...).
function isImmediatelyInvoked(ts, node) {
    if (!ts.isFunctionExpression(node) && !ts.isArrowFunction(node)) return false;

    let callee = node;
    while (ts.isParenthesizedExpression(callee.parent)) callee = callee.parent;
    const parent = callee.parent;

    if (ts.isCallExpression(parent) && parent.expression === callee) return true;
    return ts.isPropertyAccessExpression(parent) && parent.expression === callee && (parent.name.text === 'call' || parent.name.text === 'apply')
        && ts.isCallExpression(parent.parent) && parent.parent.expression === parent;
}

// The name a call is made through: f in f(), the member in a.b.f(), the function itself for an immediately invoked one
// (whose body the walk of the file visits as load time already).
function calleeName(ts, expression) {
    while (ts.isParenthesizedExpression(expression)) expression = expression.expression;
    if (ts.isPropertyAccessExpression(expression)) return expression.name;
    return ts.isIdentifier(expression) ? expression : null;
}

// The constructors of the platform that only make a value: a script creating one of these as it loads touches nothing.
const PURE_CONSTRUCTORS = new Set(['Map', 'Set', 'WeakMap', 'WeakSet', 'WeakRef', 'Array', 'Object', 'RegExp', 'Date', 'Error', 'TypeError', 'RangeError']);

function isPureConstructor(ts, checker, node) {
    const name = calleeName(ts, node.expression);
    if (!name || !PURE_CONSTRUCTORS.has(name.text)) return false;
    const symbol = checker.getSymbolAtLocation(name);
    return !!symbol?.declarations?.length && symbol.declarations.every(d => isLibraryDeclaration(d.getSourceFile().fileName));
}

function isAssignment(ts, node) {
    return ts.isBinaryExpression(node) && node.operatorToken.kind >= ts.SyntaxKind.FirstAssignment && node.operatorToken.kind <= ts.SyntaxKind.LastAssignment;
}

// Whether an assignment's target is something of the platform's - a property of window, document, a prototype of the
// DOM, a global - rather than state of the package's own.
function assignsToThePlatform(ts, checker, target) {
    let root = target;
    while (ts.isPropertyAccessExpression(root) || ts.isElementAccessExpression(root) || ts.isParenthesizedExpression(root)) root = root.expression;
    if (!ts.isIdentifier(root)) return false;
    const symbol = checker.getSymbolAtLocation(root);
    if (!symbol?.declarations?.length) return true;
    return symbol.declarations.every(d => isLibraryDeclaration(d.getSourceFile().fileName));
}

// A listener added to the page as the script loads - x.addEventListener('type', ..., true | { capture: true }) - as
// "x type" or "x type capture", the order of listeners on one node being the order they were added in.
function describeListener(ts, sf, call) {
    const callee = call.expression;
    if (!ts.isPropertyAccessExpression(callee) || callee.name.text !== 'addEventListener' || call.arguments.length < 2) return null;

    const [type, , options] = call.arguments;
    const name = ts.isStringLiteralLike(type) ? type.text : type.getText(sf);
    const capture = options !== undefined && (options.kind === ts.SyntaxKind.TrueKeyword
        || (ts.isObjectLiteralExpression(options) && options.properties.some(p => ts.isPropertyAssignment(p) && p.name.getText(sf) === 'capture' && p.initializer.kind === ts.SyntaxKind.TrueKeyword)));

    return `${callee.expression.getText(sf)} ${name}${capture ? ' capture' : ''}`;
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

        chunks.push({ name, import: importPath, source: rel(source), pragma: readPragma(fs.readFileSync(source, 'utf8'), rel(source), STYLE_PRAGMAS) });
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

// What a bit-trim comment can say in each kind of file. A stylesheet is kept for its components alone: the trimming reads
// no markup for one, and with no script running it as it loads there is nothing for 'on-demand' to acknowledge.
const SCRIPT_PRAGMAS = ['always', 'on-demand', 'owners', 'markup'];
const STYLE_PRAGMAS = ['always', 'owners'];

function readPragma(text, file, allowed) {
    const describe = { always: "'always'", 'on-demand': "'on-demand'", owners: "'owners <Type> ...'", markup: "'markup <text> ...'" };
    const expected = allowed.map(k => describe[k]).join(', ').replace(/, ([^,]*)$/, ' or $1');

    let pragma = null;
    for (const match of text.matchAll(/\/\/\s*bit-trim:\s*(.*)$/gm)) {
        const words = match[1].trim().split(/[\s,]+/).filter(Boolean);
        const [kind, ...rest] = words;
        pragma ??= {};
        if (!allowed.includes(kind)) throw new BuildError(`${file}: '// bit-trim: ${match[1].trim()}' is not one of ${expected}.`);
        if (kind === 'always' && rest.length === 0) pragma.always = true;
        else if (kind === 'on-demand' && rest.length === 0) pragma.onDemand = true;
        else if (kind === 'owners' && rest.length) pragma.owners = [...new Set([...(pragma.owners ?? []), ...rest])];
        else if (kind === 'markup' && rest.length) pragma.markup = [...new Set([...(pragma.markup ?? []), ...rest])];
        else throw new BuildError(`${file}: '// bit-trim: ${match[1].trim()}' is not one of ${expected}.`);
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

// A file served as it is written (a module of the lazy scripts), rewritten only when it changed, for the same reason as a
// bundle.
function writeIfChanged(file, text) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    if (fs.existsSync(file) && fs.readFileSync(file, 'utf8') === text) {
        const now = new Date();
        fs.utimesSync(file, now, now);
        return;
    }
    fs.writeFileSync(file, text);
}

function writeJson(file, value, { compact = false } = {}) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, (compact ? JSON.stringify(value) : JSON.stringify(value, null, 2)) + '\n');
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
