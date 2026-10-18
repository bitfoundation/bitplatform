// Block-based Cache Storage VFS. Commits rely on SQLite's journal - the Cache API has no
// multi-entry atomic write, so we do not claim SQLITE_IOCAP_BATCH_ATOMIC.
//
// Pages are 4096-byte cache entries. Dirty pages flush on xSync, in parallel. Cross-tab writers
// are coordinated by WebLocksMixin. A database that exists only in besql's `bit-Besql` cache is
// imported losslessly on first open and then served from our own layout.
//
// Every operation is addressed by page index computed from the file's recorded size, never by
// enumerating the cache's keys: `cache.keys()` walks every entry of every database in the origin,
// and a DELETE-journal commit deletes the journal file on each transaction, so a key scan there
// would make commit cost grow with the size of all databases combined.

// The base classes are wa-sqlite's, served by Bit.Bsqlite from _content/Bit.Bsqlite/engine/ - next to this
// package's own folder at runtime, but outside this project on disk, so TypeScript cannot follow the
// relative paths. Their declarations come from Bit.Bsqlite's Scripts/engine/*.d.ts instead (the
// bit-bsqlite/* path in tsconfig.json); the type-only imports are erased and the emitted imports are
// exactly the ones written here.
// @ts-expect-error -- resolved by the browser, typed through the declarations below.
import { FacadeVFS as FacadeVFSModule } from '../Bit.Bsqlite/engine/FacadeVFS.js';
// @ts-expect-error -- resolved by the browser, typed through the declarations below.
import * as VFSModule from '../Bit.Bsqlite/engine/VFS.js';
// @ts-expect-error -- resolved by the browser, typed through the declarations below.
import { WebLocksMixin as WebLocksMixinModule } from '../Bit.Bsqlite/engine/WebLocksMixin.js';
import type { FacadeVFS as FacadeVFSType } from 'bit-bsqlite/engine/FacadeVFS.js';
import type * as VFSType from 'bit-bsqlite/engine/VFS.js';
import type { WebLocksMixin as WebLocksMixinType } from 'bit-bsqlite/engine/WebLocksMixin.js';

const FacadeVFS = FacadeVFSModule as typeof FacadeVFSType;
const VFS = VFSModule as typeof VFSType;
const WebLocksMixin = WebLocksMixinModule as typeof WebLocksMixinType;

export const VFS_NAME = 'cache-storage';
export const CACHE_NAME = 'bit-bsqlite';
export const BESQL_CACHE = 'bit-Besql';
export const PAGE_SIZE = 4096;

class File {
    path: string;
    flags: number;
    size = 0;
    dirty = new Map<number, Uint8Array>();

    constructor(path: string, flags: number) {
        this.path = path;
        this.flags = flags;
    }
}

export class CacheStorageVFS extends WebLocksMixin(FacadeVFS) {
    mapIdToFile = new Map<number, File>();

    #cache: Cache | null = null;

    static async create(name: string, module: unknown): Promise<CacheStorageVFS> {
        const vfs = new CacheStorageVFS(name, module, { lockPolicy: 'shared' });
        await vfs.isReady();
        return vfs;
    }

    constructor(name: string, module: unknown, options?: { lockPolicy?: 'exclusive' | 'shared' | 'shared+hint' }) {
        super(name, module, options);
    }

    override async isReady(): Promise<boolean> {
        await super.isReady();
        this.#cache = await caches.open(CACHE_NAME);
        return true;
    }

    override getFilename(fileId: number): string {
        return this.#file(fileId).path;
    }

    /** The cache opened by isReady(), which the static create() awaits before handing the VFS out. */
    get #store(): Cache {
        return this.#cache!;
    }

    #file(fileId: number): File {
        return this.mapIdToFile.get(fileId)!;
    }

    override async jOpen(zName: string | null, fileId: number, flags: number, pOutFlags: DataView): Promise<number> {
        const path = pathOf(zName || Math.random().toString(36).slice(2));
        let size = await this.#readSize(path);

        if (size === null) {
            const imported = await importBesqlIfPresent(fileNameOf(path));
            if (imported) {
                await this.#writeImage(path, imported);
                size = imported.byteLength;
            }
        }

        if (size === null) {
            if (!(flags & VFS.SQLITE_OPEN_CREATE)) {
                return VFS.SQLITE_CANTOPEN;
            }

            size = 0;
            await this.#writeSize(path, 0);
        }

        const file = new File(path, flags);
        file.size = size;
        this.mapIdToFile.set(fileId, file);
        pOutFlags.setInt32(0, flags, true);
        return VFS.SQLITE_OK;
    }

    override async jDelete(zName: string): Promise<number> {
        await this.#deletePath(pathOf(zName));
        return VFS.SQLITE_OK;
    }

    override async jAccess(zName: string, _flags: number, pResOut: DataView): Promise<number> {
        const size = await this.#readSize(pathOf(zName));
        pResOut.setInt32(0, size === null ? 0 : 1, true);
        return VFS.SQLITE_OK;
    }

    /**
     * On taking a shared lock, re-reads the file size another tab may have changed.
     *
     * The size is cached per open file, and it is the one fact SQLite asks the VFS for at the start
     * of every read transaction to decide how many pages the database has. A tab that opened while
     * the file was small and kept the number would read past nothing after another tab appended
     * pages - a "no such table" for a table that plainly exists. The lock is the moment the other
     * writer is guaranteed to have flushed, so this is where the number is refreshed.
     */
    override async jLock(fileId: number, lockType: number): Promise<number> {
        const result = await super.jLock(fileId, lockType);
        if (result === VFS.SQLITE_OK && lockType === VFS.SQLITE_LOCK_SHARED) {
            const file = this.mapIdToFile.get(fileId);
            if (file) {
                const size = await this.#readSize(file.path);
                if (size !== null) {
                    file.size = size;
                }
            }
        }

        return result;
    }

    override async jClose(fileId: number): Promise<number> {
        const file = this.mapIdToFile.get(fileId);
        this.mapIdToFile.delete(fileId);
        if (!file) {
            return VFS.SQLITE_OK;
        }

        if (file.flags & VFS.SQLITE_OPEN_DELETEONCLOSE) {
            await this.#deletePath(file.path);
            return VFS.SQLITE_OK;
        }

        await this.#flush(file);
        return VFS.SQLITE_OK;
    }

    override async jRead(fileId: number, pData: Uint8Array, iOffset: number): Promise<number> {
        const file = this.#file(fileId);
        if (iOffset >= file.size) {
            pData.fill(0);
            return VFS.SQLITE_IOERR_SHORT_READ;
        }

        let remaining = Math.min(pData.byteLength, file.size - iOffset);
        let written = 0;

        while (written < remaining) {
            const pageIndex = Math.floor((iOffset + written) / PAGE_SIZE);
            const pageOffset = (iOffset + written) % PAGE_SIZE;
            const page = await this.#readPage(file, pageIndex);
            const n = Math.min(PAGE_SIZE - pageOffset, remaining - written);
            pData.set(page.subarray(pageOffset, pageOffset + n), written);
            written += n;
        }

        if (written < pData.byteLength) {
            pData.fill(0, written);
            return VFS.SQLITE_IOERR_SHORT_READ;
        }

        return VFS.SQLITE_OK;
    }

    override async jWrite(fileId: number, pData: Uint8Array, iOffset: number): Promise<number> {
        const file = this.#file(fileId);
        let written = 0;

        while (written < pData.byteLength) {
            const pageIndex = Math.floor((iOffset + written) / PAGE_SIZE);
            const pageOffset = (iOffset + written) % PAGE_SIZE;
            const n = Math.min(PAGE_SIZE - pageOffset, pData.byteLength - written);
            const page = pageOffset === 0 && n === PAGE_SIZE
                ? new Uint8Array(PAGE_SIZE)
                : await this.#readPage(file, pageIndex);
            page.set(pData.subarray(written, written + n), pageOffset);
            file.dirty.set(pageIndex, page);
            written += n;
        }

        file.size = Math.max(file.size, iOffset + pData.byteLength);
        return VFS.SQLITE_OK;
    }

    override async jTruncate(fileId: number, size: number): Promise<number> {
        const file = this.#file(fileId);
        const previousPages = pageCount(file.size);
        file.size = size;
        const firstDropped = pageCount(size);
        for (const index of [...file.dirty.keys()]) {
            if (index >= firstDropped) {
                file.dirty.delete(index);
            }
        }

        await this.#dropPagesFrom(file.path, firstDropped, previousPages);
        await this.#writeSize(file.path, size);
        return VFS.SQLITE_OK;
    }

    override async jSync(fileId: number): Promise<number> {
        await this.#flush(this.#file(fileId));
        return VFS.SQLITE_OK;
    }

    override jFileSize(fileId: number, pSize64: DataView): number {
        pSize64.setBigInt64(0, BigInt(this.#file(fileId).size), true);
        return VFS.SQLITE_OK;
    }

    override jDeviceCharacteristics(): number {
        return VFS.SQLITE_IOCAP_UNDELETABLE_WHEN_OPEN;
    }

    async #flush(file: File): Promise<void> {
        if (file.dirty.size > 0) {
            const writes: Promise<void>[] = [];
            for (const [index, page] of file.dirty) {
                writes.push(this.#store.put(pageUrl(file.path, index), bytesResponse(page)));
            }

            file.dirty.clear();
            await Promise.all(writes);
        }

        await this.#writeSize(file.path, file.size);
    }

    async #readPage(file: File, index: number): Promise<Uint8Array> {
        const dirty = file.dirty.get(index);
        if (dirty) {
            return dirty;
        }

        const match = await this.#store.match(pageUrl(file.path, index));
        if (!match) {
            return new Uint8Array(PAGE_SIZE);
        }

        return new Uint8Array(await match.arrayBuffer());
    }

    async #readSize(path: string): Promise<number | null> {
        const match = await this.#store.match(metaUrl(path));
        if (!match) {
            return null;
        }

        const meta = await match.json();
        return typeof meta.size === 'number' ? meta.size : 0;
    }

    async #writeSize(path: string, size: number): Promise<void> {
        await this.#store.put(metaUrl(path), jsonResponse({ size }));
    }

    async #writeImage(path: string, bytes: Uint8Array): Promise<void> {
        await this.#deletePath(path);
        const writes: Promise<void>[] = [];
        for (let offset = 0; offset < bytes.byteLength; offset += PAGE_SIZE) {
            const page = new Uint8Array(PAGE_SIZE);
            page.set(bytes.subarray(offset, offset + PAGE_SIZE));
            writes.push(this.#store.put(pageUrl(path, offset / PAGE_SIZE), bytesResponse(page)));
        }

        await Promise.all(writes);
        await this.#writeSize(path, bytes.byteLength);
    }

    async #deletePath(path: string): Promise<void> {
        const size = await this.#readSize(path);
        if (size === null) {
            return;
        }

        await this.#dropPagesFrom(path, 0, pageCount(size));
        await this.#store.delete(metaUrl(path));
    }

    /**
     * Deletes pages [firstDropped, count) by index. The recorded size bounds the walk, so no key
     * enumeration is needed; a page that was never written is simply absent.
     */
    async #dropPagesFrom(path: string, firstDropped: number, count: number): Promise<void> {
        const deletions: Promise<boolean>[] = [];
        for (let index = firstDropped; index < count; index++) {
            deletions.push(this.#store.delete(pageUrl(path, index)));
        }

        await Promise.all(deletions);
    }
}

export function pathOf(databaseName: string): string {
    return new URL(databaseName, 'file://').pathname;
}

/** Pages a file of `size` bytes occupies. */
export function pageCount(size: number): number {
    return Math.ceil(size / PAGE_SIZE);
}

export function fileNameOf(path: string): string {
    return path.startsWith('/') ? path.slice(1) : path;
}

export function metaUrl(path: string): string {
    return `https://bit-bsqlite.invalid/bit-bsqlite/db/${encodeURIComponent(path)}/meta`;
}

export function pageUrl(path: string, index: number): string {
    return `https://bit-bsqlite.invalid/bit-bsqlite/db/${encodeURIComponent(path)}/p/${index}`;
}

export async function importBesqlIfPresent(fileName: string): Promise<Uint8Array | null> {
    if (typeof caches === 'undefined' || !fileName) {
        return null;
    }

    try {
        const cache = await caches.open(BESQL_CACHE);
        const match = await cache.match(`/data/cache/${fileName}`);
        if (!match) {
            return null;
        }

        return new Uint8Array(await match.arrayBuffer());
    } catch {
        return null;
    }
}

function bytesResponse(bytes: Uint8Array): Response {
    // Every page is a fresh Uint8Array, never a view of a SharedArrayBuffer.
    return new Response(bytes as Uint8Array<ArrayBuffer>, { headers: { 'content-type': 'application/octet-stream' } });
}

function jsonResponse(value: unknown): Response {
    return new Response(JSON.stringify(value), { headers: { 'content-type': 'application/json' } });
}
