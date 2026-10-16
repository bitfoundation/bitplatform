// A minimal protobuf writer: just the wire types OTLP needs. 64-bit values are handled as two uint32 halves
// because the ES2019 target has no BigInt literals and Numbers lose precision above 2^53 (unix nanos are ~2^60).

export const WireType = { Varint: 0, Fixed64: 1, LengthDelimited: 2, Fixed32: 5 } as const;

const TWO_32 = 4294967296;

let utf8: TextEncoder | null = null;

export function encodeUtf8(text: string): Uint8Array {
    return (utf8 || (utf8 = new TextEncoder())).encode(text);
}

/** Splits a safe integer (possibly negative) into the low and high uint32 halves of its 64-bit two's complement. */
export function splitInt64(value: number): [number, number] {
    const negative = value < 0;
    const abs = Math.abs(value);
    let lo = abs % TWO_32 >>> 0;
    let hi = Math.floor(abs / TWO_32) >>> 0;
    if (negative) {
        lo = (~lo + 1) >>> 0;
        hi = (~hi + (lo === 0 ? 1 : 0)) >>> 0;
    }
    return [lo, hi];
}

/** Epoch milliseconds (fractional) to unix nanoseconds as [lo, hi] uint32 halves, with microsecond precision. */
export function msToNanos(ms: number): [number, number] {
    const micros = Math.max(0, Math.round(ms * 1000));
    const microsLo = micros % TWO_32;
    const microsHi = Math.floor(micros / TWO_32);
    const lo = microsLo * 1000;
    return [lo % TWO_32 >>> 0, (microsHi * 1000 + Math.floor(lo / TWO_32)) >>> 0];
}

export class ProtobufWriter {
    private buffer = new Uint8Array(256);
    private view = new DataView(this.buffer.buffer);
    private pos = 0;

    private ensure(extra: number): void {
        if (this.pos + extra <= this.buffer.length) return;
        let size = this.buffer.length * 2;
        while (size < this.pos + extra) size *= 2;
        const next = new Uint8Array(size);
        next.set(this.buffer.subarray(0, this.pos));
        this.buffer = next;
        this.view = new DataView(next.buffer);
    }

    get length(): number {
        return this.pos;
    }

    finish(): Uint8Array {
        return this.buffer.slice(0, this.pos);
    }

    tag(field: number, wireType: number): this {
        return this.uint32(((field << 3) | wireType) >>> 0);
    }

    /** A varint of an unsigned 32-bit value. */
    uint32(value: number): this {
        this.ensure(5);
        value >>>= 0;
        while (value > 127) {
            this.buffer[this.pos++] = (value & 127) | 128;
            value >>>= 7;
        }
        this.buffer[this.pos++] = value;
        return this;
    }

    /** A varint of a 64-bit value given as uint32 halves. */
    varint64(lo: number, hi: number): this {
        this.ensure(10);
        lo >>>= 0;
        hi >>>= 0;
        while (hi > 0 || lo > 127) {
            this.buffer[this.pos++] = (lo & 127) | 128;
            lo = ((lo >>> 7) | (hi << 25)) >>> 0;
            hi >>>= 7;
        }
        this.buffer[this.pos++] = lo;
        return this;
    }

    /** A varint of a safe integer as int64 (negative values take 10 bytes, as protobuf requires). */
    int64(value: number): this {
        const [lo, hi] = splitInt64(value);
        return this.varint64(lo, hi);
    }

    fixed32(value: number): this {
        this.ensure(4);
        this.view.setUint32(this.pos, value >>> 0, true);
        this.pos += 4;
        return this;
    }

    fixed64(lo: number, hi: number): this {
        this.ensure(8);
        this.view.setUint32(this.pos, lo >>> 0, true);
        this.view.setUint32(this.pos + 4, hi >>> 0, true);
        this.pos += 8;
        return this;
    }

    double(value: number): this {
        this.ensure(8);
        this.view.setFloat64(this.pos, value, true);
        this.pos += 8;
        return this;
    }

    raw(bytes: Uint8Array): this {
        this.ensure(bytes.length);
        this.buffer.set(bytes, this.pos);
        this.pos += bytes.length;
        return this;
    }

    // Field helpers (tag + value).

    uint32Field(field: number, value: number): this {
        return this.tag(field, WireType.Varint).uint32(value);
    }

    int64Field(field: number, value: number): this {
        return this.tag(field, WireType.Varint).int64(value);
    }

    boolField(field: number, value: boolean): this {
        return this.tag(field, WireType.Varint).uint32(value ? 1 : 0);
    }

    fixed32Field(field: number, value: number): this {
        return this.tag(field, WireType.Fixed32).fixed32(value);
    }

    fixed64Field(field: number, lo: number, hi: number): this {
        return this.tag(field, WireType.Fixed64).fixed64(lo, hi);
    }

    doubleField(field: number, value: number): this {
        return this.tag(field, WireType.Fixed64).double(value);
    }

    bytesField(field: number, bytes: Uint8Array): this {
        return this.tag(field, WireType.LengthDelimited).uint32(bytes.length).raw(bytes);
    }

    stringField(field: number, value: string): this {
        return this.bytesField(field, encodeUtf8(value));
    }

    /** A nested message: written into a child writer, then copied with its length prefix. */
    messageField(field: number, write: (writer: ProtobufWriter) => void): this {
        const child = new ProtobufWriter();
        write(child);
        return this.bytesField(field, child.finish());
    }

    packedVarint(field: number, values: number[]): this {
        const child = new ProtobufWriter();
        for (const value of values) child.int64(value);
        return this.bytesField(field, child.finish());
    }

    packedFixed64(field: number, values: [number, number][]): this {
        const child = new ProtobufWriter();
        for (const [lo, hi] of values) child.fixed64(lo, hi);
        return this.bytesField(field, child.finish());
    }

    packedDouble(field: number, values: number[]): this {
        const child = new ProtobufWriter();
        for (const value of values) child.double(value);
        return this.bytesField(field, child.finish());
    }
}

/** Decodes a lowercase/uppercase hex string into bytes. */
export function hexToBytes(hex: string): Uint8Array {
    const bytes = new Uint8Array(hex.length >> 1);
    for (let i = 0; i < bytes.length; i++) bytes[i] = parseInt(hex.substr(i * 2, 2), 16);
    return bytes;
}
