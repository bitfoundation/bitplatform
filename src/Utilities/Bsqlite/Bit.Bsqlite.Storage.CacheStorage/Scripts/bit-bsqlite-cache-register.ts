import { CacheStorageVFS, VFS_NAME } from './bit-bsqlite-cache-vfs.js';
import type { SQLiteAPI, SQLiteModule } from 'bit-bsqlite/engine/sqlite-api.js';

export { VFS_NAME };

/** Called by the worker with the Emscripten module and the wa-sqlite JavaScript API it just loaded. */
export async function register({ module, sqlite3 }: { module: SQLiteModule; sqlite3: SQLiteAPI }): Promise<{ vfsName: string }> {
    const vfs = await CacheStorageVFS.create(VFS_NAME, module);
    sqlite3.vfs_register(vfs, /* makeDefault */ false);
    return { vfsName: VFS_NAME };
}
