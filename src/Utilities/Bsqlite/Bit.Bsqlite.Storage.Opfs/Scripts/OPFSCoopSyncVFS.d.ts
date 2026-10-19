// Types for wa-sqlite's OPFSCoopSyncVFS (src/examples/OPFSCoopSyncVFS.js, wa-sqlite v1.1.2). The module is
// fetched and checksummed at restore and its imports rewritten into wwwroot/OPFSCoopSyncVFS.js (see
// vfs/FetchOpfsVfs.targets), next to the module that imports it; it is not compiled here.

export declare class OPFSCoopSyncVFS {
    static create(name: string, module: unknown): Promise<OPFSCoopSyncVFS>;
    name: string;
}
