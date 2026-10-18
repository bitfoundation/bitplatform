// Types for the vendored wa-sqlite VFS base class (engine/VFS.js, wa-sqlite v1.1.2), which every storage
// provider's VFS ultimately extends. The module is fetched and checksummed at restore, not compiled here.

export * from './sqlite-constants.js';

/** The raw VFS interface: SQLite's C callbacks, with C primitives for arguments. */
export declare class Base {
    constructor(name: string, module: unknown);
    name: string;
    mxPathname: number;
    close(): void | Promise<void>;
    isReady(): boolean | Promise<boolean | void>;
    hasAsyncMethod(methodName: string): boolean;
}

export declare const FILE_TYPE_MASK: number;
