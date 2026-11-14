// Registers wa-sqlite's IDBBatchAtomicVFS against the engine the worker just loaded.
//
// This VFS suspends on every I/O, so it cannot run on the synchronous build - the provider
// declares AsyncCapable and the loader picks JSPI or Asyncify. cache_size is set on open so
// batch-atomic writes can keep the journal in cache, which is the main IndexedDB performance win.

import { IDBBatchAtomicVFS } from './engine/examples/IDBBatchAtomicVFS.js';
import type { SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';

export const VFS_NAME = 'idb-batch-atomic';
export const IDB_NAME = 'idb-batch-atomic';

/** Called by the worker with the Emscripten module and the wa-sqlite JavaScript API it just loaded. */
export async function register({ module, sqlite3 }: { module: SQLiteModule; sqlite3: SQLiteAPI }): Promise<{ vfsName: string; vfs: { lastError?: unknown }; onOpenSql: string[] }> {
    const vfs = await IDBBatchAtomicVFS.create(VFS_NAME, module, { idbName: IDB_NAME });
    // wa-sqlite's VFS base allows 64-byte paths, and SQLite refuses to open a database whose full path - the name
    // with a leading slash - does not fit, with nothing but "sqlite3_open_v2" to say why: a 60-character database
    // name failed outright. The storage under it keys files by the whole name, so the limit is SQLite's own default.
    vfs.mxPathname = 512;
    sqlite3.vfs_register(vfs, /* makeDefault */ false);
    return {
        vfsName: VFS_NAME,
        // The VFS keeps the exception behind the I/O error it returned - a full origin, say - which is how
        // the worker tells a quota failure from any other I/O error.
        vfs,
        // Negative cache_size is kibibytes. 8 MiB holds a typical EF SaveChanges working set so
        // SQLITE_IOCAP_BATCH_ATOMIC can skip the external journal. The VFS still works if a huge
        // transaction overflows it; it just pays the journal path.
        onOpenSql: ['PRAGMA cache_size=-8000'],
    };
}
