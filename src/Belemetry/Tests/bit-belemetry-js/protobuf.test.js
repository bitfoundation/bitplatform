import { describe, expect, it } from 'vitest';
import { msToNanos, ProtobufWriter, splitInt64, hexToBytes } from '../../Bit.Belemetry/Scripts/exporters/protobuf.ts';
import { msToNanosString } from '../../Bit.Belemetry/Scripts/exporters/otlp-common.ts';
import { decode, f64, one, str, u32, u64 } from './helpers.js';

const bytes = w => Array.from(w.finish());

describe('ProtobufWriter primitives', () => {
    it('writes varints', () => {
        expect(bytes(new ProtobufWriter().uint32(0))).toEqual([0]);
        expect(bytes(new ProtobufWriter().uint32(1))).toEqual([1]);
        expect(bytes(new ProtobufWriter().uint32(300))).toEqual([0xac, 0x02]);
        expect(bytes(new ProtobufWriter().uint32(0xffffffff))).toEqual([0xff, 0xff, 0xff, 0xff, 0x0f]);
    });

    it('writes int64 varints, including values above 2^32 and negatives as 10 bytes', () => {
        expect(bytes(new ProtobufWriter().int64(2 ** 40))).toEqual([0x80, 0x80, 0x80, 0x80, 0x80, 0x20]);
        expect(bytes(new ProtobufWriter().int64(-1))).toEqual([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01]);
        const decoded = decode(new ProtobufWriter().int64Field(3, -123456789012).finish());
        expect(BigInt.asIntN(64, decoded[0].value)).toBe(-123456789012n);
        expect(decode(new ProtobufWriter().int64Field(3, Number.MAX_SAFE_INTEGER).finish())[0].value).toBe(BigInt(Number.MAX_SAFE_INTEGER));
    });

    it('splits int64 into two uint32 halves', () => {
        expect(splitInt64(0)).toEqual([0, 0]);
        expect(splitInt64(2 ** 32 + 5)).toEqual([5, 1]);
        expect(splitInt64(-1)).toEqual([0xffffffff, 0xffffffff]);
        expect(splitInt64(-(2 ** 32))).toEqual([0, 0xffffffff]);
    });

    it('writes fixed32, fixed64 and double little-endian', () => {
        expect(bytes(new ProtobufWriter().fixed32(0x01020304))).toEqual([4, 3, 2, 1]);
        expect(bytes(new ProtobufWriter().fixed64(0x01020304, 0x05060708))).toEqual([4, 3, 2, 1, 8, 7, 6, 5]);
        const fields = decode(new ProtobufWriter().doubleField(4, 3.25).fixed32Field(16, 7).fixed64Field(7, 1, 2).finish());
        expect(fields.map(f => [f.field, f.wire])).toEqual([[4, 1], [16, 5], [7, 1]]);
        expect(f64(fields[0].value)).toBe(3.25);
        expect(u32(fields[1].value)).toBe(7);
        expect(u64(fields[2].value)).toBe(2n ** 33n + 1n);
    });

    it('writes length-delimited strings, bytes and nested messages', () => {
        const w = new ProtobufWriter();
        w.stringField(1, 'héllo ✓').bytesField(2, new Uint8Array([9, 8])).messageField(3, m => m.uint32Field(1, 150));
        const fields = decode(w.finish());
        expect(str(one(fields, 1).value)).toBe('héllo ✓');
        expect(Array.from(one(fields, 2).value)).toEqual([9, 8]);
        expect(decode(one(fields, 3).value)[0]).toMatchObject({ field: 1, wire: 0, value: 150n });
    });

    it('writes packed repeated fields', () => {
        const w = new ProtobufWriter().packedVarint(4, [1, 300, 2]).packedDouble(5, [1.5, -2]).packedFixed64(6, [[1, 0], [2, 0]]);
        const fields = decode(w.finish());
        expect(Array.from(one(fields, 4).value)).toEqual([1, 0xac, 0x02, 2]);
        expect(one(fields, 5).value.length).toBe(16);
        expect(f64(one(fields, 5).value.slice(8))).toBe(-2);
        expect(u64(one(fields, 6).value.slice(8))).toBe(2n);
    });

    it('grows its buffer for large payloads', () => {
        const big = new Uint8Array(100000).fill(7);
        const fields = decode(new ProtobufWriter().bytesField(1, big).uint32Field(2, 1).finish());
        expect(one(fields, 1).value.length).toBe(100000);
        expect(one(fields, 2).value).toBe(1n);
    });

    it('converts epoch milliseconds to exact unix nanos', () => {
        const [lo, hi] = msToNanos(1700000000123.456);
        expect(BigInt(hi) * 2n ** 32n + BigInt(lo)).toBe(1700000000123456000n);
        expect(msToNanosString(1700000000123.456)).toBe('1700000000123456000');
        expect(msToNanosString(0)).toBe('0');
        const [lo2, hi2] = msToNanos(4102444800000); // 2100-01-01
        expect(BigInt(hi2) * 2n ** 32n + BigInt(lo2)).toBe(4102444800000000000n);
    });

    it('decodes hex ids', () => {
        expect(Array.from(hexToBytes('00ff10'))).toEqual([0, 255, 16]);
    });
});
