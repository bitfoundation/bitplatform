// Bit.BlazorHeadlessUI - JS initializer.
//
// Blazor automatically loads `{AssemblyName}.lib.module.js` from every Razor
// class library and calls these hooks at startup. Importing the runtime here
// installs the global, declarative behaviours ([data-bit-bh-keys] key handling,
// dismissable layers, interaction attributes) before any component renders,
// so consumers never need a <script> tag.

type RuntimeModule = typeof import('./bit-blazorheadlessui.js');

interface BlazorInstance {
    addEventListener?(name: 'enhancedload', callback: () => void): void;
}

let runtime: Promise<RuntimeModule | undefined> | undefined;

function load() {
    // A failed import is not cached: the next start hook (or the interop's own import) tries again.
    runtime ??= import('./bit-blazorheadlessui.js')
        .then((m) => { m.install(); return m; })
        .catch((error) => { runtime = undefined; console.error('[Bit.BlazorHeadlessUI] failed to load the runtime.', error); return undefined; });
    return runtime;
}

// Blazor Web App (.NET 8+)
export async function afterWebStarted(blazor?: BlazorInstance) {
    const m = await load();
    if (m) blazor?.addEventListener?.('enhancedload', m.onEnhancedLoad);
}

// Blazor Server / WebAssembly standalone
export async function afterStarted() { await load(); }
