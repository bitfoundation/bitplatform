// Chooses a SQLite build, loads it, and lets the selected storage provider register its VFS.
//
// Three builds are vendored and exactly one is fetched per session. The choice is not a preference
// setting: a VFS that suspends to read storage (IndexedDB, Cache Storage) cannot run on the
// synchronous build at all, and the two builds that can host it differ by more than a factor of two
// in download size, so the cheapest capable build wins.

// Imported dynamically so this module's cache key travels with it: see the note in the worker.
const { raiseFunctionTableLimit }: typeof import('./bit-bsqlite-wasm-table.js') =
    await import('./bit-bsqlite-wasm-table.js' + new URL(import.meta.url).search);
import * as SQLite from './engine/sqlite-api.js';
import type { SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';
import type { JsModuleReference } from './bit-bsqlite-host.js';

/** What a storage provider's VFS module hands back from its register export. */
export interface VfsRegistration {
    vfsName?: string | null;
    onOpenSql?: string[];
}

/** A loaded engine, with the provider's VFS registered against it. */
export interface LoadedEngine {
    sqlite3: SQLiteAPI;
    module: SQLiteModule;
    build: string;
    vfsName: string | null;
    onOpenSql: string[];
}

interface EngineBuildFiles {
    id: string;
    module: string;
    wasm: string;
}

/** Which engine build a VFS needs. Mirrors BsqliteEngineBuild on the .NET side. */
export const EngineBuild = Object.freeze({
    synchronous: 'synchronous',
    asyncCapable: 'asyncCapable',
});

const BUILDS = Object.freeze({
    synchronous: { id: 'synchronous', module: './engine/wa-sqlite.mjs', wasm: './engine/wa-sqlite.wasm' },
    jspi: { id: 'jspi', module: './engine/wa-sqlite-jspi.mjs', wasm: './engine/wa-sqlite-jspi.wasm' },
    asyncify: { id: 'asyncify', module: './engine/wa-sqlite-async.mjs', wasm: './engine/wa-sqlite-async.wasm' },
});

/**
 * Whether the browser has JavaScript Promise Integration, which lets the engine suspend natively
 * instead of through Asyncify's instrumented control flow.
 *
 * Unflagged in Chrome 137+, flagged in Firefox, and absent in Safari as of mid-2026, which is why
 * Asyncify is still vendored: it is the only route to an asynchronous VFS on Safari.
 */
export function supportsJspi(): boolean {
    // Not in TypeScript's WebAssembly typings yet: JSPI is still being standardised.
    return typeof (WebAssembly as { Suspending?: unknown })?.Suspending === 'function';
}

/** Picks the smallest build that can host the given VFS requirement, a value of {@link EngineBuild}. */
export function selectBuild(requiredBuild: string): EngineBuildFiles {
    switch (requiredBuild) {
        case EngineBuild.synchronous:
            return BUILDS.synchronous;

        case EngineBuild.asyncCapable:
            return supportsJspi() ? BUILDS.jspi : BUILDS.asyncify;

        default:
            throw new Error(`Unknown engine build requirement '${requiredBuild}'.`);
    }
}

/**
 * Loads the engine and registers the storage provider's VFS against it.
 *
 * `requiredBuild` is a value of {@link EngineBuild} and `databaseName` the database the VFS will be
 * asked to open. `vfs` is the provider's VFS module - omitted for a provider that has none, which
 * leaves the engine on its built-in memory VFS.
 */
export async function loadEngine({ requiredBuild, databaseName, vfs }: { requiredBuild: string; databaseName: string; vfs?: JsModuleReference | null }): Promise<LoadedEngine> {
    if (!databaseName) {
        throw new Error('A database name is required.');
    }

    const build = selectBuild(requiredBuild);

    if (vfs && build.id === BUILDS.synchronous.id && requiredBuild !== EngineBuild.synchronous) {
        // Unreachable via selectBuild, but this is the mismatch that produces
        // "Synchronous WebAssembly cannot call async function" deep inside a query rather than here.
        throw new Error('An asynchronous VFS cannot run on the synchronous engine build.');
    }

    const factory = (await import(build.module)).default;
    const wasmBinary = raiseFunctionTableLimit(
        new Uint8Array(await (await fetch(new URL(build.wasm, import.meta.url))).arrayBuffer()));
    const module: SQLiteModule = await factory({ wasmBinary });
    const sqlite3 = SQLite.Factory(module);

    let vfsName: string | null = null;
    let onOpenSql: string[] = [];

    if (vfs) {
        const vfsModule = await import(vfs.moduleUrl);
        const register = vfsModule[vfs.registerExport ?? 'register'];

        if (typeof register !== 'function') {
            throw new Error(
                `'${vfs.moduleUrl}' does not export a function named '${vfs.registerExport ?? 'register'}'.`);
        }

        // The provider both creates and registers: VFS construction is asynchronous and
        // implementation-specific (handle pools, IndexedDB connections), so the engine host cannot do it.
        const registered: VfsRegistration | undefined = await register({ module, sqlite3, databaseName });
        vfsName = registered?.vfsName ?? null;
        onOpenSql = Array.isArray(registered?.onOpenSql) ? registered.onOpenSql : [];

        if (!vfsName) {
            throw new Error(
                `'${vfs.moduleUrl}' registered a VFS without reporting its name, so the connection cannot `
                + 'ask for it.');
        }
    }

    return { sqlite3, module, build: build.id, vfsName, onOpenSql };
}
