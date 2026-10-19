// Types for wa-sqlite's IDBBatchAtomicVFS (src/examples/IDBBatchAtomicVFS.js, wa-sqlite v1.1.2). The module
// is fetched and checksummed at restore and its imports rewritten into wwwroot/IDBBatchAtomicVFS.js (see
// vfs/FetchIndexedDbVfs.targets), next to the module that imports it; it is not compiled here.

export declare class IDBBatchAtomicVFS {
    static create(name: string, module: unknown, options?: { idbName?: string; lockPolicy?: 'exclusive' | 'shared' | 'shared+hint'; lockTimeout?: number }): Promise<IDBBatchAtomicVFS>;
    name: string;
}
