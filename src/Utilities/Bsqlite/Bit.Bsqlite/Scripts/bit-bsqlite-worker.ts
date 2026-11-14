// The dedicated worker that owns one SQLite database.
//
// It exists because the storage APIs worth using are worker-only: OPFS synchronous access handles do
// not exist on the main thread. Everything here is request/response over postMessage, correlated by
// id, and strictly serialized - one SQLite connection cannot execute two statements at once, so
// overlapping requests queue rather than interleave.
//
// Two execute paths share one engine loop. `executeBinary` is what the .NET transport uses: the
// batch arrives as a transferred buffer, rows are written straight from the statement into a
// reply buffer, and that buffer is transferred back. `execute` is the JavaScript-facing path with
// structured-clone-friendly tagged rows. Both are driven by `runBatch` with a different row sink.

import * as SQLite from './engine/sqlite-api.js';
import type { SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';
import type { JsModuleReference } from './bit-bsqlite-host.js';
import type { BatchRequest } from './bit-bsqlite-wire.js';

// This module runs as a dedicated worker, while the shared tsconfig types every module against the DOM
// (the host and the storage admin modules run on the window). Only this much of the worker scope is
// used here, so it is described rather than pulling in the WebWorker lib, which conflicts with DOM.
interface MessageTarget {
    postMessage(message: unknown, transfer?: Transferable[]): void;
}

const scope = self as unknown as MessageTarget & {
    addEventListener(type: 'message', listener: (event: MessageEvent) => void): void;
};

// The host spawns this worker at a URL carrying the package version, and every module below has to
// answer to that same key. A static import would not: the browser resolves it to the bare path and
// can satisfy it from a copy cached under an older version, which then links against this file and
// fails - as an error event with no message, because that is all a worker that died at load can say.
const VERSION = new URL(import.meta.url).search;

// Bound through `ready` rather than by an import statement, so the message listener below is still
// registered synchronously. The host posts `open` the instant it constructs the worker, and a
// message that arrives before there is a listener is gone.
type Engine = typeof import('./bit-bsqlite-engine.js');
type Functions = typeof import('./bit-bsqlite-functions.js');
type Wire = typeof import('./bit-bsqlite-wire.js');

let loadEngine: Engine['loadEngine'];
let registerFunctions: Functions['registerFunctions'];
let resetAggregateState: Functions['resetAggregateState'];
let BinaryWriter: Wire['BinaryWriter'];
let ResultWriter: Wire['ResultWriter'];
let beginReply: Wire['beginReply'];
let decodeBatchBinary: Wire['decodeBatchBinary'];
let decodeParameter: Wire['decodeParameter'];
let encodeErrorReply: Wire['encodeErrorReply'];
let encodeRow: Wire['encodeRow'];

const ready = (async () => {
    ({ loadEngine } = await import('./bit-bsqlite-engine.js' + VERSION));
    ({ registerFunctions, resetAggregateState } = await import('./bit-bsqlite-functions.js' + VERSION));
    ({
        BinaryWriter,
        ResultWriter,
        beginReply,
        decodeBatchBinary,
        decodeParameter,
        encodeErrorReply,
        encodeRow,
    } = await import('./bit-bsqlite-wire.js' + VERSION));
})();

interface Session {
    sqlite3: SQLiteAPI;
    module: SQLiteModule;
    db: number;
    build: string;
    vfsName: string | null;
    /** The VFS, when it records the exception behind its last error. See {@link describe}. */
    vfs: { lastError?: unknown } | null;
    /**
     * The extension modules' register functions, run on every connection this worker opens on the database -
     * the snapshot's copy included, whose VACUUM rebuilds indexes that may use their functions or collations.
     */
    extensions: ExtensionRegistration[];
    databaseName: string;
    limits: { supportsMultiDatabaseTransactions: boolean; canChangePageSize: boolean };
    statements: StatementCache;
    /**
     * Tables written since the last notification, and whether a transaction has committed since then.
     * See {@link notifyIfCommitted}.
     */
    changes: { tables: Set<string>; committed: boolean; commitPending: boolean };
    stmtReadonly: (stmt: number) => number;
    lastInsertRowId: (db: number) => bigint;
    bindZeroBlob: (stmt: number, index: number, length: number) => number;
}

type ExtensionRegistration = (handles: { module: SQLiteModule; sqlite3: SQLiteAPI; db: number; databaseName: string }) => unknown;

/** What `open` is asked for, as the host posts it. */
interface OpenRequest {
    databaseName: string;
    requiredBuild: string;
    vfs?: JsModuleReference | null;
    limits?: { supportsMultiDatabaseTransactions?: boolean; canChangePageSize?: boolean };
    foreignKeys?: boolean;
    onOpenSql?: string[];
    extensions?: JsModuleReference[];
}

/** The callbacks one execute path streams a batch's outcome into. See {@link runBatch}. */
interface RowSink {
    begin(): void;
    columns(names: string[], storageClasses: (number | null)[]): void;
    beginRows(): void;
    row(sqlite3: SQLiteAPI, stmt: number, columnCount: number): void;
    end(recordsAffected: number, lastInsertRowId: bigint): void;
}

/** A result set of the JavaScript-facing `execute` path. */
interface TaggedResult {
    columnNames: string[];
    columnTypes: (string | null)[];
    rows: { t: number[]; v: unknown[] }[];
    recordsAffected: number;
    lastInsertRowId: bigint | number;
}

let session: Session | null = null;

// Requests are chained so that each runs to completion before the next begins. Without this a second
// execute could start stepping while the first is suspended awaiting storage.
let queue: Promise<void> = Promise.resolve();

scope.addEventListener('message', event => {
    const request = event.data;
    const port: MessageTarget = event.ports?.[0] ?? scope;

    const run = async () => {
        try {
            await ready;

            if (request.kind === 'executeBinary') {
                // Never throws across the boundary: the reply itself carries success or failure, so the
                // .NET side decodes exactly one shape.
                const reply = await executeBinary(request.batch);
                port.postMessage({ id: request.id, ok: true, result: reply }, [reply.buffer]);
                return;
            }

            const result = await dispatch(request);
            port.postMessage({ id: request.id, ok: true, result });
        } catch (error) {
            port.postMessage({ id: request.id, ok: false, error: describe(error) });
        }
    };

    // The chain has to survive its own failure. `then(run)` alone would skip every later request
    // once one link rejected - and postMessage can still throw after the catch above, on a result
    // the structured clone algorithm refuses. A wedged worker answers nothing, ever again.
    queue = queue.then(run, run).catch(() => { });
});

/**
 * Errors cross the boundary as data, so the .NET side can raise a typed exception carrying the SQLite
 * result code rather than a flattened string.
 */
function describe(error: any): { message: string; sqliteCode: number | null; name: string } {
    const described = {
        message: error?.message ?? String(error),
        // wa-sqlite raises SQLiteError with the primary result code; anything else has no code.
        sqliteCode: typeof error?.code === 'number' ? error.code : null,
        name: error?.name ?? 'Error',
    };

    // A VFS turns every storage exception into SQLITE_IOERR - or SQLITE_FULL - and keeps the exception. A
    // full origin is worth telling apart: the .NET side raises BsqliteQuotaExceededException for it, which
    // an application can act on by freeing space, where a plain I/O error says nothing it can act on.
    const cause = session?.vfs?.lastError as { name?: string; message?: string } | null | undefined;
    if ((described.sqliteCode === SQLite.SQLITE_IOERR || described.sqliteCode === SQLite.SQLITE_FULL)
        && cause?.name === 'QuotaExceededError') {
        described.name = cause.name;
        described.message = `${described.message}: ${cause.message ?? 'the storage quota is exhausted'}`;
    }

    return described;
}

async function dispatch(request: any): Promise<unknown> {
    switch (request.kind) {
        case 'open':
            return await open(request);

        case 'execute':
            return await execute(request.batch ?? []);

        case 'close':
            return await close();

        case 'version':
            return { version: requireSession().sqlite3.libversion(), build: session!.build };

        case 'serialize':
            return await snapshot(request.schema ?? 'main');

        default:
            throw new Error(`Unknown request kind '${request.kind}'.`);
    }
}

function requireSession(): Session {
    if (!session) {
        throw new Error('No database is open in this worker.');
    }

    return session;
}

async function open({ databaseName, requiredBuild, vfs, limits, foreignKeys, onOpenSql, extensions }: OpenRequest) {
    if (session) {
        if (session.databaseName !== databaseName) {
            throw new Error(
                `This worker already owns '${session.databaseName}'. A worker hosts one database, so `
                + `opening '${databaseName}' needs its own worker.`);
        }

        return { build: session.build, vfsName: session.vfsName, reused: true };
    }

    const engine = await loadEngine({ requiredBuild, databaseName, vfs });
    const { sqlite3, module } = engine;

    // The VFS name matters: passing it explicitly means a provider that registers without claiming the
    // default still gets used, and a bug in registration surfaces as "no such vfs" here rather than as
    // a database silently opened on the wrong storage.
    const db = await sqlite3.open_v2(
        databaseName,
        SQLite.SQLITE_OPEN_CREATE | SQLite.SQLITE_OPEN_READWRITE,
        engine.vfsName ?? undefined);

    // EF Core's SQLite provider only installs these on a real SqliteConnection. We are not one, so
    // every open has to - miss it and every decimal comparison, aggregate, and REGEXP is wrong.
    registerFunctions(module, sqlite3, db);

    // Change tracking for live queries. The update hook names every table a row was inserted into,
    // updated in, or deleted from - including through triggers and ON DELETE CASCADE, which no
    // reading of the SQL can know about - and the commit hook says when those rows became visible to
    // anyone else. Together they let a notification wait for COMMIT instead of firing on a write that
    // may yet roll back, and that another tab could not have seen anyway.
    const changes = { tables: new Set<string>(), committed: false, commitPending: false };
    sqlite3.update_hook(db, (_operation, _schema, table) => {
        if (table) {
            changes.tables.add(table);
        }
    });
    sqlite3.commit_hook(db, () => {
        // Pending, not done: the hook runs before the commit, which can still fail - SQLITE_BUSY while
        // another tab reads - leaving the transaction open for a ROLLBACK that throws its writes away.
        // runRequest counts it once the statement has succeeded and the connection is back in autocommit.
        changes.commitPending = true;
        return SQLite.SQLITE_OK;
    });

    const stmtReadonly = module.cwrap('sqlite3_stmt_readonly', 'number', ['number']);
    const bindZeroBlob = module.cwrap('sqlite3_bind_zeroblob', 'number', ['number', 'number', 'number']);
    const lastInsertRowId = (db: number) => {
        const lo32 = module.ccall('sqlite3_last_insert_rowid', 'number', ['number'], [db]);
        const hi32 = module.getTempRet0();
        return (BigInt(hi32) << 32n) | (BigInt(lo32) & 0xffffffffn);
    };

    session = {
        sqlite3,
        module,
        db,
        build: engine.build,
        vfsName: engine.vfsName,
        vfs: engine.vfs ?? null,
        extensions: [],
        databaseName,
        limits: {
            supportsMultiDatabaseTransactions: limits?.supportsMultiDatabaseTransactions !== false,
            canChangePageSize: limits?.canChangePageSize !== false,
        },
        statements: new StatementCache(sqlite3, db),
        changes,
        stmtReadonly,
        lastInsertRowId,
        bindZeroBlob,
    };

    // SQLite defaults foreign keys OFF, and Microsoft.Data.Sqlite turns them ON for every connection
    // it opens. We are not one, so without this the same model enforces its relationships on the
    // server and silently does not in the browser: an orphaned row inserts cleanly here and is
    // rejected there, and ON DELETE CASCADE never fires. On unless the caller opts out, and still
    // overridable - EF's own table-rebuild migrations toggle it off and back on.
    const configuration = [
        ...(foreignKeys === false ? [] : ['PRAGMA foreign_keys=ON']),
        ...(engine.onOpenSql ?? []),
        ...(onOpenSql ?? []),
    ];

    try {
        // Provider configuration (cache_size for batch-atomic and the like) and the application's own.
        // Run here rather than through execute() so a guard meant for application SQL cannot block them,
        // and before extensions so an extension sees the configured connection.
        for (const sql of configuration) {
            await runConfigurationSql(sqlite3, db, sql);
        }

        // Extension modules add user-defined functions, collations, or virtual tables. They receive the
        // same handles the built-in function set does.
        for (const extension of extensions ?? []) {
            const imported = await import(extension.moduleUrl);
            const exportName = extension.registerExport ?? 'register';
            const register = Object.hasOwn(imported, exportName) ? imported[exportName] : undefined;
            if (typeof register !== 'function') {
                throw new Error(`'${extension.moduleUrl}' does not export a function named '${exportName}'.`);
            }

            await register({ module, sqlite3, db, databaseName });
            session!.extensions.push(register);
        }
    } catch (error) {
        // An open that failed part way leaves nothing open: kept, the session would be handed to the next
        // open as `reused` - without the configuration or the extension that failed.
        session = null;
        await closeQuietly(sqlite3, db);
        throw error;
    }

    return { build: engine.build, vfsName: engine.vfsName, reused: false };
}

async function closeQuietly(sqlite3: SQLiteAPI, db: number): Promise<void> {
    try {
        await sqlite3.close(db);
    } catch {
        // The failure that brought us here is the one worth reporting.
    }
}

async function runConfigurationSql(sqlite3: SQLiteAPI, db: number, sql: string): Promise<void> {
    for await (const stmt of sqlite3.statements(db, sql)) {
        while (await sqlite3.step(stmt) === SQLite.SQLITE_ROW) {
            // Drained: these statements are configuration, not queries.
        }
    }
}

async function close() {
    if (!session) {
        return { closed: false };
    }

    const current = session;
    session = null;
    await current.statements.clear();
    await current.sqlite3.close(current.db);
    return { closed: true };
}

let snapshotSequence = 0;

/**
 * A consistent snapshot of the open database as a SQLite file image, reflecting every committed write,
 * taken without closing the connection.
 *
 * On the synchronous build that is `sqlite3_serialize`, byte for byte - inside a read transaction begun
 * through the statement API first. OPFSCoopSyncVFS takes its access handles cooperatively: a read that
 * finds the handle released answers SQLITE_BUSY and has the caller retry once the handle is back, which
 * wa-sqlite's prepare and step do and `sqlite3_serialize`, a single synchronous call, cannot - it fails
 * on the first page. Begun through step, the transaction holds the handle and the lock, and every page
 * serialize reads after that is a plain synchronous read.
 *
 * The asynchronous builds can only suspend inside the handful of entry points they were built to suspend
 * in - step, prepare, open, close - and `sqlite3_serialize` is not one of them: on IndexedDB or Cache
 * Storage every read may suspend, so it fails with "trying to suspend without WebAssembly.promising"
 * (JSPI) or corrupts Asyncify's stack. There the copy is made with SQL instead - see
 * {@link snapshotThroughVacuum}.
 */
async function snapshot(schema: string): Promise<Uint8Array> {
    const current = requireSession();
    if (current.build === 'synchronous' || !current.vfsName) {
        return await serializeInReadTransaction(current, schema);
    }

    if (schema !== 'main') {
        throw new Error(`Only the main schema can be snapshotted on the '${current.build}' engine build.`);
    }

    return await snapshotThroughVacuum(current);
}

/**
 * Copies the database into memory with `VACUUM ... INTO`, which runs as a statement - so the engine may
 * suspend in it - and then serializes the copy, which lives in the engine's own memory VFS and needs no
 * storage at all.
 *
 * The copy is made on a connection of its own, opened on a named `memdb` file with URI filenames on:
 * the database's own connection was opened without them, and `VACUUM INTO` and `ATTACH` read a URI only
 * on a connection that has them. That connection attaches the database through the same VFS - one more
 * connection to the file, coordinated with this one and other tabs by the VFS's locks - and vacuums it
 * into the memdb file it already holds open, which keeps the copy alive until it has been serialized.
 *
 * The image holds the same schema and rows, `user_version` and `application_id` as the database, but is
 * compacted: free pages are dropped and tables are rewritten in order, so it is not byte for byte the file.
 */
async function snapshotThroughVacuum(current: Session): Promise<Uint8Array> {
    const { sqlite3, module, databaseName } = current;
    const memdb = `file:/bit-bsqlite-snapshot-${++snapshotSequence}?vfs=memdb`;
    const source = `file:${encodeURIComponent(databaseName)}?vfs=${encodeURIComponent(current.vfsName!)}`;

    // Whatever the VFS kept from an earlier failure is not the cause of one here.
    if (current.vfs) {
        current.vfs.lastError = null;
    }

    const copy = await sqlite3.open_v2(
        memdb,
        SQLite.SQLITE_OPEN_CREATE | SQLite.SQLITE_OPEN_READWRITE | SQLite.SQLITE_OPEN_URI);
    try {
        // VACUUM rebuilds every index of the copy on this connection: an index on an expression or with a
        // collation needs the functions the database's own connection has - EF_DECIMAL, the ef_* set, and
        // whatever the extension modules added.
        registerFunctions(module, sqlite3, copy);
        for (const register of current.extensions) {
            await register({ module, sqlite3, db: copy, databaseName });
        }

        await runConfigurationSql(sqlite3, copy, `ATTACH ${sqlLiteral(source)} AS bit_bsqlite_source`);
        await runConfigurationSql(sqlite3, copy, `VACUUM bit_bsqlite_source INTO ${sqlLiteral(memdb)}`);
        await runConfigurationSql(sqlite3, copy, 'DETACH bit_bsqlite_source');
        return serialize({ module, db: copy }, 'main');
    } finally {
        await sqlite3.close(copy);
    }
}

async function serializeInReadTransaction(current: Session, schema: string): Promise<Uint8Array> {
    const { sqlite3, db } = current;
    await runConfigurationSql(sqlite3, db, 'BEGIN');
    try {
        // Any read of the schema takes the shared lock - and, on OPFS, the file's access handle - for the
        // rest of the transaction.
        await runConfigurationSql(sqlite3, db, `SELECT count(*) FROM "${schema.replaceAll('"', '""')}".sqlite_schema`);
        return serialize(current, schema);
    } finally {
        await runConfigurationSql(sqlite3, db, 'ROLLBACK');
    }
}

function sqlLiteral(text: string): string {
    return `'${text.replaceAll("'", "''")}'`;
}

/** `sqlite3_serialize`: only for a database whose pages can be read without suspending. */
function serialize({ module, db }: Pick<Session, 'module' | 'db'>, schema: string): Uint8Array {
    const sizePtr = module._sqlite3_malloc(8);
    try {
        const pointer = module.ccall(
            'sqlite3_serialize',
            'number',
            ['number', 'string', 'number', 'number'],
            [db, schema, sizePtr, 0]);

        const lo32 = module.getValue(sizePtr, 'i32');
        const hi32 = module.getValue(sizePtr + 4, 'i32');
        const size = hi32 * 0x100000000 + (lo32 >>> 0);

        if (!pointer) {
            if (size === 0) {
                // A database with no pages yet serializes to nothing, which is a valid (empty) image.
                return new Uint8Array(0);
            }

            throw new Error(`sqlite3_serialize could not snapshot schema '${schema}'.`);
        }

        try {
            return module.HEAPU8.slice(pointer, pointer + size);
        } finally {
            module._sqlite3_free(pointer);
        }
    } finally {
        module._sqlite3_free(sizePtr);
    }
}

/**
 * Runs a batch in one round trip, returning one result per request in order.
 *
 * Batching is the whole point of the coarse transport contract: in the browser the cost that dominates
 * is crossing this boundary, not executing the SQL.
 */
async function execute(batch: BatchRequest[]): Promise<TaggedResult[]> {
    const results: TaggedResult[] = [];

    await runBatch(batch, {
        begin: () => {
            results.push({
                columnNames: [],
                columnTypes: [],
                rows: [],
                recordsAffected: 0,
                lastInsertRowId: 0n,
            });
        },
        columns: (names, storageClasses) => {
            const result = results[results.length - 1];
            result.columnNames = names;
            result.columnTypes = storageClasses.map(code => code === null ? null : STORAGE_CLASS[code] ?? null);
        },
        beginRows: () => { },
        row: (sqlite3, stmt, columnCount) => {
            results[results.length - 1].rows.push(encodeRow(sqlite3, stmt, columnCount));
        },
        end: (recordsAffected, lastInsertRowId) => {
            const result = results[results.length - 1];
            result.recordsAffected = recordsAffected;
            result.lastInsertRowId = lastInsertRowId;
        },
    });

    // Tagged rows are plain data, but the BigInt row id is not JSON-safe for every caller.
    for (const result of results) {
        result.lastInsertRowId = Number(result.lastInsertRowId);
    }

    return results;
}

/** The .NET path: a binary batch in, a binary reply out, with no object graph in between. */
async function executeBinary(bytes: Uint8Array): Promise<Uint8Array> {
    // Sized by what a reply usually holds, not by the request: a request carrying a large blob gets a reply
    // of a few bytes, and a buffer four times its size would be allocated, zeroed and transferred for it.
    // The writer grows by doubling when rows need more.
    const writer = new BinaryWriter(64 * 1024);
    beginReply(writer);

    let batch;
    try {
        batch = decodeBatchBinary(bytes);
    } catch (error) {
        return encodeErrorReply(describe(error));
    }

    const countOffset = writer.reserveU32();
    const result = new ResultWriter(writer);
    let completed = 0;

    try {
        await runBatch(batch, {
            begin: () => {
                completed++;
            },
            columns: (names, storageClasses) => result.columns(names, storageClasses),
            beginRows: () => result.beginRows(),
            row: (sqlite3, stmt) => result.row(sqlite3, stmt),
            end: (recordsAffected, lastInsertRowId) => result.finish(recordsAffected, lastInsertRowId),
        });
    } catch (error) {
        // A failure part-way through discards the partial reply: the .NET side wants one exception,
        // not half a result set followed by one.
        return encodeErrorReply(describe(error));
    }

    writer.patchU32(countOffset, completed);
    return writer.finish();
}

/**
 * The engine loop shared by both execute paths.
 *
 * The sink receives, per request: `begin()`, then `columns(names, classes)` once the row-producing
 * statement is known (or with empty arrays if none), then `beginRows()`, then `row(...)` per row,
 * then `end(recordsAffected, lastInsertRowId)`.
 */
async function runBatch(batch: BatchRequest[], sink: RowSink): Promise<void> {
    const current = requireSession();
    let schemaChanged = false;

    // Whatever the VFS kept from an earlier failure is not the cause of one in this batch.
    if (current.vfs) {
        current.vfs.lastError = null;
    }

    try {
        for (const request of batch) {
            if (isSchemaChange(request.commandText)) {
                schemaChanged = true;
            }

            await runRequest(current, request, sink);
        }
    } finally {
        // Also on failure: the statements before the one that failed have run, and in autocommit mode
        // they have committed.
        notifyIfCommitted(current);

        if (schemaChanged) {
            // Cached statements compiled against the old schema would re-prepare themselves on step, but
            // their column metadata was read before stepping. Dropping them is cheap and always right.
            await current.statements.clear();
        }
    }
}

/**
 * Runs one request of a batch and streams its outcome into the sink.
 */
async function runRequest(current: Session, request: BatchRequest, sink: RowSink): Promise<void> {
    const { sqlite3, db } = current;

    rejectUnsupportedSql(request.commandText);
    sink.begin();

    const wantsRows = request.resultKind !== 'nonQuery';
    const maxRows = request.resultKind === 'scalar'
        ? 1
        : (Number.isInteger(request.maxRows) && request.maxRows! >= 0 ? request.maxRows! : Infinity);

    // CommandBehavior.SchemaOnly: the shape of the result, without running anything. Stepping even the
    // statements that return no rows would carry out a DELETE the caller only asked to describe.
    const schemaOnly = wantsRows && request.maxRows === 0;

    const prepared = await current.statements.acquire(request.commandText);
    // -1 until a statement that can write has run: what Microsoft.Data.Sqlite - and so the native transport
    // a Blazor Hybrid app runs on - reports for a query, as ADO.NET asks of a SELECT.
    let recordsAffected = -1;
    let captured = false;
    let succeeded = false;

    try {
        // Lazily: each statement of a multi-statement text is prepared only once the one before it has run,
        // as sqlite3_exec does, so `CREATE TABLE t ...; INSERT INTO t ...` finds the table it just created.
        for await (const stmt of prepared.statements) {
            try {
                bind(current, stmt, request.parameters ?? []);
                const producesRows = sqlite3.column_count(stmt) > 0;

                // Rows are taken from the first statement that produces any, and later statements still
                // run. This is what EF's insert-then-select pattern needs: the INSERT reports its changes
                // and the following SELECT is the result set the reader consumes.
                if (wantsRows && producesRows && !captured) {
                    captured = true;
                    let storageClasses: (number | null)[] | null = null;
                    let columnCount = 0;
                    let rows = 0;

                    try {
                        while (rows < maxRows && await sqlite3.step(stmt) === SQLite.SQLITE_ROW) {
                            if (storageClasses === null) {
                                // Read once the statement has stepped: a cached statement whose table another tab
                                // altered is prepared again inside sqlite3_step, and only then has its new columns.
                                // Storage classes of the first row, not declared types: the engine is compiled
                                // with SQLITE_OMIT_DECLTYPE, so declared types do not exist in this build.
                                columnCount = sqlite3.column_count(stmt);
                                storageClasses = readStorageClasses(sqlite3, stmt, columnCount);
                                sink.columns(sqlite3.column_names(stmt), storageClasses);
                                sink.beginRows();
                            }

                            sink.row(sqlite3, stmt, columnCount);
                            rows++;
                        }
                    } finally {
                        if (storageClasses === null) {
                            columnCount = sqlite3.column_count(stmt);
                            sink.columns(sqlite3.column_names(stmt), new Array(columnCount).fill(null));
                            sink.beginRows();
                        }
                    }
                } else if (!schemaOnly) {
                    while (await sqlite3.step(stmt) === SQLite.SQLITE_ROW) {
                        // Drained deliberately: a statement whose rows nobody asked for still has to run.
                    }
                }

                // Read-only statements leave sqlite3_changes() at the previous write's value, which must
                // not be counted twice - this is the same rule Microsoft.Data.Sqlite applies. Nor is a
                // statement that schema-only never ran.
                if (!schemaOnly && !current.stmtReadonly(stmt)) {
                    recordsAffected = Math.max(recordsAffected, 0) + sqlite3.changes(db);
                }

                succeeded = true;
            } finally {
                // Whether the statement finished or threw, its bindings and cursor must be released: to
                // free locks, and so a cached statement starts clean next time.
                await prepared.release(stmt);

                // Only now: a statement stopped before it was done - a scalar or single-row read of an
                // INSERT ... RETURNING in autocommit - commits in the reset above, and its commit hook with it.
                // A commit the hook announced for a statement that failed did not happen.
                if (succeeded && current.changes.commitPending && sqlite3.get_autocommit(db)) {
                    current.changes.committed = true;
                }

                current.changes.commitPending = false;
                succeeded = false;
            }
        }

        if (!captured) {
            sink.columns([], []);
            sink.beginRows();
        }
    } finally {
        await prepared.dispose();

        // Aggregate state is keyed by an address SQLite frees with the statement and reuses, so it
        // is dropped at the statement boundary rather than left to the final callbacks to clean up.
        resetAggregateState();

        // Also on failure, for the statements that ran before it.
        recordTextualWrites(current, request.commandText, recordsAffected);
    }

    sink.end(recordsAffected, current.lastInsertRowId(db));
}

/**
 * Adds the tables a request's SQL names, for the writes the update hook cannot see.
 *
 * The update hook is exact for row changes but blind to two things: DDL, which changes no row, and
 * `DELETE FROM t` with no WHERE, where SQLite's truncate optimisation bypasses the hook. The
 * statement text covers both, at the cost of the odd extra name - a comment or string that happens
 * to contain a keyword - which only causes an extra re-query, never a missed one. It is consulted
 * only when the request changed rows or reads as DDL, so a write that matched nothing raises
 * nothing. Mirrors BsqliteTableNames on the .NET side.
 */
function recordTextualWrites(current: Session, sql: string, recordsAffected: number): void {
    if ((recordsAffected > 0 || isSchemaChange(sql)) && looksLikeWrite(sql)) {
        for (const name of extractTables(sql)) {
            current.changes.tables.add(name);
        }
    }
}

/**
 * Tells the host which tables changed, once the change is committed.
 *
 * Nothing is sent while a transaction is open: a live query in another tab that re-ran on an
 * uncommitted write would read the old data and then never hear about the commit. Once the
 * connection is back in autocommit, the accumulated names go out if a commit happened, and are
 * dropped if the transaction rolled back - nothing anyone can see has changed.
 */
function notifyIfCommitted({ sqlite3, db, databaseName, changes }: Session): void {
    if (!sqlite3.get_autocommit(db)) {
        return;
    }

    const { tables, committed } = changes;
    changes.tables = new Set();
    changes.committed = false;

    if (!committed || tables.size === 0) {
        return;
    }

    scope.postMessage({ kind: 'notify', databaseName, tables: [...tables] });
}

/**
 * Prepared-statement cache keyed by SQL text.
 *
 * EF Core and most applications send the same statement text over and over with different
 * parameters. Preparing is a full parse and plan; resetting a prepared statement is nearly free.
 * Only single-statement texts are cached, because a multi-statement batch is almost never repeated
 * verbatim and would need every handle kept alive together.
 */
class StatementCache {
    static CAPACITY = 64;
    static SQLITE_PREPARE_PERSISTENT = 0x01;

    #sqlite3: SQLiteAPI;
    #db: number;
    /** Insertion order is recency: Map iteration gives the oldest first. */
    #entries = new Map<string, number>();
    /** Handles currently lent out, which must not be evicted or lent twice. */
    #inUse = new Set<number>();

    constructor(sqlite3: SQLiteAPI, db: number) {
        this.#sqlite3 = sqlite3;
        this.#db = db;
    }

    async acquire(sql: string): Promise<{ statements: AsyncIterable<number>; release(stmt: number): Promise<void>; dispose(): Promise<void> }> {
        const sqlite3 = this.#sqlite3;
        const cached = this.#entries.get(sql);

        if (cached !== undefined && !this.#inUse.has(cached)) {
            // Refresh recency.
            this.#entries.delete(sql);
            this.#entries.set(sql, cached);
            this.#inUse.add(cached);
            return {
                statements: (async function* () { yield cached; })(),
                release: stmt => this.#reset(stmt),
                dispose: async () => {
                    this.#inUse.delete(cached);
                },
            };
        }

        // Prepared one at a time, as the caller runs them: a statement is planned against the schema the
        // statements before it left, so preparing the whole text up front fails on anything a later
        // statement uses that an earlier one creates or alters.
        const prepared: number[] = [];
        let finished = false;
        const statements = (async function* (db: number) {
            for await (const stmt of sqlite3.statements(db, sql, {
                unscoped: true,
                flags: StatementCache.SQLITE_PREPARE_PERSISTENT,
            })) {
                prepared.push(stmt);
                yield stmt;
            }

            finished = true;
        })(this.#db);

        return {
            statements,
            release: stmt => this.#reset(stmt),
            dispose: async () => {
                // A text that turned out to be one statement, run to the end, is kept for the next time it
                // is sent; anything else - several statements, or one cut short by an error - is finalized.
                if (finished && prepared.length === 1 && cached === undefined && !this.#entries.has(sql)) {
                    this.#entries.set(sql, prepared[0]);
                    await this.#evict();
                    return;
                }

                for (const stmt of prepared) {
                    await sqlite3.finalize(stmt);
                }
            },
        };
    }

    async #reset(stmt: number): Promise<void> {
        try {
            await this.#sqlite3.reset(stmt);
        } catch {
            // reset() repeats the error code of a failed step, which has already been thrown.
        }

        try {
            this.#sqlite3.clear_bindings(stmt);
        } catch {
            // A statement that cannot clear is finalized on the next schema change or eviction.
        }
    }

    async #evict(): Promise<void> {
        while (this.#entries.size > StatementCache.CAPACITY) {
            let evicted = false;
            for (const [sql, stmt] of this.#entries) {
                if (this.#inUse.has(stmt)) {
                    continue;
                }

                this.#entries.delete(sql);
                await this.#sqlite3.finalize(stmt);
                evicted = true;
                break;
            }

            if (!evicted) {
                return;
            }
        }
    }

    async clear(): Promise<void> {
        const entries = [...this.#entries.values()];
        this.#entries.clear();
        for (const stmt of entries) {
            if (!this.#inUse.has(stmt)) {
                await this.#sqlite3.finalize(stmt);
            }
        }
    }
}

function isSchemaChange(sql: unknown): boolean {
    return typeof sql === 'string' && /\b(?:create|drop|alter|vacuum|reindex)\b/i.test(sql);
}

// Every statement, not just the first: a batch is routinely `BEGIN; INSERT …; COMMIT;`, and looking
// only at the leading keyword would call that a read. Leading comments and a leading common table
// expression are skipped for the same reason: `WITH … INSERT INTO` is a write. A block comment is
// spelled out rather than matched lazily: a lazy body can end at any `*/`, so a run of `*//*` that
// never reaches a write backtracks through every way of splitting it - exponential in its length.
// Mirrors BsqliteTableNames.LooksLikeWrite.
function looksLikeWrite(sql: unknown): boolean {
    return typeof sql === 'string'
        && /(?:^|;)\s*(?:--[^\n]*\n\s*|\/\*[^*]*\*+(?:[^/*][^*]*\*+)*\/\s*)*(?:with\b[\s\S]*?)?\b(?:insert|update|delete|replace|create|drop|alter)\b/i.test(sql);
}

// Table-level and deliberately coarse. Accepts the identifier quoting SQLite does ("t", `t`, [t]),
// a schema prefix (main."t"), the conflict clause an UPDATE may carry (UPDATE OR REPLACE t), and the
// objects DDL names. Mirrors BsqliteTableNames.Extract.
function extractTables(sql: string): string[] {
    const names: string[] = [];
    const pattern = /\b(?:from|join|into|update(?:\s+or\s+(?:rollback|abort|replace|fail|ignore))?|table|view|trigger|index\s+(?:if\s+(?:not\s+)?exists\s+)?[\w"`[\]]+\s+on)\s+(?:if\s+(?:not\s+)?exists\s+)?(?:(?:temp|temporary)\s+)?(?:["`[]?[A-Za-z_]\w*["`\]]?\s*\.\s*)?["`[]?([A-Za-z_]\w*)/gi;
    let match;
    while ((match = pattern.exec(sql))) {
        if (!match[1].toLowerCase().startsWith('sqlite_')) {
            names.push(match[1]);
        }
    }

    return names;
}

/**
 * WAL is not a VFS-specific limitation: no web build of SQLite can provide the shared memory it
 * needs. Rejected here as well as in the .NET command layer so a JavaScript caller cannot enable it
 * either - and so a "success" that actually left DELETE mode cannot be mistaken for WAL.
 */
function rejectUnsupportedSql(sql: unknown): void {
    if (typeof sql !== 'string') {
        return;
    }

    // Cheap pre-check: the guarded statements all mention one of these words, and most SQL does not.
    const lower = sql.length < 4096 ? sql.toLowerCase() : null;
    if (lower !== null && !lower.includes('pragma') && !lower.includes('attach')) {
        return;
    }

    if (/pragma\s+journal_mode\s*=\s*['"`]?wal\b/i.test(sql)) {
        throw new Error(
            'WAL mode is not available in the browser: WebAssembly has no shared-memory primitives for it, '
            + 'and no web VFS implements it. Bsqlite uses DELETE or TRUNCATE journaling.');
    }

    const limits = session?.limits;
    if (limits && !limits.supportsMultiDatabaseTransactions && /(?:^|;)\s*attach(?:\s+database)?\b/i.test(sql)) {
        throw new Error(
            'ATTACH is not available on this storage backend: it cannot run a transaction that spans '
            + 'more than one database. Open each database on its own connection.');
    }

    if (limits && !limits.canChangePageSize && /pragma\s+page_size\s*=/i.test(sql)) {
        throw new Error(
            'PRAGMA page_size cannot be changed on this storage backend: the page size is fixed to the '
            + 'backend\'s block size.');
    }
}

/**
 * Binds by asking the statement what it wants, rather than handing the engine a dictionary and hoping
 * the keys line up.
 *
 * The alternative - `bind_collection` with a name-keyed object - leaves an unmatched parameter bound to
 * NULL, which turns a misspelled parameter name into wrong results instead of an error. Names are
 * matched with or without their sigil, because ADO.NET callers write `p0` as often as `@p0`, and a
 * statement using positional `?` markers reports no name at all and is matched by position.
 *
 * An empty blob is bound with `sqlite3_bind_zeroblob`: wa-sqlite's `bind_blob` copies the bytes into
 * `sqlite3_malloc(0)`, which is a NULL pointer, and SQLite binds a blob at a NULL pointer as NULL - so
 * an empty `byte[]`, such as a row version not yet assigned, would fail a NOT NULL column it satisfies
 * everywhere else.
 */
function bind(session: Session, stmt: number, parameters: ReadonlyArray<{ name: string; type: number; value: unknown }>): void {
    const { sqlite3 } = session;
    const count = sqlite3.bind_parameter_count(stmt);
    if (count === 0) {
        return;
    }

    const byName = parameters.length > 4 ? new Map(parameters.map(p => [p.name, p])) : null;
    const lookup = (name: string) => byName ? byName.get(name) : parameters.find(p => p.name === name);

    for (let i = 1; i <= count; i++) {
        const declared = sqlite3.bind_parameter_name(stmt, i);
        const parameter = declared
            ? (lookup(declared) ?? lookup(declared.slice(1)))
            : parameters[i - 1];

        if (!parameter) {
            throw new Error(
                `No value was supplied for parameter ${declared ?? `#${i}`}. Supplied: `
                + `${parameters.map(p => p.name).join(', ') || '(none)'}.`);
        }

        const value = decodeParameter(parameter);
        if (value instanceof Uint8Array && value.byteLength === 0) {
            const rc = session.bindZeroBlob(stmt, i, 0);
            if (rc !== SQLite.SQLITE_OK) {
                throw new Error(`Binding an empty blob to parameter ${declared ?? `#${i}`} failed with SQLite result code ${rc}.`);
            }

            continue;
        }

        sqlite3.bind(stmt, i, value);
    }
}

const STORAGE_CLASS: Readonly<Record<number, string | null>> = Object.freeze({
    [SQLite.SQLITE_INTEGER]: 'INTEGER',
    [SQLite.SQLITE_FLOAT]: 'REAL',
    [SQLite.SQLITE_TEXT]: 'TEXT',
    [SQLite.SQLITE_BLOB]: 'BLOB',
    [SQLite.SQLITE_NULL]: null,
});

function readStorageClasses(sqlite3: SQLiteAPI, stmt: number, columnCount: number): (number | null)[] {
    const classes = new Array<number | null>(columnCount);
    for (let i = 0; i < columnCount; i++) {
        const code = sqlite3.column_type(stmt, i);
        classes[i] = code === SQLite.SQLITE_NULL ? null : code;
    }

    return classes;
}
