// OPFS as a bag of files, without the engine.
//
// Probe and admin run on the window: getDirectory is available there. Sync access handles are
// not - those exist only in the worker, which is why the VFS is worker-only and this module is not.
// Related files (-journal, -wal) and the VFS's temporary .ahp-* directories are not databases.

const RELATED_SUFFIXES = ['-journal', '-wal'];
const TEMP_DIR_PREFIX = '.ahp-';

/** What `probe` reports to the .NET provider, which turns it into a BsqliteProbeResult. */
interface ProbeResult {
    available: boolean;
    reason: string | null;
    quotaBytes: number | null;
    usageBytes: number | null;
    environment: Record<string, string>;
}

// Neither is in TypeScript's DOM typings: one exists only in workers, the other is not standard yet.
declare const DedicatedWorkerGlobalScope: { new(): unknown } | undefined;

interface IterableDirectoryHandle extends FileSystemDirectoryHandle {
    entries(): AsyncIterable<[string, FileSystemHandle]>;
}

export async function probe(): Promise<ProbeResult> {
    const environment = {
        secureContext: String(Boolean(globalThis.isSecureContext)),
        hasGetDirectory: String(typeof navigator.storage?.getDirectory === 'function'),
        hasCreateSyncAccessHandle: String(
            typeof (FileSystemFileHandle?.prototype as { createSyncAccessHandle?: unknown } | undefined)?.createSyncAccessHandle === 'function'),
        isDedicatedWorker: String(typeof DedicatedWorkerGlobalScope !== 'undefined'
            && globalThis instanceof (DedicatedWorkerGlobalScope as { new(): unknown })),
    };

    if (!globalThis.isSecureContext) {
        return unavailable('OPFS requires a secure context (HTTPS or localhost).', environment);
    }

    if (typeof navigator.storage?.getDirectory !== 'function') {
        return unavailable(
            'This browser does not expose navigator.storage.getDirectory, which OPFS needs.',
            environment);
    }

    try {
        const root = await navigator.storage.getDirectory();
        if (!root) {
            return unavailable('navigator.storage.getDirectory() returned nothing.', environment);
        }

        let quotaBytes: number | null = null;
        let usageBytes: number | null = null;
        if (typeof navigator.storage.estimate === 'function') {
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
    const name = requireName(databaseName);

    try {
        await getFileHandle(name, { create: false });
        return true;
    } catch (error) {
        if (isNotFound(error)) {
            return false;
        }

        throw error;
    }
}

export async function list(): Promise<string[]> {
    const root = await navigator.storage.getDirectory();
    const names: string[] = [];
    await collect(root, '', names);
    names.sort();
    return names;
}

export async function deleteDatabase(databaseName: string): Promise<void> {
    const name = requireName(databaseName);
    const { directory, fileName } = await directoryFor(name, { create: false }).catch(error => {
        if (isNotFound(error)) {
            return { directory: null, fileName: null };
        }

        throw error;
    });

    if (!directory || !fileName) {
        return;
    }

    for (const suffix of ['', ...RELATED_SUFFIXES]) {
        try {
            await directory.removeEntry(fileName + suffix);
        } catch (error) {
            if (!isNotFound(error)) {
                throw error;
            }
        }
    }
}

export async function exportDatabase(databaseName: string): Promise<Uint8Array> {
    const name = requireName(databaseName);

    try {
        const handle = await getFileHandle(name, { create: false });

        // A rollback journal with a header is a write that has not finished - another tab's, or one a
        // crash cut short. The file alone is then half old and half new pages: SQLite rolls the journal
        // back into it on the next open, and an image taken now would pass every header check and still
        // be torn. A journal that was committed is deleted, truncated, or has its header zeroed.
        if (await hasHotJournal(name)) {
            throw new Error(
                `OPFS database '${name}' has a rollback journal from a write that has not finished. Open it `
                + 'once so SQLite rolls the journal back, or wait for the write to commit, then export it.');
        }

        const file = await handle.getFile();
        return new Uint8Array(await file.arrayBuffer());
    } catch (error) {
        if (isNotFound(error)) {
            throw new Error(`OPFS holds no database named '${name}'.`);
        }

        throw error;
    }
}

export async function importDatabase(databaseName: string, contents: Uint8Array | ArrayLike<number> | null): Promise<void> {
    const name = requireName(databaseName);
    const bytes = contents instanceof Uint8Array ? contents : new Uint8Array(contents ?? []);
    const { directory, fileName } = await directoryFor(name, { create: true });

    // A journal with a header is a write that has not finished - another tab's, or one a crash cut short -
    // and the file alone is then torn: the journal is what rolls it back. Deleted here, an import that then
    // failed to write would leave the old database torn for good, so it is refused and the journal kept.
    if (await hasHotJournal(name)) {
        throw new Error(
            `OPFS database '${name}' has a rollback journal from a write that has not finished. Open it once so `
            + 'SQLite rolls the journal back, or wait for the write to commit, then import over it.');
    }

    // The old file's journal goes first. Left until after the new file is written, a tab closed in
    // between - or a removal that fails - would leave it to be rolled back into the imported database
    // on its next open, writing the old database's pages over the new one's.
    for (const suffix of RELATED_SUFFIXES) {
        try {
            await directory.removeEntry(fileName + suffix);
        } catch (error) {
            if (!isNotFound(error)) {
                throw error;
            }
        }
    }

    // A writable stream writes to a swap file and replaces the file only on close, so an interrupted
    // import leaves the previous file whole.
    const handle = await directory.getFileHandle(fileName, { create: true });
    const writable = await handle.createWritable();
    // Never a SharedArrayBuffer view: the bytes come from .NET interop or a fresh copy.
    await writable.write(bytes as Uint8Array<ArrayBuffer>);
    await writable.close();
}

async function hasHotJournal(databaseName: string): Promise<boolean> {
    const { directory, fileName } = await directoryFor(databaseName, { create: false });

    let journal: File;
    try {
        journal = await (await directory.getFileHandle(fileName + '-journal', { create: false })).getFile();
    } catch (error) {
        if (isNotFound(error)) {
            return false;
        }

        throw error;
    }

    // SQLite's own test: a journal is hot when its header is there - the first bytes not zero.
    const header = new Uint8Array(await journal.slice(0, 8).arrayBuffer());
    return header.some(byte => byte !== 0);
}

function unavailable(reason: string, environment: Record<string, string>): ProbeResult {
    return { available: false, reason, quotaBytes: null, usageBytes: null, environment };
}

function requireName(databaseName: unknown): string {
    if (typeof databaseName !== 'string' || databaseName.trim() === '') {
        throw new Error('A database name is required.');
    }

    const name = databaseName.trim();
    if (name.includes('..') || name.startsWith('/') || name.startsWith('\\')) {
        throw new Error(`'${name}' is not a valid OPFS database name.`);
    }

    return name;
}

async function getFileHandle(databaseName: string, { create }: { create: boolean }): Promise<FileSystemFileHandle> {
    const { directory, fileName } = await directoryFor(databaseName, { create });
    return await directory.getFileHandle(fileName, { create });
}

/**
 * The directory and file name the VFS keeps <paramref name="databaseName"/> under. OPFSCoopSyncVFS
 * names its files by the URL path of the name it is opened with, so a space or a non-ASCII character
 * is stored percent-encoded - `My Notes.db` is the file `My%20Notes.db` - and the admin surface has to
 * look where the engine writes, or it reports a database that exists as absent.
 */
async function directoryFor(databaseName: string, { create }: { create: boolean }): Promise<{ directory: FileSystemDirectoryHandle; fileName: string }> {
    const parts = new URL(databaseName, 'file://').pathname.split('/').filter(Boolean);
    const fileName = parts.pop()!;
    let directory = await navigator.storage.getDirectory();

    for (const part of parts) {
        directory = await directory.getDirectoryHandle(part, { create });
    }

    return { directory, fileName };
}

async function collect(directory: FileSystemDirectoryHandle, prefix: string, names: string[]): Promise<void> {
    for await (const [name, handle] of (directory as IterableDirectoryHandle).entries()) {
        if (handle.kind === 'directory') {
            if (name.startsWith(TEMP_DIR_PREFIX)) {
                continue;
            }

            const decoded = decodeName(name);
            await collect(handle as FileSystemDirectoryHandle, prefix ? `${prefix}/${decoded}` : decoded, names);
            continue;
        }

        if (RELATED_SUFFIXES.some(suffix => name.endsWith(suffix))) {
            continue;
        }

        const decoded = decodeName(name);
        names.push(prefix ? `${prefix}/${decoded}` : decoded);
    }
}

/**
 * The database name a file of the VFS's stands for: its URL path segment, decoded - but only when the decoded
 * name maps back to this same segment through {@link directoryFor}. The URL parser escapes a space or a
 * non-ASCII character and leaves `%` alone, so a file named `a%41.db` or `a%2Fb.db` came from a name spelled
 * that way: decoded, it would read as `aA.db` or `a/b.db`, which are other files.
 */
function decodeName(segment: string): string {
    let decoded: string;
    try {
        decoded = decodeURIComponent(segment);
    } catch {
        return segment;
    }

    return new URL(decoded, 'file://').pathname === `/${segment}` ? decoded : segment;
}

function isNotFound(error: unknown): boolean {
    const name = (error as Error | undefined)?.name;
    return name === 'NotFoundError' || name === 'TypeError';
}
