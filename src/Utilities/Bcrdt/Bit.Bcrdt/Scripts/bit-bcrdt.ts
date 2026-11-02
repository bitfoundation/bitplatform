// Text control bridge for the collaborative input components, and the browser store.
//
// The CRDT engine, the synchronization protocol, the presence system and every data structure are pure C# and never
// call into here. This module exists for the two things .NET cannot reach in a browser: a text control's caret and
// value - read, and updated without overwriting what the user typed a moment ago - and IndexedDB.
//
// Positions are UTF-16 code units throughout, which is what both the browser's selection API and .NET strings use.

type TextControl = HTMLTextAreaElement | HTMLInputElement;

interface DotNetReference {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface ControlState {
    ref: DotNetReference;
    generation: number;
    // What .NET believes the control holds: the value it rendered or last set, plus every edit reported since. Edits
    // are reported as changes to it, so a keystroke costs a few bytes on the wire however long the text is.
    known: string;
    // How many edits have been reported for the current field. .NET quotes the count it had received when it built
    // an update; a different count means the user typed in between, and the update is refused.
    inputs: number;
    start: number;
    end: number;
    backward: boolean;
    applied: number;
    composing: boolean;
    onInput: (event?: Event) => void;
    onSelection: () => void;
    onDocumentSelection: () => void;
    onBlur: () => void;
    onCompositionStart: () => void;
    onCompositionEnd: () => void;
}

// What setValue says: applied; refused because the user typed since the update was built; refused because an input
// method is composing - in which case .NET waits to hear that the composition ended instead of retrying; or refused
// because the control does not hold what .NET believes it does, in which case .NET reads the value back.
const Applied = 1;
const Stale = 0;
const Composing = -1;
const Drifted = 2;

// The most an edit inserts and still goes to .NET as a change. One message from the browser larger than Blazor Server's
// limit (32 KB by default) drops the connection, and a character can take six bytes as JSON. A larger edit - a big
// paste - has .NET read the whole value back instead, as a stream, which Blazor delivers in chunks.
const MaxInlineEdit = 4096;

// readValue's header: inputs, selection start, selection end, backward and the last applied update, as float64s.
const ValueHeaderSize = 40;

const states = new WeakMap<TextControl, ControlState>();
const selectionEvents = ['select', 'keyup', 'mouseup', 'focus'];

// The smallest single change turning `before` into `after`: everything outside it is shared. Never splits a surrogate
// pair, so the inserted text is always a well-formed string.
function diff(before: string, after: string): { start: number; removed: number; inserted: string } {
    const limit = Math.min(before.length, after.length);
    let prefix = 0;
    while (prefix < limit && before.charCodeAt(prefix) === after.charCodeAt(prefix)) prefix++;
    if (prefix > 0 && isHighSurrogate(before.charCodeAt(prefix - 1))) prefix--;

    let suffix = 0;
    while (suffix < limit - prefix &&
        before.charCodeAt(before.length - 1 - suffix) === after.charCodeAt(after.length - 1 - suffix)) suffix++;
    if (suffix > 0 && isLowSurrogate(before.charCodeAt(before.length - suffix))) suffix--;

    return { start: prefix, removed: before.length - prefix - suffix, inserted: after.slice(prefix, after.length - suffix) };
}

function isHighSurrogate(code: number): boolean { return code >= 0xD800 && code <= 0xDBFF; }
function isLowSurrogate(code: number): boolean { return code >= 0xDC00 && code <= 0xDFFF; }

// Starts reporting a control's input and selection to .NET.
// `generation` identifies which text field the control is bound to; every report carries it, so .NET can discard
// reports meant for a field it has since been rebound away from. `value` is what .NET rendered into the control. If
// the control holds something else, the user typed between the render and now; that is reported as input rather than
// overwritten.
export function attach(element: TextControl | null, dotnetRef: DotNetReference, generation: number, value: string): void {
    if (!element) return;
    detach(element);

    const send = (method: string, ...args: unknown[]) => {
        try {
            const pending = state.ref.invokeMethodAsync(method, state.generation, ...args);
            if (pending && typeof pending.catch === 'function') pending.catch(() => { });
        } catch {
            // The .NET side has gone away; there is nobody left to tell.
        }
    };

    const read = () => {
        state.start = element.selectionStart ?? 0;
        state.end = element.selectionEnd ?? 0;
        state.backward = element.selectionDirection === 'backward';
    };

    const reportSelection = (force: boolean) => {
        const start = element.selectionStart ?? 0;
        const end = element.selectionEnd ?? 0;
        const backward = element.selectionDirection === 'backward';
        if (!force && start === state.start && end === state.end && backward === state.backward) return;
        read();
        send('OnControlSelectionChanged', state.start, state.end, state.backward, state.applied);
    };

    // `applied` is the identifier of the last update from .NET this control applied. It travels with every report, so
    // .NET can settle an update whose answer was lost on the way back.
    const state: ControlState = {
        ref: dotnetRef,
        generation,
        known: value,
        inputs: 0,
        start: -1,
        end: -1,
        backward: false,
        applied: 0,
        composing: false,

        onInput: (event?: Event) => {
            // While an input method is composing, the value holds provisional text that the next keystroke may
            // replace. The settled value is reported when the composition ends.
            if (state.composing || (event && (event as InputEvent).isComposing)) return;

            const current = element.value;
            if (current === state.known) {
                reportSelection(false);
                return;
            }

            const change = diff(state.known, current);
            if (change.inserted.length > MaxInlineEdit) {
                // Nothing moves on here: the read records what it took, and an edit reported after it is a change
                // to that. Until then every further keystroke asks again, so a request lost on the way is repeated.
                send('OnControlEditTooLarge', state.inputs);
                return;
            }

            state.known = current;
            state.inputs++;
            read();
            send('OnControlEdit', change.start, change.removed, change.inserted, state.start, state.end, state.backward, state.applied, state.inputs);
        },

        onSelection: () => reportSelection(false),

        // Caret moves that raise none of the element's own events - a held arrow key, a drag, a touch handle - still
        // raise this one, on the document. Keeping the reported caret current is what lets an incoming change put it
        // back where the user actually is.
        onDocumentSelection: () => {
            if (document.activeElement === element && !state.composing) reportSelection(false);
        },

        onBlur: () => {
            state.start = -1;
            state.end = -1;
            send('OnControlBlur');
        },

        onCompositionStart: () => {
            state.composing = true;
        },

        onCompositionEnd: () => {
            state.composing = false;
            // Some browsers follow this with an input event and some do not; reporting here covers both, and a
            // repeated report of the same value changes nothing. .NET held back its updates while the composition
            // lasted, so it hears from the control either way.
            const before = state.inputs;
            state.onInput();
            if (state.inputs === before) reportSelection(true);
        },
    };

    element.addEventListener('input', state.onInput);
    for (const type of selectionEvents) element.addEventListener(type, state.onSelection);
    document.addEventListener('selectionchange', state.onDocumentSelection);
    element.addEventListener('blur', state.onBlur);
    element.addEventListener('compositionstart', state.onCompositionStart);
    element.addEventListener('compositionend', state.onCompositionEnd);

    states.set(element, state);

    if (element.value !== value) {
        // Reported after this call returns, so .NET is not re-entered while it waits on it.
        queueMicrotask(() => {
            if (states.get(element) === state) state.onInput();
        });
    }
}

// Rebinds an attached control to a different text field and shows that field's value. Unlike attaching, this
// replaces the content: whatever was there belonged to the previous field.
export function reset(element: TextControl | null, generation: number, value: string): void {
    const state = element ? states.get(element) : undefined;
    if (!element || !state) return;
    state.generation = generation;
    state.applied = 0;
    state.inputs = 0;
    state.known = value;
    element.value = value;
}

// Reads the control's whole value back, for when .NET's idea of it has drifted - a report lost on the way, or an edit
// too large to report as a change. The value becomes the shared starting point again: counted as an input, so an update
// built before it is refused, and what later edits are changes to.
//
// .NET asks for the result as a stream reference, which Blazor makes of the array itself, so a long text reaches .NET in
// chunks and never as one oversized message. Layout: the header - inputs, selection start, selection end, backward (0 or
// 1) and the last applied update, each a little-endian float64 - then the value as UTF-16 code units, little-endian, which
// keeps even a lone surrogate exactly. Never empty, which Blazor would refuse to stream: a control no longer bound to
// `generation` answers the header alone, with inputs -1.
export function readValue(element: TextControl | null, generation: number): Uint8Array {
    const state = element ? states.get(element) : undefined;
    if (!element || !state || state.generation !== generation) {
        const unbound = new Uint8Array(ValueHeaderSize);
        new DataView(unbound.buffer).setFloat64(0, -1, true);
        return unbound;
    }

    const value = element.value;
    state.known = value;
    state.inputs++;
    state.start = element.selectionStart ?? 0;
    state.end = element.selectionEnd ?? 0;
    state.backward = element.selectionDirection === 'backward';

    const bytes = new Uint8Array(ValueHeaderSize + value.length * 2);
    const view = new DataView(bytes.buffer);
    view.setFloat64(0, state.inputs, true);
    view.setFloat64(8, state.start, true);
    view.setFloat64(16, state.end, true);
    view.setFloat64(24, state.backward ? 1 : 0, true);
    view.setFloat64(32, state.applied, true);
    for (let i = 0; i < value.length; i++) view.setUint16(ValueHeaderSize + i * 2, value.charCodeAt(i), true);
    return bytes;
}

// Applies a change .NET computed - but only if the control still holds exactly what .NET built it against, and no
// input method is composing. `inputs` is the number of edits .NET had received; if the control has reported more, the
// user typed since, that input reaches .NET first, and the update is refused rather than allowed to overwrite it.
// The content, its length and the replaced text are checked as well, so a control that drifted is never patched
// blindly: that refusal says so, and .NET reads the value back. `id` is recorded when the update is applied and
// reported back with every later report.
export function setValue(
    element: TextControl | null,
    generation: number,
    inputs: number,
    expectedLength: number,
    start: number,
    removed: string,
    inserted: string,
    selectionStart: number,
    selectionEnd: number,
    id: number): number {
    const state = element ? states.get(element) : undefined;
    if (!element || !state || state.generation !== generation) return Stale;
    if (state.composing) return Composing;

    if (state.inputs !== inputs) return Stale;

    const current = element.value;
    if (current !== state.known || current.length !== expectedLength) return Drifted;
    if (current.substr(start, removed.length) !== removed) return Drifted;

    const value = current.slice(0, start) + inserted + current.slice(start + removed.length);
    const focused = document.activeElement === element;

    // Where the caret goes: .NET's mapping of the caret the control last reported, unless the caret has moved since
    // in a way nothing reported - then the caret as it is now, carried across this change.
    let from = selectionStart;
    let to = selectionEnd;
    const caretStart = element.selectionStart ?? 0;
    const caretEnd = element.selectionEnd ?? 0;
    if (focused && (caretStart !== state.start || caretEnd !== state.end)) {
        from = mapThrough(caretStart, start, removed.length, inserted.length);
        to = mapThrough(caretEnd, start, removed.length, inserted.length);
    }

    element.value = value;
    state.known = value;
    if (focused) {
        const length = value.length;
        from = Math.max(0, Math.min(from, length));
        to = Math.max(from, Math.min(to, length));
        element.setSelectionRange(from, to, state.backward ? 'backward' : 'forward');
        state.start = from;
        state.end = to;
    }

    state.applied = id;
    return Applied;
}

// Carries a position across the replacement of `removed` characters at `start` by `inserted` characters.
function mapThrough(position: number, start: number, removed: number, inserted: number): number {
    if (position <= start) return position;
    if (position >= start + removed) return position - removed + inserted;
    return start + inserted;
}

// The identifier of the last update the control applied, for settling one whose answer was lost. Zero if it has
// applied none; -1 if the control is no longer bound to `generation`.
export function appliedUpdate(element: TextControl | null, generation: number): number {
    const state = element ? states.get(element) : undefined;
    if (!state || state.generation !== generation) return -1;
    return state.applied;
}

// ---------------------------------------------------------------------------------------------------------------------
// IndexedDB store
//
// One object store of records `{ document, data }` under auto-incremented keys, indexed by document. The store knows
// nothing about what the bytes mean: .NET merges them. Every write is a transaction with strict durability, so it is
// reported done only once committed to disk.
// ---------------------------------------------------------------------------------------------------------------------

interface StoredRecord {
    document: string;
    data: Uint8Array;
}

const RecordStore = 'updates';
const DocumentIndex = 'document';
const FrameFormat = 1;
const databases = new Map<string, Promise<IDBDatabase>>();

function openDatabase(name: string): Promise<IDBDatabase> {
    let pending = databases.get(name);
    if (pending) return pending;

    pending = new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open(name, 1);
        request.onupgradeneeded = () => {
            const store = request.result.createObjectStore(RecordStore, { autoIncrement: true });
            store.createIndex(DocumentIndex, 'document', { unique: false });
        };
        request.onsuccess = () => {
            const database = request.result;
            // Another tab upgrading the schema: step aside, and reopen on the next call.
            database.onversionchange = () => {
                database.close();
                databases.delete(name);
            };
            database.onclose = () => databases.delete(name);
            resolve(database);
        };
        request.onerror = () => {
            databases.delete(name);
            reject(request.error);
        };
    });

    databases.set(name, pending);
    return pending;
}

function completion(transaction: IDBTransaction): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onerror = () => reject(transaction.error);
        transaction.onabort = () => reject(transaction.error ?? new Error('The IndexedDB transaction was aborted.'));
    });
}

function write(database: IDBDatabase): IDBTransaction {
    return database.transaction(RecordStore, 'readwrite', { durability: 'strict' });
}

// Appends one record. A copy is stored, so nothing the caller does to its buffer afterwards reaches the database.
export async function idbAppend(name: string, documentId: string, data: Uint8Array): Promise<void> {
    const database = await openDatabase(name);
    const transaction = write(database);
    const record: StoredRecord = { document: documentId, data: new Uint8Array(data) };
    transaction.objectStore(RecordStore).add(record);
    await completion(transaction);
}

// Every record of a document with its key: a format byte, then [float64 key][uint32 length][bytes] each - nothing more when the
// document has none. .NET asks for the result as a stream reference, which Blazor makes of the array itself, so a large
// document reaches .NET in chunks and never as one oversized interop message. It must never be null: Blazor would try
// to stream that too.
export async function idbLoad(name: string, documentId: string): Promise<Uint8Array> {
    const database = await openDatabase(name);
    const transaction = database.transaction(RecordStore, 'readonly');
    const index = transaction.objectStore(RecordStore).index(DocumentIndex);

    const records: { key: number; data: Uint8Array }[] = [];
    await new Promise<void>((resolve, reject) => {
        const request = index.openCursor(IDBKeyRange.only(documentId));
        request.onsuccess = () => {
            const cursor = request.result;
            if (!cursor) {
                resolve();
                return;
            }
            records.push({ key: cursor.primaryKey as number, data: (cursor.value as StoredRecord).data });
            cursor.continue();
        };
        request.onerror = () => reject(request.error);
    });

    // A leading format byte: it versions the framing, and it keeps the array from ever being empty - Blazor refuses to
    // stream nothing.
    let size = 1;
    for (const record of records) size += 12 + record.data.length;

    const framed = new Uint8Array(size);
    const view = new DataView(framed.buffer);
    framed[0] = FrameFormat;
    let offset = 1;
    for (const record of records) {
        view.setFloat64(offset, record.key, true);
        view.setUint32(offset + 8, record.data.length, true);
        framed.set(record.data, offset + 12);
        offset += 12 + record.data.length;
    }

    return framed;
}

// Replaces exactly the records that were read with their merge, in one transaction. A record added since the read is
// left alone, whoever added it.
export async function idbReplace(name: string, documentId: string, keys: number[], merged: Uint8Array): Promise<void> {
    const database = await openDatabase(name);
    const transaction = write(database);
    const store = transaction.objectStore(RecordStore);
    for (const key of keys) store.delete(key);
    const record: StoredRecord = { document: documentId, data: new Uint8Array(merged) };
    store.add(record);
    await completion(transaction);
}

export async function idbDelete(name: string, documentId: string): Promise<void> {
    const database = await openDatabase(name);
    const transaction = write(database);
    const request = transaction.objectStore(RecordStore).index(DocumentIndex).openKeyCursor(IDBKeyRange.only(documentId));
    const store = transaction.objectStore(RecordStore);
    request.onsuccess = () => {
        const cursor = request.result;
        if (!cursor) return;
        store.delete(cursor.primaryKey);
        cursor.continue();
    };
    await completion(transaction);
}

export async function idbList(name: string): Promise<string[]> {
    const database = await openDatabase(name);
    const transaction = database.transaction(RecordStore, 'readonly');
    const ids: string[] = [];
    await new Promise<void>((resolve, reject) => {
        const request = transaction.objectStore(RecordStore).index(DocumentIndex).openKeyCursor(null, 'nextunique');
        request.onsuccess = () => {
            const cursor = request.result;
            if (!cursor) {
                resolve();
                return;
            }
            ids.push(cursor.key as string);
            cursor.continue();
        };
        request.onerror = () => reject(request.error);
    });
    return ids;
}

// Stops reporting a control's input. Safe to call for a control that was never attached.
export function detach(element: TextControl | null): void {
    const state = element ? states.get(element) : undefined;
    if (!element || !state) return;

    element.removeEventListener('input', state.onInput);
    for (const type of selectionEvents) element.removeEventListener(type, state.onSelection);
    document.removeEventListener('selectionchange', state.onDocumentSelection);
    element.removeEventListener('blur', state.onBlur);
    element.removeEventListener('compositionstart', state.onCompositionStart);
    element.removeEventListener('compositionend', state.onCompositionEnd);
    states.delete(element);
}
