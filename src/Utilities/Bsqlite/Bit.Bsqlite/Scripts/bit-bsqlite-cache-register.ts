import { CacheStorageVFS, VFS_NAME } from './bit-bsqlite-cache-vfs.js';
import type { SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';

export { VFS_NAME };

/** Called by the worker with the Emscripten module and the wa-sqlite JavaScript API it just loaded. */
export async function register({ module, sqlite3 }: { module: SQLiteModule; sqlite3: SQLiteAPI }): Promise<{ vfsName: string }> {
    const vfs = await CacheStorageVFS.create(VFS_NAME, module);
    // wa-sqlite's VFS base allows 64-byte paths, and SQLite refuses to open a database whose full path - the name
    // with a leading slash - does not fit, with nothing but "sqlite3_open_v2" to say why: a 60-character database
    // name failed outright. The storage under it keys files by the whole name, so the limit is SQLite's own default.
    vfs.mxPathname = 512;
    sqlite3.vfs_register(vfs, /* makeDefault */ false);
    return { vfsName: VFS_NAME };
}
