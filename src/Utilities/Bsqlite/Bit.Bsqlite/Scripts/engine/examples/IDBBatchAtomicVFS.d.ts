// Types for the vendored wa-sqlite IDBBatchAtomicVFS (engine/examples/IDBBatchAtomicVFS.js, wa-sqlite v1.1.2),
// the VFS the IndexedDB provider registers. The module is fetched and checksummed at restore and served
// unchanged - its imports of the base classes one folder up resolve as written - not compiled here.

export declare class IDBBatchAtomicVFS {
    static create(name: string, module: unknown, options?: { idbName?: string; lockPolicy?: 'exclusive' | 'shared' | 'shared+hint'; lockTimeout?: number }): Promise<IDBBatchAtomicVFS>;
    name: string;
}
