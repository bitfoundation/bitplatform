// Types for the vendored wa-sqlite WebLocksMixin (engine/WebLocksMixin.js, wa-sqlite v1.1.2): the mix-in
// that implements SQLite's locking protocol over the Web Locks API, so tabs sharing a database
// coordinate. The module is fetched and checksummed at restore, not compiled here.

import type { FacadeVFS } from './FacadeVFS.js';

export interface WebLocksOptions {
    /** `'exclusive'` (the default), `'shared'` or `'shared+hint'`. */
    lockPolicy?: 'exclusive' | 'shared' | 'shared+hint';
    lockTimeout?: number;
}

type FacadeConstructor = abstract new (name: string, module: unknown, options?: any) => FacadeVFS;

export declare function WebLocksMixin<TBase extends FacadeConstructor>(superclass: TBase):
    TBase & (abstract new (name: string, module: unknown, options?: WebLocksOptions) => FacadeVFS);
