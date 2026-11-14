// Registers wa-sqlite's OPFSCoopSyncVFS against the engine the worker just loaded.
//
// Chosen over AccessHandlePoolVFS because it allows more than one connection and leaves a real
// inspectable file in OPFS - export is a read, not a reconstruction. The VFS itself is vendored
// unchanged under engine/examples; this file is the stable register() the .NET provider points at.

// Imported dynamically so this module's cache key travels with it: see the note in the worker.
const { retryStaleAccessHandles }: typeof import('./bit-bsqlite-opfs-handles.js') =
    await import('./bit-bsqlite-opfs-handles.js' + new URL(import.meta.url).search);
import { OPFSCoopSyncVFS } from './engine/examples/OPFSCoopSyncVFS.js';
import type { SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';
import type { SyncAccessHandleSource } from './bit-bsqlite-opfs-handles.js';

export const VFS_NAME = 'opfs-coop-sync';

/** Called by the worker with the Emscripten module and the wa-sqlite JavaScript API it just loaded. */
export async function register({ module, sqlite3 }: { module: SQLiteModule; sqlite3: SQLiteAPI }): Promise<{ vfsName: string; vfs: { lastError?: unknown } }> {
    // This worker is the engine's alone, so the handle API every OPFS file here goes through can wait out
    // a handle a closed tab still holds - see bit-bsqlite-opfs-handles.ts.
    retryStaleAccessHandles(FileSystemFileHandle.prototype as unknown as SyncAccessHandleSource);

    const vfs = await OPFSCoopSyncVFS.create(VFS_NAME, module);
    // wa-sqlite's VFS base allows 64-byte paths, and SQLite refuses to open a database whose full path - the name
    // with a leading slash - does not fit, with nothing but "sqlite3_open_v2" to say why: a 60-character database
    // name failed outright. The storage under it keys files by the whole name, so the limit is SQLite's own default.
    vfs.mxPathname = 512;
    sqlite3.vfs_register(vfs, /* makeDefault */ false);

    // The VFS keeps the exception behind the I/O error it returned - a full origin, say - which is how the
    // worker tells a quota failure from any other I/O error.
    return { vfsName: VFS_NAME, vfs };
}
