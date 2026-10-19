// Registers wa-sqlite's OPFSCoopSyncVFS against the engine the worker just loaded.
//
// Chosen over AccessHandlePoolVFS because it allows more than one connection and leaves a real
// inspectable file in OPFS - export is a read, not a reconstruction. The VFS itself is vendored
// and import-rewritten; this file is the stable register() the .NET provider points at.

import { OPFSCoopSyncVFS } from './OPFSCoopSyncVFS.js';
import type { SQLiteAPI, SQLiteModule } from 'bit-bsqlite/engine/sqlite-api.js';

export const VFS_NAME = 'opfs-coop-sync';

/** Called by the worker with the Emscripten module and the wa-sqlite JavaScript API it just loaded. */
export async function register({ module, sqlite3 }: { module: SQLiteModule; sqlite3: SQLiteAPI }): Promise<{ vfsName: string }> {
    const vfs = await OPFSCoopSyncVFS.create(VFS_NAME, module);
    sqlite3.vfs_register(vfs, /* makeDefault */ false);
    return { vfsName: VFS_NAME };
}
