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
    const handle = await getFileHandle(name, { create: true });
    const writable = await handle.createWritable();
    // Never a SharedArrayBuffer view: the bytes come from .NET interop or a fresh copy.
    await writable.write(bytes as Uint8Array<ArrayBuffer>);
    await writable.close();

    const { directory, fileName } = await directoryFor(name, { create: false });
    for (const suffix of RELATED_SUFFIXES) {
        try {
            await directory.removeEntry(fileName + suffix);
        } catch (error) {
            if (!isNotFound(error)) {
                throw error;
            }
        }
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
    if (name.includes('..') || name.startsWith('/') || name.startsWith('\\')) {
        throw new Error(`'${name}' is not a valid OPFS database name.`);
    }

    return name;
}

async function getFileHandle(databaseName: string, { create }: { create: boolean }): Promise<FileSystemFileHandle> {
    const { directory, fileName } = await directoryFor(databaseName, { create });
    return await directory.getFileHandle(fileName, { create });
}

async function directoryFor(databaseName: string, { create }: { create: boolean }): Promise<{ directory: FileSystemDirectoryHandle; fileName: string }> {
    const parts = databaseName.split('/').filter(Boolean);
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

            await collect(handle as FileSystemDirectoryHandle, prefix ? `${prefix}/${name}` : name, names);
            continue;
        }

        if (RELATED_SUFFIXES.some(suffix => name.endsWith(suffix))) {
            continue;
        }

        names.push(prefix ? `${prefix}/${name}` : name);
    }
}

function isNotFound(error: unknown): boolean {
    const name = (error as Error | undefined)?.name;
    return name === 'NotFoundError' || name === 'TypeError';
}
