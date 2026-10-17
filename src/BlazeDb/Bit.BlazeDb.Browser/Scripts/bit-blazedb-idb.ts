// Bit.BlazeDb IndexedDB storage module.
// Fallback for contexts where OPFS is unavailable - notably Firefox private windows and older
// Safari. Each BlazeDb "file" is one record in a single object store keyed by file name.
//
// IndexedDB has no append primitive, so a WAL append is a read-modify-write of the record. That
// is markedly slower than OPFS for a large WAL, which is exactly why this is the fallback rather
// than the default. Checkpointing keeps the WAL small, which keeps appends cheap.

const STORE = 'files';

export async function openDatabase(databaseName: string): Promise<IDBDatabase> {
    return await new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open('blazedb:' + databaseName, 1);
        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains(STORE)) {
                db.createObjectStore(STORE);
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new Error('IndexedDB upgrade blocked by another tab.'));
    });
}

// Opens the database only if it already exists; resolves null otherwise. indexedDB.open() always
// creates, so a first-time open is recognized by the upgrade it triggers and abandoned there - the
// aborted upgrade leaves nothing behind. A replica tab uses this so it waits for the writer instead
// of materializing an empty database of its own.
export async function openExistingDatabase(databaseName: string): Promise<IDBDatabase | null> {
    return await new Promise<IDBDatabase | null>((resolve, reject) => {
        const request = indexedDB.open('blazedb:' + databaseName);
        let created = false;
        request.onupgradeneeded = (event) => {
            if (event.oldVersion === 0) {
                created = true;
                request.transaction?.abort();
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = (event) => {
            if (created) {
                event.preventDefault();
                resolve(null);
                return;
            }
            reject(request.error);
        };
        request.onblocked = () => reject(new Error('IndexedDB upgrade blocked by another tab.'));
    });
}

function run<T>(db: IDBDatabase, mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>): Promise<T | undefined> {
    return new Promise<T | undefined>((resolve, reject) => {
        const tx = db.transaction(STORE, mode);
        const store = tx.objectStore(STORE);
        let result: IDBRequest<T> | undefined;
        try {
            result = action(store);
        } catch (error) {
            reject(error);
            return;
        }
        // Resolve on transaction completion, not on request success: for writes that is the point at
        // which the data is durable, which is what the engine's flush contract promises.
        tx.oncomplete = () => resolve(result ? result.result : undefined);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));
    });
}

export async function readFile(db: IDBDatabase, name: string): Promise<Uint8Array | null> {
    const value = await run<ArrayBuffer | ArrayBufferView | undefined>(db, 'readonly', (store) => store.get(name));
    return value === undefined ? null : toBytes(value);
}

export function copyBytes(source: Uint8Array, destination: DotNetMemoryView): void {
    destination.set(source);
}

export async function writeAtomic(db: IDBDatabase, name: string, bytes: Uint8Array): Promise<void> {
    // A single IndexedDB transaction is atomic and durable on commit, so this needs no swap file.
    await run(db, 'readwrite', (store) => store.put(bytes, name));
}

export async function appendFile(db: IDBDatabase, name: string, bytes: Uint8Array): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        const tx = db.transaction(STORE, 'readwrite');
        const store = tx.objectStore(STORE);
        const existing = store.get(name);
        existing.onsuccess = () => {
            const current = existing.result as ArrayBuffer | ArrayBufferView | undefined;
            if (current === undefined) {
                store.put(bytes, name);
                return;
            }
            const previous = toBytes(current);
            const merged = new Uint8Array(previous.byteLength + bytes.byteLength);
            merged.set(previous, 0);
            merged.set(bytes, previous.byteLength);
            store.put(merged, name);
        };
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));
    });
}

export async function deleteFile(db: IDBDatabase, name: string): Promise<void> {
    await run(db, 'readwrite', (store) => store.delete(name));
}

export function closeDatabase(db: IDBDatabase): void {
    db.close();
}

// True when the Origin Private File System is usable *for writing* from this context, so callers
// can pick a backend. Reading (getDirectory, getFile) has been in every engine for years; the
// main-thread write API, FileSystemFileHandle.createWritable(), arrived much later (Safari 26).
// A browser that can read OPFS but not write to it would open the database and then fail every
// flush, so the probe asks about the write API, not just the directory.
export async function isOpfsAvailable(): Promise<boolean> {
    try {
        if (!navigator.storage || !navigator.storage.getDirectory) {
            return false;
        }
        if (typeof FileSystemFileHandle === 'undefined' ||
            typeof FileSystemFileHandle.prototype.createWritable !== 'function') {
            return false;
        }
        await navigator.storage.getDirectory();
        return true;
    } catch {
        return false;
    }
}

// A record is stored as the Uint8Array it was put as, and structured cloning hands it back as one;
// older records may come back as a bare ArrayBuffer. Either way the bytes are what is wanted.
function toBytes(value: ArrayBuffer | ArrayBufferView): Uint8Array {
    return value instanceof ArrayBuffer
        ? new Uint8Array(value)
        : new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
}
