// Bit.BlazeDb OPFS storage module.
// Files live in the Origin Private File System under a per-database directory.
// createWritable() commits atomically on close() (the browser writes to a swap file),
// which gives BlazeDb its atomic manifest/snapshot writes for free.

interface HeldLock {
    release: () => void;
    clientId: string | null;
}

// Locks this document holds: name -> { release, clientId }. clientId is what navigator.locks.query()
// reports for the holder, recorded at acquisition so a later query can tell "we still hold it" from
// "another tab holds it now".
const heldLocks = new Map<string, HeldLock>();
// Locks this document believed it held but can no longer vouch for. See revalidateLocks.
const lostLocks = new Set<string>();

export function hasWebLocks(): boolean {
    return !!navigator.locks;
}

// Resolves true when the lock was taken, false when another tab holds it. Without a Web Locks API
// there is no election to win: the caller has to say explicitly that a single tab is assumed, or
// the open fails - quietly proceeding would let two tabs on an old browser write the same files.
export async function acquireLock(lockName: string, allowWithoutWebLocks: boolean): Promise<boolean> {
    if (!navigator.locks) {
        if (!allowWithoutWebLocks) {
            return false;
        }
        heldLocks.set(lockName, { release: () => { }, clientId: null });
        return true;
    }
    return await requestLock(lockName);
}

async function requestLock(lockName: string): Promise<boolean> {
    const granted = await new Promise<boolean>((resolve) => {
        navigator.locks.request(lockName, { ifAvailable: true }, (lock) => {
            if (lock === null) {
                resolve(false);
                return null;
            }
            // Hold the lock until releaseLock is called (or the document goes away).
            const holding = new Promise<void>((release) => {
                heldLocks.set(lockName, { release, clientId: null });
            });
            resolve(true);
            return holding;
        });
    });
    if (granted) {
        lostLocks.delete(lockName);
        const entry = heldLocks.get(lockName);
        if (entry) {
            entry.clientId = await holderOf(lockName);
        }
    }
    return granted;
}

async function holderOf(lockName: string): Promise<string | null> {
    if (!navigator.locks.query) {
        return null;
    }
    try {
        const state = await navigator.locks.query();
        const held = (state.held ?? []).find((l) => l.name === lockName);
        return held?.clientId ?? null;
    } catch {
        return null;
    }
}

export function releaseLock(lockName: string): void {
    lostLocks.delete(lockName);
    const entry = heldLocks.get(lockName);
    if (entry) {
        heldLocks.delete(lockName);
        entry.release();
    }
}

// Whether the document can still vouch for holding the lock. Checked before every write: data
// written after the lock was lost could interleave with another tab's. Synchronous on purpose, so
// the check costs the engine nothing measurable per flush.
export function isLockHeld(lockName: string): boolean {
    return heldLocks.has(lockName) && !lostLocks.has(lockName);
}

// A document restored from the back/forward cache may have had its locks released while it was
// frozen, and another tab may have taken over in the meantime. Ask the browser who holds each lock
// now; where it is not us, try to take it back, and where that fails mark it lost so writes stop
// instead of colliding with the new holder.
//
// The check needs navigator.locks.query() and the clientId recorded when the lock was taken;
// without either there is no way to tell "still ours" from "someone else's", and guessing wrong in
// the pessimistic direction is not harmless: dropping our own entry and re-requesting a lock this
// document still holds fails (ifAvailable sees it taken - by us), which would mark a perfectly good
// lock lost and leave it unreleasable for the life of the page. So a lock that cannot be checked
// is left as it is.
async function revalidateLocks(): Promise<void> {
    if (!navigator.locks || !navigator.locks.query || heldLocks.size === 0) {
        return;
    }
    for (const [lockName, entry] of [...heldLocks]) {
        if (entry.clientId === null) {
            continue; // Taken without an id to compare against; cannot be checked, so not second-guessed.
        }
        let state: LockManagerSnapshot;
        try {
            state = await navigator.locks.query();
        } catch {
            continue; // The browser would not say; same as above.
        }
        const held = (state.held ?? []).find((l) => l.name === lockName);
        if (held && held.clientId === entry.clientId) {
            continue; // Still ours.
        }
        if (!held) {
            // Nobody holds it: the browser let go of ours. Take it again, and only give the old entry
            // up once the new one is in place.
            const previous = heldLocks.get(lockName);
            heldLocks.delete(lockName);
            if (await requestLock(lockName)) {
                continue;
            }
            if (previous) {
                heldLocks.set(lockName, previous);
            }
        }
        lostLocks.add(lockName);
    }
}

if (typeof window !== 'undefined') {
    window.addEventListener('pageshow', (event) => {
        if (event.persisted) {
            void revalidateLocks();
        }
    });
    document.addEventListener('resume', () => {
        void revalidateLocks();
    });
}

// Whether this context can write OPFS files. createWritable() is the only write API available on
// the main thread and is newer than the rest of OPFS (Safari gained it in version 26), so a
// writer has to check for it up front rather than discover its absence on the first flush.
export function canWrite(): boolean {
    return typeof FileSystemFileHandle !== 'undefined' &&
        typeof FileSystemFileHandle.prototype.createWritable === 'function';
}

export async function openDatabaseDirectory(databaseName: string): Promise<FileSystemDirectoryHandle> {
    if (!canWrite()) {
        throw new Error(
            'This browser can read the Origin Private File System but cannot write to it from the main ' +
            'thread (FileSystemFileHandle.createWritable is missing). Use BlazeDbIndexedDbStorage instead; ' +
            'BlazeDbIndexedDbStorage.IsOpfsAvailableAsync() reports this.');
    }
    const root = await navigator.storage.getDirectory();
    return await root.getDirectoryHandle(databaseName, { create: true });
}

// Opens an existing database directory without creating it; returns null when the writer tab has
// not created the database yet, so a replica can wait rather than materialize an empty one.
export async function openExistingDatabaseDirectory(databaseName: string): Promise<FileSystemDirectoryHandle | null> {
    try {
        const root = await navigator.storage.getDirectory();
        return await root.getDirectoryHandle(databaseName);
    } catch (error) {
        if (isNotFound(error)) {
            return null;
        }
        throw error;
    }
}

// Returns the file contents as a Uint8Array, or null if the file does not exist.
export async function readFile(directory: FileSystemDirectoryHandle, name: string): Promise<Uint8Array | null> {
    try {
        const handle = await directory.getFileHandle(name);
        const file = await handle.getFile();
        return new Uint8Array(await file.arrayBuffer());
    } catch (error) {
        if (isNotFound(error)) {
            return null;
        }
        throw error;
    }
}

// Copies a staged Uint8Array into a .NET-provided span (zero extra allocations on the JS side).
export function copyBytes(source: Uint8Array, destination: DotNetMemoryView): void {
    destination.set(source);
}

// Writes through a writable stream, aborting it when the write fails. A stream that is neither
// closed nor aborted keeps its exclusive lock on the file, and every later write to that file -
// the flusher's retries, a checkpoint - would fail until the tab was closed. Aborting also discards
// the swap copy, so nothing half-written can become the file.
async function writeStream(handle: FileSystemFileHandle, options: FileSystemCreateWritableOptions | undefined, chunk: FileSystemWriteChunkType): Promise<void> {
    const writable = await handle.createWritable(options);
    try {
        await writable.write(chunk);
    } catch (error) {
        try {
            await writable.abort();
        } catch {
            // The original failure is the one worth reporting.
        }
        throw error;
    }
    await writable.close();
}

export async function writeAtomic(directory: FileSystemDirectoryHandle, name: string, bytes: Uint8Array): Promise<void> {
    const handle = await directory.getFileHandle(name, { create: true });
    await writeStream(handle, undefined, bytes as Uint8Array<ArrayBuffer>);
}

// On the main thread the only writable API is createWritable(), which stages the whole file in a
// swap copy and swaps it in on close(). An append is therefore an atomic rewrite whose cost grows
// with the WAL - fine at the default checkpoint size, and the reason the engine compacts the log
// rather than letting it grow. (Sync access handles would avoid the copy but exist only in workers.)
export async function appendFile(directory: FileSystemDirectoryHandle, name: string, bytes: Uint8Array): Promise<void> {
    const handle = await directory.getFileHandle(name, { create: true });
    const file = await handle.getFile();
    await writeStream(handle, { keepExistingData: true }, { type: 'write', position: file.size, data: bytes as Uint8Array<ArrayBuffer> });
}

// { usage, quota } for this origin, or null when the browser does not implement estimate().
// Values are deliberately coarse (browsers pad them) and cover the whole origin, not just BlazeDb.
export async function storageEstimate(): Promise<{ usage: number; quota: number } | null> {
    if (!navigator.storage || !navigator.storage.estimate) {
        return null;
    }
    const estimate = await navigator.storage.estimate();
    return { usage: estimate.usage ?? 0, quota: estimate.quota ?? 0 };
}

// Asks the browser to exempt this origin from automatic eviction under storage pressure.
// Resolves to whether persistence is granted; browsers may grant it silently based on engagement.
export async function requestPersistence(): Promise<boolean> {
    if (!navigator.storage || !navigator.storage.persist) {
        return false;
    }
    if (await navigator.storage.persisted()) {
        return true;
    }
    return await navigator.storage.persist();
}

export async function deleteFile(directory: FileSystemDirectoryHandle, name: string): Promise<void> {
    try {
        await directory.removeEntry(name);
    } catch (error) {
        if (!isNotFound(error)) {
            throw error;
        }
    }
}

function isNotFound(error: unknown): boolean {
    return (error as { name?: string } | null)?.name === 'NotFoundError';
}
