// IndexedDB as a bag of SQLite file images, without the engine.
//
// The VFS stores each database as metadata + page-sized blocks under one origin-wide IndexedDB
// (`idb-batch-atomic`). Admin uses that same database and the same path mapping
// (`new URL(name, 'file://').pathname`) so an imported image is what `open_v2` will see.
// Related files (-journal, -wal) are not databases.

export const IDB_NAME = 'idb-batch-atomic';
export const IDB_VERSION = 6;
const RELATED_SUFFIXES = ['-journal', '-wal'];
const PAGE_SIZE = 4096;

/** What `probe` reports to the .NET provider, which turns it into a BsqliteProbeResult. */
interface ProbeResult {
    available: boolean;
    reason: string | null;
    quotaBytes: number | null;
    usageBytes: number | null;
    environment: Record<string, string>;
}

/**
 * A row of the VFS's `metadata` store: one per file.
 *
 * `version` counts down: each write transaction writes its blocks one below the committed version
 * and lowers it on commit, so a block below `version` belongs to a transaction that never committed -
 * one in flight in another tab, or one whose tab died, which `pendingVersion` marks until the VFS
 * cleans it up on the next shared lock.
 */
interface MetadataRow {
    name: string;
    fileSize: number;
    version: number;
    pendingVersion?: number;
}

/** A row of the VFS's `blocks` store. Offsets are stored negated, so a key range finds a block's start. */
interface BlockRow {
    path: string;
    offset: number;
    version: number;
    data: Uint8Array;
}

interface Stores {
    metadata: IDBObjectStore;
    blocks: IDBObjectStore;
}

export function pathOf(databaseName: string): string {
    return new URL(databaseName, 'file://').pathname;
}

export function displayName(path: string): string {
    return path.startsWith('/') ? path.slice(1) : path;
}

export async function probe(): Promise<ProbeResult> {
    const environment = {
        hasIndexedDB: String(typeof indexedDB !== 'undefined'),
        secureContext: String(Boolean(globalThis.isSecureContext)),
        supportsJspi: String(typeof (WebAssembly as { Suspending?: unknown })?.Suspending === 'function'),
    };

    if (typeof indexedDB === 'undefined') {
        return unavailable(
            'This browser does not expose indexedDB, which this backend needs.',
            environment);
    }

    try {
        // Opening and immediately closing proves the API is actually usable, not merely present.
        const db = await openDatabase();
        db.close();

        let quotaBytes: number | null = null;
        let usageBytes: number | null = null;
        if (typeof navigator.storage?.estimate === 'function') {
            const estimate = await navigator.storage.estimate();
            quotaBytes = estimate.quota ?? null;
            usageBytes = estimate.usage ?? null;
        }

        return {
            available: true,
            reason: null,
            quotaBytes,
            usageBytes,
            environment,
        };
    } catch (error) {
        return unavailable((error as Error | undefined)?.message ?? String(error), environment);
    }
}

export async function exists(databaseName: string): Promise<boolean> {
    const path = pathOf(requireName(databaseName));
    const db = await openDatabase();

    try {
        const meta = await withStore(db, 'metadata', 'readonly', store => requestOf(store.get(path)));
        return Boolean(meta);
    } finally {
        db.close();
    }
}

export async function list(): Promise<string[]> {
    const db = await openDatabase();

    try {
        const rows = await withStore(db, 'metadata', 'readonly', store => requestOf<MetadataRow[]>(store.getAll()));
        return rows
            .map(row => row.name)
            .filter(name => !RELATED_SUFFIXES.some(suffix => name.endsWith(suffix)))
            .map(displayName)
            .sort();
    } finally {
        db.close();
    }
}

export async function deleteDatabase(databaseName: string): Promise<void> {
    const name = requireName(databaseName);
    const db = await openDatabase();

    try {
        await withTransaction(db, 'readwrite', async ({ metadata, blocks }) => {
            for (const suffix of ['', ...RELATED_SUFFIXES]) {
                const path = pathOf(name + suffix);
                metadata.delete(path);
                blocks.delete(IDBKeyRange.bound([path, -Infinity], [path, Infinity]));
            }
        });
    } finally {
        db.close();
    }
}

/**
 * Reads out the committed database, as of one moment.
 *
 * One read-only transaction covers the metadata and every block, so a commit in another tab cannot
 * land between them. Blocks below the committed version are skipped (see MetadataRow): exporting
 * them would mix a transaction that never committed into an image that still passes for a SQLite
 * file - and a migration would bind the user's database to it.
 */
export async function exportDatabase(databaseName: string): Promise<Uint8Array> {
    const path = pathOf(requireName(databaseName));
    const db = await openDatabase();

    try {
        return await withTransaction(db, 'readonly', async ({ metadata, blocks }) => {
            const meta = await requestOf<MetadataRow | undefined>(metadata.get(path));
            if (!meta) {
                throw new Error(`IndexedDB holds no database named '${displayName(path)}'.`);
            }

            return readCommittedImage(blocks, path, meta);
        });
    } finally {
        db.close();
    }
}

/**
 * Walks the blocks the way the VFS reads them - for each offset, the newest block that starts at or
 * below it - passing over any whose version is below the committed one.
 */
function readCommittedImage(blocks: IDBObjectStore, path: string, meta: MetadataRow): Promise<Uint8Array> {
    const image = new Uint8Array(meta.fileSize ?? 0);

    return new Promise<Uint8Array>((resolve, reject) => {
        let offset = 0;

        const seek = () => {
            if (offset >= image.byteLength) {
                resolve(image);
                return;
            }

            // Callbacks rather than awaits, so each request is issued while the transaction is
            // certainly still active.
            const request = blocks.openCursor(IDBKeyRange.bound([path, -offset], [path, Infinity]));
            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const cursor = request.result;
                if (!cursor) {
                    resolve(image);
                    return;
                }

                const block = cursor.value as BlockRow;
                if (block.version < meta.version) {
                    cursor.continue();
                    return;
                }

                if (block.data.byteLength - block.offset <= offset) {
                    resolve(image);
                    return;
                }

                const srcOffset = offset + block.offset;
                const n = Math.min(block.data.byteLength - srcOffset, image.byteLength - offset);
                image.set(block.data.subarray(srcOffset, srcOffset + n), offset);
                offset += n;
                seek();
            };
        };

        seek();
    });
}

export async function importDatabase(databaseName: string, contents: Uint8Array | ArrayLike<number> | null): Promise<void> {
    const name = requireName(databaseName);
    const path = pathOf(name);
    const bytes = contents instanceof Uint8Array ? contents : new Uint8Array(contents ?? []);
    const db = await openDatabase();

    try {
        await withTransaction(db, 'readwrite', async ({ metadata, blocks }) => {
            for (const suffix of ['', ...RELATED_SUFFIXES]) {
                const related = pathOf(name + suffix);
                metadata.delete(related);
                blocks.delete(IDBKeyRange.bound([related, -Infinity], [related, Infinity]));
            }

            metadata.put({ name: path, fileSize: bytes.byteLength, version: 0 });

            for (let offset = 0; offset < bytes.byteLength; offset += PAGE_SIZE) {
                const data = bytes.subarray(offset, offset + PAGE_SIZE).slice();
                blocks.put({ path, offset: -offset, version: 0, data });
            }
        });
    } finally {
        db.close();
    }
}

function unavailable(reason: string, environment: Record<string, string>): ProbeResult {
    return { available: false, reason, quotaBytes: null, usageBytes: null, environment };
}

function requireName(databaseName: unknown): string {
    if (typeof databaseName !== 'string' || databaseName.trim() === '') {
        throw new Error('A database name is required.');
    }

    const name = databaseName.trim();
    if (name.includes('..') || name.startsWith('\\')) {
        throw new Error(`'${name}' is not a valid IndexedDB database name.`);
    }

    return name;
}

function openDatabase(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(IDB_NAME, IDB_VERSION);

        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains('blocks')) {
                db.createObjectStore('blocks', { keyPath: ['path', 'offset', 'version'] });
            }

            if (!db.objectStoreNames.contains('metadata')) {
                db.createObjectStore('metadata', { keyPath: 'name' });
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function withStore<T>(db: IDBDatabase, storeName: keyof Stores, mode: IDBTransactionMode, work: (store: IDBObjectStore) => T | Promise<T>): Promise<T> {
    return withTransaction(db, mode, stores => work(stores[storeName]));
}

function withTransaction<T>(db: IDBDatabase, mode: IDBTransactionMode, work: (stores: Stores) => T | Promise<T>): Promise<T> {
    return new Promise<T>((resolve, reject) => {
        const tx = db.transaction(['metadata', 'blocks'], mode);
        const stores: Stores = {
            metadata: tx.objectStore('metadata'),
            blocks: tx.objectStore('blocks'),
        };

        // Handlers first: a transaction of only synchronous puts can complete before a then() runs.
        let result: T;
        tx.oncomplete = () => resolve(result);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));

        try {
            Promise.resolve(work(stores)).then(value => {
                result = value;
            }, reject);
        } catch (error) {
            reject(error);
        }
    });
}

function requestOf<T>(request: IDBRequest): Promise<T> {
    return new Promise<T>((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}
