// The wire format shared by the worker, the host, and the .NET transport.
//
// Two encodings live here, and they exist for two different callers.
//
// The *binary* encoding is what the .NET transport speaks. A batch is one `Uint8Array`, a reply is
// one `Uint8Array`, and both cross the worker boundary as transferred buffers (zero-copy) and the
// Blazor interop boundary as byte arrays (which Blazor marshals without base64). There is no JSON to
// build or parse, a blob is copied once, and a 64-bit integer is eight bytes rather than a decimal
// string. `BsqliteWireFormat` on the .NET side mirrors this file byte for byte.
//
// The *tagged* encoding (`{ t: [...], v: [...] }` rows, `{ type, value }` parameters) is the
// JavaScript-facing shape: structured-clone friendly, readable in a debugger, and what a caller
// driving the host from JavaScript sees. It carries the same information - every value travels with
// its SQLite storage class - so the two encodings never disagree about what a value is.
//
// Both encodings tag values with SQLite's own storage-class codes, which lets the worker label a
// value straight from `sqlite3_column_type` with no translation table.

import type { SQLiteAPI, SQLiteCompatibleType } from './engine/sqlite-api.js';

/** A parameter in the tagged encoding: its SQLite storage class and its JSON-safe value. */
export interface TaggedParameter {
    name: string;
    type: number;
    value: unknown;
}

/** One request of a batch, in the tagged shape the worker executes. */
export interface BatchRequest {
    commandText: string;
    resultKind?: string | number;
    maxRows?: number | null;
    parameters?: ReadonlyArray<TaggedParameter>;
}

/** A row in the tagged encoding: one storage class and one value per column. */
export interface TaggedRow {
    t: number[];
    v: unknown[];
}

/** One result set of a decoded reply. */
export interface DecodedResult {
    columnNames: string[];
    columnTypes: (string | null)[];
    recordsAffected: number;
    lastInsertRowId: bigint;
    rows: TaggedRow[];
}

/** An error decoded from a reply, carrying the SQLite result code when SQLite raised it. */
export interface WireError extends Error {
    sqliteCode?: number;
}

export const WireType = Object.freeze({
    integer: 1,
    real: 2,
    text: 3,
    blob: 4,
    null: 5,
});

/** The result kinds a request may ask for, as the binary encoding numbers them. */
export const ResultKind = Object.freeze({
    nonQuery: 0,
    scalar: 1,
    reader: 2,
});

const RESULT_KIND_NAMES = Object.freeze(['nonQuery', 'scalar', 'reader']);

/** First byte of every binary message, so a stray buffer is rejected rather than misread. */
export const WIRE_MAGIC = 0xb5;

/** Bumped whenever the layout changes; the .NET decoder refuses anything else. */
export const WIRE_VERSION = 1;

const STATUS_OK = 0;
const STATUS_ERROR = 1;

/** Integers beyond this cannot be represented exactly as a JavaScript number. */
const MAX_EXACT_INTEGER = 9007199254740991n; // 2^53 - 1

const textEncoder = new TextEncoder();
const textDecoder = new TextDecoder('utf-8');

// -------------------------------------------------------------------------------------------------
// Binary primitives
// -------------------------------------------------------------------------------------------------

/**
 * A growable little-endian byte sink. Strings are UTF-8 with a 32-bit byte-length prefix, written
 * straight into the buffer with `encodeInto` so there is no intermediate array per string.
 */
export class BinaryWriter {
    #bytes: Uint8Array;
    #view: DataView;
    #length = 0;

    constructor(initialCapacity = 4096) {
        this.#bytes = new Uint8Array(initialCapacity);
        this.#view = new DataView(this.#bytes.buffer);
    }

    get length(): number {
        return this.#length;
    }

    #ensure(extra: number) {
        const needed = this.#length + extra;
        if (needed <= this.#bytes.length) {
            return;
        }

        let capacity = this.#bytes.length * 2;
        while (capacity < needed) {
            capacity *= 2;
        }

        const grown = new Uint8Array(capacity);
        grown.set(this.#bytes.subarray(0, this.#length));
        this.#bytes = grown;
        this.#view = new DataView(grown.buffer);
    }

    u8(value: number): void {
        this.#ensure(1);
        this.#bytes[this.#length++] = value;
    }

    u16(value: number): void {
        this.#ensure(2);
        this.#view.setUint16(this.#length, value, true);
        this.#length += 2;
    }

    i32(value: number): void {
        this.#ensure(4);
        this.#view.setInt32(this.#length, value, true);
        this.#length += 4;
    }

    u32(value: number): void {
        this.#ensure(4);
        this.#view.setUint32(this.#length, value, true);
        this.#length += 4;
    }

    i64(value: bigint): void {
        this.#ensure(8);
        this.#view.setBigInt64(this.#length, value, true);
        this.#length += 8;
    }

    f64(value: number): void {
        this.#ensure(8);
        this.#view.setFloat64(this.#length, value, true);
        this.#length += 8;
    }

    bytes(bytes: Uint8Array): void {
        this.#ensure(4 + bytes.byteLength);
        this.#view.setUint32(this.#length, bytes.byteLength, true);
        this.#length += 4;
        this.#bytes.set(bytes, this.#length);
        this.#length += bytes.byteLength;
    }

    string(text: string): void {
        // UTF-8 never needs more than three bytes per UTF-16 code unit.
        this.#ensure(4 + text.length * 3);
        const { written } = textEncoder.encodeInto(text, this.#bytes.subarray(this.#length + 4));
        this.#view.setUint32(this.#length, written, true);
        this.#length += 4 + written;
    }

    /** Reserves a 32-bit slot and returns its offset, for counts known only after the fact. */
    reserveU32(): number {
        const offset = this.#length;
        this.u32(0);
        return offset;
    }

    patchU32(offset: number, value: number): void {
        this.#view.setUint32(offset, value, true);
    }

    /** Reserves an i32 + i64 pair (records affected, last insert row id) for later patching. */
    reserveCounters(): number {
        const offset = this.#length;
        this.i32(0);
        this.i64(0n);
        return offset;
    }

    patchCounters(offset: number, recordsAffected: number, lastInsertRowId: bigint | number): void {
        this.#view.setInt32(offset, recordsAffected, true);
        this.#view.setBigInt64(offset + 4, BigInt(lastInsertRowId), true);
    }

    /** The written bytes as a view; the caller owns the result and the writer must not be reused. */
    finish(): Uint8Array {
        return this.#bytes.subarray(0, this.#length);
    }
}

/** A cursor over a little-endian buffer, the inverse of {@link BinaryWriter}. */
export class BinaryReader {
    #bytes: Uint8Array;
    #view: DataView;
    #offset = 0;

    constructor(bytes: Uint8Array) {
        this.#bytes = bytes;
        this.#view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    }

    get remaining(): number {
        return this.#bytes.byteLength - this.#offset;
    }

    #need(count: number) {
        if (this.#offset + count > this.#bytes.byteLength) {
            throw new Error(`Truncated wire message: needed ${count} more byte(s) at offset ${this.#offset}.`);
        }
    }

    u8(): number {
        this.#need(1);
        return this.#bytes[this.#offset++];
    }

    u16(): number {
        this.#need(2);
        const value = this.#view.getUint16(this.#offset, true);
        this.#offset += 2;
        return value;
    }

    i32(): number {
        this.#need(4);
        const value = this.#view.getInt32(this.#offset, true);
        this.#offset += 4;
        return value;
    }

    u32(): number {
        this.#need(4);
        const value = this.#view.getUint32(this.#offset, true);
        this.#offset += 4;
        return value;
    }

    i64(): bigint {
        this.#need(8);
        const value = this.#view.getBigInt64(this.#offset, true);
        this.#offset += 8;
        return value;
    }

    f64(): number {
        this.#need(8);
        const value = this.#view.getFloat64(this.#offset, true);
        this.#offset += 8;
        return value;
    }

    /** A copy, so the caller never aliases the message buffer. */
    bytes(): Uint8Array {
        const length = this.u32();
        this.#need(length);
        const value = this.#bytes.slice(this.#offset, this.#offset + length);
        this.#offset += length;
        return value;
    }

    string(): string {
        const length = this.u32();
        this.#need(length);
        const value = textDecoder.decode(this.#bytes.subarray(this.#offset, this.#offset + length));
        this.#offset += length;
        return value;
    }
}

// -------------------------------------------------------------------------------------------------
// Binary batch (request) encoding
// -------------------------------------------------------------------------------------------------

/**
 * Encodes a batch of tagged requests to the binary layout. Used by JavaScript callers that want the
 * transfer-friendly path and by tests that check the .NET encoder against this one.
 */
export function encodeBatchBinary(batch: ReadonlyArray<BatchRequest>): Uint8Array {
    const writer = new BinaryWriter();
    writer.u8(WIRE_MAGIC);
    writer.u8(WIRE_VERSION);
    writer.u32(batch.length);

    for (const request of batch) {
        writer.string(request.commandText ?? '');
        writer.u8(resultKindCode(request.resultKind));
        writer.i32(Number.isInteger(request.maxRows) && request.maxRows! >= 0 ? request.maxRows! : -1);

        const parameters = request.parameters ?? [];
        writer.u16(parameters.length);
        for (const parameter of parameters) {
            writer.string(parameter.name ?? '');
            writeTaggedValue(writer, parameter.type, parameter.value);
        }
    }

    return writer.finish();
}

/**
 * Decodes a binary batch into the tagged request shape the worker executes. Parameter values come
 * back already in the form the engine binds (BigInt for integers, Uint8Array for blobs).
 */
export function decodeBatchBinary(bytes: Uint8Array): BatchRequest[] {
    const reader = new BinaryReader(bytes);
    readHeader(reader, { withStatus: false });

    const count = reader.u32();
    const batch = new Array<BatchRequest>(count);

    for (let i = 0; i < count; i++) {
        const commandText = reader.string();
        const resultKind = RESULT_KIND_NAMES[reader.u8()] ?? 'nonQuery';
        const maxRows = reader.i32();
        const parameterCount = reader.u16();
        const parameters = new Array<TaggedParameter>(parameterCount);

        for (let p = 0; p < parameterCount; p++) {
            const name = reader.string();
            const type = reader.u8();
            parameters[p] = { name, type, value: readTaggedPayload(reader, type) };
        }

        batch[i] = { commandText, resultKind, maxRows: maxRows < 0 ? null : maxRows, parameters };
    }

    return batch;
}

// -------------------------------------------------------------------------------------------------
// Binary reply encoding
// -------------------------------------------------------------------------------------------------

/**
 * Writes the reply header and a success status. The worker then streams results through
 * {@link ResultWriter} so rows go from the statement into the buffer with no intermediate array.
 */
export function beginReply(writer: BinaryWriter): void {
    writer.u8(WIRE_MAGIC);
    writer.u8(WIRE_VERSION);
    writer.u8(STATUS_OK);
}

/** Encodes a failure as a whole reply, carrying the SQLite result code when there is one. */
export function encodeErrorReply({ message, sqliteCode, name }: { message?: string; sqliteCode?: number | null; name?: string }): Uint8Array {
    const writer = new BinaryWriter(256);
    writer.u8(WIRE_MAGIC);
    writer.u8(WIRE_VERSION);
    writer.u8(STATUS_ERROR);
    writer.string(message ?? 'The SQLite worker reported a failure.');
    writer.i32(Number.isInteger(sqliteCode) ? sqliteCode! : -1);
    writer.string(name ?? 'Error');
    return writer.finish();
}

/**
 * Serializes one result set directly from a live statement.
 *
 * Column names and storage classes are written first, then rows as they are stepped, then the
 * counters and row count are patched in. Encoding happens while the statement is live because the distinction
 * between INTEGER 1 and REAL 1.0 exists only there.
 */
export class ResultWriter {
    #writer: BinaryWriter;
    #countersOffset = 0;
    #rowCountOffset = 0;
    #rowCount = 0;
    #columnCount = 0;

    constructor(writer: BinaryWriter) {
        this.#writer = writer;
    }

    /** `storageClasses` holds the SQLite type code per column, or null when unknown. */
    columns(columnNames: string[], storageClasses: (number | null)[] | null): void {
        this.#columnCount = columnNames.length;
        this.#writer.u16(columnNames.length);
        for (let i = 0; i < columnNames.length; i++) {
            this.#writer.string(columnNames[i]);
            this.#writer.u8(storageClasses?.[i] ?? 0);
        }
    }

    /** Reserves the counters and row count, which are known only after every statement has run. */
    beginRows(): void {
        this.#countersOffset = this.#writer.reserveCounters();
        this.#rowCountOffset = this.#writer.reserveU32();
        this.#rowCount = 0;
    }

    /** Writes the current row of `stmt`. */
    row(sqlite3: SQLiteAPI, stmt: number): void {
        const writer = this.#writer;
        for (let i = 0; i < this.#columnCount; i++) {
            const type = sqlite3.column_type(stmt, i);
            writer.u8(type);

            switch (type) {
                case WireType.null:
                    break;

                case WireType.integer:
                    writer.i64(sqlite3.column_int64(stmt, i));
                    break;

                case WireType.real:
                    writer.f64(sqlite3.column_double(stmt, i));
                    break;

                case WireType.text:
                    writer.string(sqlite3.column_text(stmt, i));
                    break;

                case WireType.blob:
                    // Copied immediately: column_blob aliases WebAssembly memory, which the next engine call
                    // may move.
                    writer.bytes(sqlite3.column_blob(stmt, i));
                    break;

                default:
                    throw new Error(`Column ${i} has unknown SQLite type ${type}.`);
            }
        }

        this.#rowCount++;
    }

    finish(recordsAffected: number, lastInsertRowId: bigint | number): void {
        this.#writer.patchCounters(this.#countersOffset, recordsAffected, lastInsertRowId);
        this.#writer.patchU32(this.#rowCountOffset, this.#rowCount);
    }
}

/**
 * Decodes a binary reply into the tagged result shape, for JavaScript callers and tests. Throws
 * the carried error, with `sqliteCode` attached, when the reply is a failure.
 */
export function decodeReplyBinary(bytes: Uint8Array): DecodedResult[] {
    const reader = new BinaryReader(bytes);
    const status = readHeader(reader);

    if (status === STATUS_ERROR) {
        const message = reader.string();
        const sqliteCode = reader.i32();
        const name = reader.string();
        const error: WireError = new Error(message);
        error.name = name;
        if (sqliteCode >= 0) {
            error.sqliteCode = sqliteCode;
        }

        throw error;
    }

    const count = reader.u32();
    const results = new Array<DecodedResult>(count);

    for (let r = 0; r < count; r++) {
        const columnCount = reader.u16();
        const columnNames = new Array<string>(columnCount);
        const columnTypes = new Array<string | null>(columnCount);
        for (let c = 0; c < columnCount; c++) {
            columnNames[c] = reader.string();
            columnTypes[c] = STORAGE_CLASS_NAMES[reader.u8() as keyof typeof STORAGE_CLASS_NAMES] ?? null;
        }

        const recordsAffected = reader.i32();
        const lastInsertRowId = reader.i64();
        const rowCount = reader.u32();
        const rows = new Array<TaggedRow>(rowCount);

        for (let i = 0; i < rowCount; i++) {
            const t = new Array<number>(columnCount);
            const v = new Array<unknown>(columnCount);
            for (let c = 0; c < columnCount; c++) {
                const type = reader.u8();
                t[c] = type;
                v[c] = toTaggedValue(type, readTaggedPayload(reader, type));
            }

            rows[i] = { t, v };
        }

        results[r] = { columnNames, columnTypes, recordsAffected, lastInsertRowId, rows };
    }

    return results;
}

const STORAGE_CLASS_NAMES = Object.freeze({
    [WireType.integer]: 'INTEGER',
    [WireType.real]: 'REAL',
    [WireType.text]: 'TEXT',
    [WireType.blob]: 'BLOB',
});

function readHeader(reader: BinaryReader, { withStatus = true }: { withStatus?: boolean } = {}): number {
    const magic = reader.u8();
    if (magic !== WIRE_MAGIC) {
        throw new Error(`Not a Bsqlite wire message (leading byte 0x${magic.toString(16)}).`);
    }

    const version = reader.u8();
    if (version !== WIRE_VERSION) {
        throw new Error(`Unsupported wire version ${version}; this build speaks version ${WIRE_VERSION}.`);
    }

    return withStatus ? reader.u8() : STATUS_OK;
}

function resultKindCode(kind: string | number | undefined): number {
    if (typeof kind === 'number') {
        return kind;
    }

    const index = (RESULT_KIND_NAMES as readonly string[]).indexOf(kind ?? 'nonQuery');
    return index < 0 ? ResultKind.nonQuery : index;
}

function writeTaggedValue(writer: BinaryWriter, type: number, value: unknown): void {
    writer.u8(type);
    switch (type) {
        case WireType.null:
            return;

        case WireType.integer:
            writer.i64(typeof value === 'bigint' ? value : BigInt(typeof value === 'string' ? value : Math.trunc(value as number)));
            return;

        // Infinity may arrive as the string the tagged encoding spells it with.
        case WireType.real:
            writer.f64(typeof value === 'string' ? Number(value) : value as number);
            return;

        case WireType.text:
            writer.string(value as string);
            return;

        case WireType.blob:
            writer.bytes(value instanceof Uint8Array ? value : fromBase64(value as string));
            return;

        default:
            throw new Error(`Cannot encode unknown wire type ${type}.`);
    }
}

/** Reads a value payload for `type`; integers come back as BigInt, blobs as Uint8Array. */
function readTaggedPayload(reader: BinaryReader, type: number): bigint | number | string | Uint8Array | null {
    switch (type) {
        case WireType.null:
            return null;

        case WireType.integer:
            return reader.i64();

        case WireType.real:
            return reader.f64();

        case WireType.text:
            return reader.string();

        case WireType.blob:
            return reader.bytes();

        default:
            throw new Error(`Unknown wire type ${type} in message.`);
    }
}

/** Converts an engine-form value into the tagged (JSON-safe) representation. */
function toTaggedValue(type: number, value: unknown): unknown {
    switch (type) {
        case WireType.integer: {
            const integer = value as bigint;
            return integer > MAX_EXACT_INTEGER || integer < -MAX_EXACT_INTEGER ? String(integer) : Number(integer);
        }

        case WireType.blob:
            return toBase64(value as Uint8Array);

        default:
            return value;
    }
}

// -------------------------------------------------------------------------------------------------
// Tagged (JSON-safe) encoding, for JavaScript callers
// -------------------------------------------------------------------------------------------------

/**
 * Reads the current row of a live statement into tagged form. `columnCount` comes from
 * `column_count`, so a row of no columns is still well-formed.
 */
export function encodeRow(sqlite3: SQLiteAPI, stmt: number, columnCount: number): TaggedRow {
    const t = new Array<number>(columnCount);
    const v = new Array<unknown>(columnCount);

    for (let i = 0; i < columnCount; i++) {
        const type = sqlite3.column_type(stmt, i);
        t[i] = type;

        switch (type) {
            case WireType.null:
                v[i] = null;
                break;

            case WireType.integer: {
                // wa-sqlite hands back a number when the value fits exactly and a BigInt when it does not,
                // which is the same boundary this format cares about.
                const value = sqlite3.column(stmt, i);
                v[i] = typeof value === 'bigint' ? String(value) : value;
                break;
            }

            case WireType.real: {
                // JSON has no spelling for Infinity - JSON.stringify writes null - so it travels as the
                // string Number() and double.Parse both read back. NaN never reaches here: SQLite stores
                // it as NULL.
                const value = sqlite3.column_double(stmt, i);
                v[i] = Number.isFinite(value) ? value : String(value);
                break;
            }

            case WireType.text:
                v[i] = sqlite3.column_text(stmt, i);
                break;

            case WireType.blob:
                // Encoded immediately: column_blob can alias WebAssembly memory, which the next engine call
                // may move.
                v[i] = toBase64(sqlite3.column_blob(stmt, i));
                break;

            default:
                throw new Error(`Column ${i} has unknown SQLite type ${type}.`);
        }
    }

    return { t, v };
}

/** Turns a tagged parameter back into the value the engine binds. */
export function decodeParameter({ type, value }: { type: number; value: unknown }): SQLiteCompatibleType {
    switch (type) {
        case WireType.null:
            return null;

        // A large integer arrives as a decimal string; BigInt is what makes the engine bind it as int64.
        case WireType.integer:
            return typeof value === 'string' ? BigInt(value) : value as number | bigint;

        // Infinity arrives as a string, JSON having no spelling for it.
        case WireType.real:
            return typeof value === 'string' ? Number(value) : value as number;

        case WireType.text:
            return value as string;

        case WireType.blob:
            return value instanceof Uint8Array ? value : fromBase64(value as string);

        default:
            throw new Error(`A parameter arrived with unknown wire type ${type}.`);
    }
}

/** Tags a plain JavaScript value, for callers driving the library from JavaScript rather than .NET. */
export function encodeValue(value: unknown): { type: number; value: unknown } {
    if (value === null || value === undefined) {
        return { type: WireType.null, value: null };
    }

    switch (typeof value) {
        case 'boolean':
            return { type: WireType.integer, value: value ? 1 : 0 };

        case 'number':
            if (Number.isNaN(value)) {
                // SQLite stores NaN as NULL, so it is sent as one.
                return { type: WireType.null, value: null };
            }

            if (Number.isInteger(value)) {
                return { type: WireType.integer, value };
            }

            return { type: WireType.real, value: Number.isFinite(value) ? value : String(value) };

        case 'bigint':
            return {
                type: WireType.integer,
                value: value > MAX_EXACT_INTEGER || value < -MAX_EXACT_INTEGER ? String(value) : Number(value),
            };

        case 'string':
            return { type: WireType.text, value };

        default:
            if (value instanceof Uint8Array) {
                return { type: WireType.blob, value: toBase64(value) };
            }

            throw new Error(`Cannot put a value of type ${typeof value} on the wire.`);
    }
}

/** Untags a value, giving back a `bigint` only where a number would lose precision. */
export function decodeValue(type: number, value: unknown): unknown {
    switch (type) {
        case WireType.null:
            return null;

        case WireType.integer:
            return typeof value === 'string' ? BigInt(value) : value;

        case WireType.real:
            return typeof value === 'string' ? Number(value) : value;

        case WireType.blob:
            return fromBase64(value as string);

        default:
            return value;
    }
}

// Chunked because spreading a large array into String.fromCharCode overflows the argument limit, which
// shows up only once a blob gets big - exactly where it is least welcome.
const CHUNK = 0x8000;

export function toBase64(bytes: Uint8Array): string {
    let binary = '';
    for (let i = 0; i < bytes.length; i += CHUNK) {
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + CHUNK) as unknown as number[]);
    }

    return btoa(binary);
}

export function fromBase64(text: string): Uint8Array {
    const binary = atob(text);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
}
