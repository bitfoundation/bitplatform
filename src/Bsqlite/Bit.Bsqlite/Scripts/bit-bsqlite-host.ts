// The main-thread half of the transport: spawns the worker that owns a database and correlates
// requests with responses. This is the surface the .NET transport calls.

// Everything this module reaches for inherits the cache-busting query it was imported with, so an
// upgrade cannot pair a fresh host with a worker - or a wire module - the browser still holds from
// an older version.
const VERSION = new URL(import.meta.url).search;

const DEFAULT_WORKER_URL = new URL('./bit-bsqlite-worker.js' + VERSION, import.meta.url).href;

// Re-exported so a JavaScript caller can work in plain values without importing the wire module and
// without inventing a second, divergent encoding. The .NET transport does the same work in C#. The
// import is dynamic only to carry VERSION: a `export ... from` would resolve to the bare path.
const wire: typeof import('./bit-bsqlite-wire.js') = await import('./bit-bsqlite-wire.js' + VERSION);

export const decodeValue = wire.decodeValue;
export const encodeValue = wire.encodeValue;
export const WireType = wire.WireType;
export const encodeBatchBinary = wire.encodeBatchBinary;
export const decodeReplyBinary = wire.decodeReplyBinary;

/** A module the worker imports: a storage provider's VFS, or an extension registering functions. */
export interface JsModuleReference {
    moduleUrl: string;
    registerExport?: string;
}

export interface OpenOptions {
    databaseName: string;
    /** `'synchronous'` or `'asyncCapable'`. */
    requiredBuild: string;
    vfs?: JsModuleReference;
    limits?: { supportsMultiDatabaseTransactions?: boolean; canChangePageSize?: boolean };
    /** `false` to leave foreign-key enforcement off. */
    foreignKeys?: boolean;
    /** Configuration statements run once the database is open. */
    onOpenSql?: string[];
    /** Function/collation modules. */
    extensions?: JsModuleReference[];
    /** Overridable so an application can host the worker itself. */
    workerUrl?: string;
}

/** What a committed write reports: the database it was in and every table a row landed in. */
export interface NotifyPayload {
    kind: 'notify';
    databaseName: string;
    tables?: string[];
}

export type NotifyListener = (payload: NotifyPayload, context: { local: boolean }) => void;

/** The .NET side of a [JSInvokable] callback, as Blazor hands it to JavaScript. */
export interface DotNetObjectReference {
    invokeMethodAsync(name: string, ...args: unknown[]): Promise<unknown>;
}

/** An error rebuilt from the worker, carrying the SQLite result code when SQLite raised it. */
export interface SqliteHostError extends Error {
    sqliteCode?: number;
}

interface WorkerRequest {
    kind: string;
    vfs?: JsModuleReference;
    extensions?: JsModuleReference[];
    [key: string]: unknown;
}

/** A Web Lock request: `release` is set once it is granted, `abort` withdraws it until then. */
interface HeldLock {
    abort: AbortController;
    release: (() => void) | null;
}

interface WorkerResponse {
    id?: number;
    kind?: string;
    ok?: boolean;
    result?: unknown;
    error?: { message: string; sqliteCode?: number | null; name?: string };
}

/** Starts a worker for one database. */
export async function open({ databaseName, requiredBuild, vfs, limits, foreignKeys, onOpenSql, extensions, workerUrl }: OpenOptions): Promise<SqliteHost> {
    const host = createHost(workerUrl);
    await host.send({ kind: 'open', databaseName, requiredBuild, vfs, limits, foreignKeys, onOpenSql, extensions });
    return host;
}

/**
 * Creates a host without opening a database.
 *
 * Exists for the .NET transport: Blazor can hold an <c>IJSObjectReference</c> to the host and then
 * drive every operation - including open - through <c>call</c>, so a failure never has to throw
 * across the interop boundary.
 */
export function createHost(workerUrl?: string | null): SqliteHost {
    return new SqliteHost(workerUrl ?? DEFAULT_WORKER_URL);
}

/**
 * Forwards committed writes to a .NET object, which is how a live query re-runs - for this tab's
 * own writes and for another tab's alike.
 *
 * Both are forwarded because the worker is the one that knows: its update hook names every table a
 * row landed in, cascades and triggers included, and it waits for the commit. The .NET transport
 * declares `ReportsLocalWrites` so the command layer does not raise the same write again from the
 * SQL text. Writes to a different database on the same origin are dropped: the broadcast channel is
 * origin-wide, so it carries every database's traffic, and the name is the only thing that
 * separates them.
 *
 * `method` is the [JSInvokable] method to call with the table names.
 */
export function listen(host: SqliteHost, target: DotNetObjectReference, databaseName: string, method = 'OnTablesChanged'): () => boolean {
    return host.onNotify(payload => {
        if (payload.databaseName !== databaseName) {
            return;
        }

        // Fire and forget: a disposed .NET reference must not wedge the notification pipeline.
        target.invokeMethodAsync(method, payload.tables ?? []).catch(() => { });
    });
}

const CHANGE_CHANNEL = 'bit-bsqlite-changes';

export class SqliteHost {
    #worker: Worker;
    #pending = new Map<number, { resolve: (value: unknown) => void; reject: (reason: unknown) => void }>();
    #nextId = 1;
    #disposed = false;
    #fatal: Error | null = null;
    #listeners = new Set<NotifyListener>();
    #channel = typeof BroadcastChannel === 'function' ? new BroadcastChannel(CHANGE_CHANNEL) : null;
    #locks = new Map<number, HeldLock>();

    constructor(workerUrl: string) {
        this.#worker = new Worker(workerUrl, { type: 'module' });
        this.#worker.addEventListener('message', event => this.#settle(event.data));
        this.#channel?.addEventListener('message', event => this.#emit(event.data, { local: false }));

        // A worker that dies with requests outstanding must not leave them pending forever - and one
        // that died must not swallow later requests either. The worker script wraps every request in
        // its own catch, so an error event here means the script itself failed to load or link: there
        // is nobody on the other end, and every request from now on gets told so instead of hanging.
        this.#worker.addEventListener('error', (event: ErrorEvent) => {
            this.#fatal = new Error(`The SQLite worker failed: ${event.message ?? 'unknown error'}`);
            this.#failAll(this.#fatal);
        });

        // A reply that cannot be deserialized never reaches #settle, so its request would wait for an
        // answer that has already been thrown away. The id is gone with it, so every outstanding
        // request fails - the same trade the error handler makes, and better than hanging.
        this.#worker.addEventListener('messageerror', () => this.#failAll(
            new Error('A reply from the SQLite worker could not be deserialized.')));
    }

    /**
     * Subscribes to write notifications.
     *
     * The listener is called as `listener(payload, { local })`. `local` is true for a write this
     * host's own worker performed and false for one another tab broadcast, which is the distinction a
     * caller that already knows about its own writes needs in order not to react twice.
     */
    onNotify(listener: NotifyListener): () => boolean {
        this.#listeners.add(listener);
        return () => this.#listeners.delete(listener);
    }

    #emit(payload: NotifyPayload | undefined, { local }: { local: boolean }) {
        if (!payload || payload.kind !== 'notify') {
            return;
        }

        if (local) {
            this.#channel?.postMessage(payload);
        }

        for (const listener of this.#listeners) {
            try {
                listener(payload, { local });
            } catch (error) {
                console.error('A Bsqlite change listener threw.', error);
            }
        }
    }

    #settle(response: WorkerResponse | undefined) {
        if (response?.kind === 'notify') {
            this.#emit(response as NotifyPayload, { local: true });
            return;
        }

        const id = response?.id;
        const pending = id === undefined ? undefined : this.#pending.get(id);
        if (id === undefined || !pending) {
            return;
        }

        this.#pending.delete(id);

        if (response!.ok) {
            pending.resolve(response!.result);
        } else {
            pending.reject(toError(response!.error ?? { message: 'The SQLite worker failed without saying why.' }));
        }
    }

    #failAll(error: Error) {
        for (const pending of this.#pending.values()) {
            pending.reject(error);
        }

        this.#pending.clear();
    }

    /** `transfer` lists the buffers to move rather than copy into the worker. */
    send(request: WorkerRequest, transfer?: Transferable[]): Promise<unknown> {
        if (this.#disposed) {
            return Promise.reject(new Error('This SQLite host has been disposed.'));
        }

        if (this.#fatal) {
            return Promise.reject(this.#fatal);
        }

        const id = this.#nextId++;

        return new Promise((resolve, reject) => {
            this.#pending.set(id, { resolve, reject });
            this.#worker.postMessage({ ...resolveModuleUrls(request), id }, transfer ?? []);
        });
    }

    /** Executes a batch in one round trip, with tagged rows in the reply. */
    execute(batch: ReadonlyArray<{ commandText: string; parameters?: ReadonlyArray<{ name: string; type: number; value: unknown }>; resultKind?: 'nonQuery' | 'scalar' | 'reader'; maxRows?: number }>): Promise<unknown> {
        return this.send({ kind: 'execute', batch });
    }

    /**
     * Executes a binary-encoded batch and returns the binary reply, transferring both buffers so
     * neither is copied. Never rejects for a SQL failure: the reply carries it.
     *
     * `batch` is encoded by `encodeBatchBinary` or by `BsqliteWireFormat` on the .NET side.
     */
    async executeBinary(batch: Uint8Array | ArrayBuffer | ArrayLike<number>): Promise<Uint8Array> {
        const bytes = batch instanceof Uint8Array ? batch : new Uint8Array(batch as ArrayBuffer);
        const transferable = bytes.byteOffset === 0 && bytes.byteLength === bytes.buffer.byteLength
            ? [bytes.buffer]
            : [];
        const reply = await this.send({ kind: 'executeBinary', batch: bytes }, transferable);
        return reply instanceof Uint8Array ? reply : new Uint8Array(reply as ArrayBuffer);
    }

    /**
     * Runs any worker request and returns the outcome as data rather than by throwing.
     *
     * This exists for .NET callers, and the reason is a hard limit rather than a preference: Blazor's JS
     * interop reduces a thrown JavaScript error to its message, so the SQLite result code would be lost -
     * and that code is how EF tells a unique-constraint violation from a busy database. An envelope keeps
     * it.
     */
    async call(request: WorkerRequest): Promise<{ ok: true; result: unknown } | { ok: false; error: { message: string; sqliteCode: number | null; name: string } }> {
        try {
            return { ok: true, result: await this.send(request) };
        } catch (caught) {
            const error = caught as SqliteHostError;
            return {
                ok: false,
                error: {
                    message: error.message,
                    sqliteCode: error.sqliteCode ?? null,
                    name: error.name ?? 'Error',
                },
            };
        }
    }

    version(): Promise<unknown> {
        return this.send({ kind: 'version' });
    }

    /**
     * A consistent snapshot of the open database as a SQLite file image. `schema` is `'main'` unless
     * an attached database is wanted.
     */
    export(schema = 'main'): Promise<Uint8Array> {
        return this.send({ kind: 'serialize', schema }) as Promise<Uint8Array>;
    }

    async close(): Promise<void> {
        await this.send({ kind: 'close' });
    }

    /**
     * Waits for the Web Lock `name` and holds it until `releaseLock(id)` - or until this host is
     * disposed, or the tab goes away, whichever comes first.
     *
     * Web Locks are shared by every tab and worker of the origin, which is what makes this a lock
     * between the workers of different tabs that open the same file; and the browser drops a lock
     * whose holder is gone, so a tab that crashes mid-migration never leaves the others waiting.
     * `id` is chosen by the caller so it can release a request it gave up on before it was granted.
     */
    acquireLock(name: string, id: number): Promise<void> {
        if (this.#disposed) {
            return Promise.reject(new Error('This SQLite host has been disposed.'));
        }

        const locks = globalThis.navigator?.locks;
        if (!locks) {
            // The VFSes coordinate tabs through the same API, so without it there is nothing a lock
            // here could be coordinating with.
            return Promise.resolve();
        }

        const held: HeldLock = { abort: new AbortController(), release: null };
        this.#locks.set(id, held);

        return new Promise<void>((granted, failed) => {
            locks.request(name, { signal: held.abort.signal }, () => new Promise<void>(release => {
                if (this.#locks.get(id) !== held) {
                    // Released while still queued, and granted before the abort landed.
                    release();
                    return;
                }

                held.release = release;
                granted();
            })).catch(error => {
                if (this.#locks.get(id) === held) {
                    this.#locks.delete(id);
                }

                failed(error);
            });
        });
    }

    /** Releases a lock taken with `acquireLock`, or withdraws a request still waiting for one. */
    releaseLock(id: number): void {
        const held = this.#locks.get(id);
        if (!held) {
            return;
        }

        this.#locks.delete(id);
        if (held.release) {
            held.release();
        } else {
            held.abort.abort();
        }
    }

    /**
     * Terminates the worker. Outstanding requests are rejected rather than abandoned, because a caller
     * awaiting a query that will never answer is worse than a caller seeing the shutdown.
     */
    dispose(): void {
        if (this.#disposed) {
            return;
        }

        this.#disposed = true;
        this.#failAll(new Error('The SQLite host was disposed while the request was in flight.'));
        for (const id of [...this.#locks.keys()]) {
            this.releaseLock(id);
        }

        this.#listeners.clear();
        this.#channel?.close();
        this.#worker.terminate();
    }
}

/**
 * Makes a storage provider's VFS module URL - and any extension module URL - absolute before it
 * reaches the worker.
 *
 * Providers name their module the way every other Blazor asset is named - relative to the document,
 * as `./_content/<package>/<file>.js` - so an application served under a sub-path still finds it.
 * The worker cannot resolve that itself: a relative import there is relative to the worker script,
 * which lives in this package's own `_content` folder. Resolving here, where the document base is
 * known, keeps that detail out of every provider.
 */
function resolveModuleUrls(request: WorkerRequest): WorkerRequest {
    const hasVfs = typeof request?.vfs?.moduleUrl === 'string' && request.vfs.moduleUrl.length > 0;
    const hasExtensions = Array.isArray(request?.extensions) && request.extensions.length > 0;
    if (!hasVfs && !hasExtensions) {
        return request;
    }

    const base = globalThis.document?.baseURI ?? globalThis.location?.href ?? import.meta.url;
    const resolve = (module: JsModuleReference) => typeof module?.moduleUrl === 'string' && module.moduleUrl.length > 0
        ? { ...module, moduleUrl: new URL(module.moduleUrl, base).href }
        : module;

    return {
        ...request,
        ...(hasVfs ? { vfs: resolve(request.vfs!) } : {}),
        ...(hasExtensions ? { extensions: request.extensions!.map(resolve) } : {}),
    };
}

/**
 * Rebuilds an error from the worker, preserving the SQLite result code so callers can distinguish a
 * constraint violation from a busy database.
 */
function toError({ message, sqliteCode, name }: { message: string; sqliteCode?: number | null; name?: string }): SqliteHostError {
    const error: SqliteHostError = new Error(message);
    error.name = name ?? 'Error';

    if (sqliteCode !== null && sqliteCode !== undefined) {
        error.sqliteCode = sqliteCode;
    }

    return error;
}
