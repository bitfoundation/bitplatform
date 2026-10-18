// Raises the WebAssembly function-table maximum on a copy of a wa-sqlite build.
//
// The vendored engine is compiled without ALLOW_TABLE_GROWTH, so the table's
// minimum and maximum are the same (436). Emscripten's `addFunction` then cannot
// install a JS collation callback, and `EF_DECIMAL` never exists. Patching a copy
// at load time - not the checksummed artifact - gives `addFunction` spare slots
// without a from-source rebuild.

const TABLE_SECTION = 4;
const FUNCREF = 0x70;
const LIMITS_MIN_MAX = 1;

interface WasmSection {
    id: number;
    payload: Uint8Array;
}

/**
 * Returns a copy of a wa-sqlite `.wasm` with a higher table maximum; the input is not mutated.
 * `extraSlots` is how many function pointers `addFunction` may take.
 */
export function raiseFunctionTableLimit(bytes: Uint8Array, extraSlots = 16): Uint8Array {
    if (!(bytes instanceof Uint8Array)) {
        throw new Error('raiseFunctionTableLimit expects the WASM bytes.');
    }

    if (!Number.isInteger(extraSlots) || extraSlots < 1) {
        throw new Error('extraSlots must be a positive integer.');
    }

    const sections = parseSections(bytes);
    const table = sections.find(section => section.id === TABLE_SECTION);

    if (!table) {
        throw new Error('The WASM module has no function table to grow.');
    }

    const patched = patchTablePayload(table.payload, extraSlots);
    if (patched === table.payload) {
        return bytes;
    }

    return rebuild(bytes.subarray(0, 8), sections.map(section => (
        section.id === TABLE_SECTION ? { ...section, payload: patched } : section
    )));
}

function parseSections(bytes: Uint8Array): WasmSection[] {
    if (bytes.length < 8 || bytes[0] !== 0 || bytes[1] !== 0x61 || bytes[2] !== 0x73 || bytes[3] !== 0x6d) {
        throw new Error('Not a WebAssembly module.');
    }

    const sections: WasmSection[] = [];
    let offset = 8;

    while (offset < bytes.length) {
        const id = bytes[offset++];
        const size = readLeb(bytes, offset);
        offset = size.next;
        sections.push({ id, payload: bytes.subarray(offset, offset + size.value) });
        offset += size.value;
    }

    return sections;
}

function patchTablePayload(payload: Uint8Array, extraSlots: number): Uint8Array {
    let offset = 0;
    const count = readLeb(payload, offset);
    offset = count.next;

    if (count.value !== 1) {
        throw new Error(`Expected one function table, found ${count.value}.`);
    }

    const elemType = payload[offset++];
    if (elemType !== FUNCREF) {
        throw new Error(`Unexpected table element type 0x${elemType.toString(16)}.`);
    }

    const limits = payload[offset++];
    const min = readLeb(payload, offset);
    offset = min.next;

    if (limits !== LIMITS_MIN_MAX) {
        // Already unbounded - addFunction can grow it.
        return payload;
    }

    const max = readLeb(payload, offset);
    const needed = min.value + extraSlots;

    if (max.value >= needed) {
        return payload;
    }

    return Uint8Array.from([
        ...encodeLeb(1),
        FUNCREF,
        LIMITS_MIN_MAX,
        ...encodeLeb(min.value),
        ...encodeLeb(needed),
    ]);
}

function rebuild(header: Uint8Array, sections: WasmSection[]): Uint8Array {
    const parts: Uint8Array[] = [header];
    let length = header.length;

    for (const section of sections) {
        const size = Uint8Array.from(encodeLeb(section.payload.length));
        parts.push(Uint8Array.of(section.id), size, section.payload);
        length += 1 + size.length + section.payload.length;
    }

    const out = new Uint8Array(length);
    let offset = 0;
    for (const part of parts) {
        out.set(part, offset);
        offset += part.length;
    }

    return out;
}

function readLeb(bytes: Uint8Array, offset: number): { value: number; next: number } {
    let value = 0;
    let shift = 0;
    let next = offset;

    while (next < bytes.length) {
        const byte = bytes[next++];
        value |= (byte & 0x7f) << shift;
        if ((byte & 0x80) === 0) {
            return { value: value >>> 0, next };
        }

        shift += 7;
        if (shift > 28) {
            throw new Error('LEB128 value is larger than expected for a table limit.');
        }
    }

    throw new Error('Truncated LEB128.');
}

function encodeLeb(value: number): number[] {
    const bytes: number[] = [];
    let n = value >>> 0;

    while (n > 0x7f) {
        bytes.push((n & 0x7f) | 0x80);
        n >>>= 7;
    }

    bytes.push(n);
    return bytes;
}
