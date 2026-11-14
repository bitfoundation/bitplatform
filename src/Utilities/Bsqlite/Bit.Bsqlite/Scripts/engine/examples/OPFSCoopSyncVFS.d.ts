// Types for the vendored wa-sqlite OPFSCoopSyncVFS (engine/examples/OPFSCoopSyncVFS.js, wa-sqlite v1.1.2), the
// VFS the OPFS provider registers. The module is fetched and checksummed at restore and served unchanged - its
// imports of the base classes one folder up resolve as written - not compiled here.

export declare class OPFSCoopSyncVFS {
    static create(name: string, module: unknown): Promise<OPFSCoopSyncVFS>;
    name: string;
    /** The longest full path SQLite may ask the VFS for; read when the VFS is registered. */
    mxPathname: number;
    /** The exception behind the last error result a method returned, or null. */
    lastError: unknown;
}
