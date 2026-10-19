// Types for the vendored wa-sqlite JavaScript API (engine/sqlite-api.js, wa-sqlite v1.1.2). The module
// itself is fetched and checksummed at restore (see engine/wa-sqlite.lock.props), not compiled here;
// these declarations only let the TypeScript modules in this folder import it, and they describe the
// members those modules call rather than the whole API.

export * from './sqlite-constants.js';

/** A pointer into the engine's WebAssembly heap. */
export type Pointer = number;

/** The value of a column or a bound parameter as wa-sqlite reads and writes it. */
export type SQLiteCompatibleType = number | string | Uint8Array | ArrayLike<number> | bigint | null;

/** Raised by every API call that fails, with the primary SQLite result code. */
export declare class SQLiteError extends Error {
    constructor(message: string, code: number);
    code: number;
}

/** The Emscripten module of an engine build. */
export interface SQLiteModule {
    HEAPU8: Uint8Array;
    UTF8ToString(pointer: Pointer, maxBytesToRead?: number): string;
    addFunction(fn: (...args: number[]) => number, signature: string): Pointer;
    ccall(name: string, returnType: string | null, argTypes: string[], args: unknown[]): any;
    cwrap(name: string, returnType: string | null, argTypes: string[]): (...args: any[]) => any;
    getTempRet0(): number;
    getValue(pointer: Pointer, type: string): number;
    _sqlite3_malloc(size: number): Pointer;
    _sqlite3_free(pointer: Pointer): void;
    _sqlite3_aggregate_context(context: Pointer, size: number): Pointer;
    [member: string]: any;
}

/** The JavaScript API wa-sqlite builds over an engine module. */
export interface SQLiteAPI {
    libversion(): string;
    open_v2(filename: string, flags?: number, vfsName?: string): Promise<number>;
    close(db: number): Promise<number>;
    changes(db: number): number;
    get_autocommit(db: number): number;
    update_hook(db: number, callback: (operation: number, schema: string | null, table: string | null, rowid: bigint) => void): void;
    commit_hook(db: number, callback: (() => number) | null): void;

    statements(db: number, sql: string, options?: { unscoped?: boolean; flags?: number }): AsyncIterable<number>;
    step(stmt: number): Promise<number>;
    reset(stmt: number): Promise<number>;
    finalize(stmt: number): Promise<number>;
    clear_bindings(stmt: number): number;

    bind(stmt: number, index: number, value: SQLiteCompatibleType): number;
    bind_parameter_count(stmt: number): number;
    bind_parameter_name(stmt: number, index: number): string | null;

    column(stmt: number, index: number): SQLiteCompatibleType;
    column_blob(stmt: number, index: number): Uint8Array;
    column_count(stmt: number): number;
    column_double(stmt: number, index: number): number;
    column_int64(stmt: number, index: number): bigint;
    column_names(stmt: number): string[];
    column_text(stmt: number, index: number): string;
    column_type(stmt: number, index: number): number;

    create_function(
        db: number,
        name: string,
        nArg: number,
        eTextRep: number,
        pApp: number,
        xFunc?: ((context: Pointer, values: Uint32Array) => void) | null,
        xStep?: ((context: Pointer, values: Uint32Array) => void) | null,
        xFinal?: ((context: Pointer) => void) | null): number;
    result(context: Pointer, value: SQLiteCompatibleType): void;
    value_text(value: Pointer): string;
    value_type(value: Pointer): number;

    vfs_register(vfs: unknown, makeDefault?: boolean): number;

    [member: string]: any;
}

/** Wraps an engine module in the JavaScript API. */
export declare function Factory(module: SQLiteModule): SQLiteAPI;
