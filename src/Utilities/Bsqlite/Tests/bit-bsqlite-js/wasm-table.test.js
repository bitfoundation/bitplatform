import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { test, expect } from 'vitest';
import { fileURLToPath } from 'node:url';
import { raiseFunctionTableLimit } from '../../Bit.Bsqlite/Scripts/bit-bsqlite-wasm-table.ts';

const engineDir = join(
    dirname(fileURLToPath(import.meta.url)),
    '../../Bit.Bsqlite/wwwroot/engine');

const builds = ['wa-sqlite.wasm', 'wa-sqlite-jspi.wasm', 'wa-sqlite-async.wasm'];

function readTableLimits(bytes) {
    let offset = 8;
    while (offset < bytes.length) {
        const id = bytes[offset++];
        let size = 0;
        let shift = 0;
        while (true) {
            const byte = bytes[offset++];
            size |= (byte & 0x7f) << shift;
            if ((byte & 0x80) === 0) {
                break;
            }

            shift += 7;
        }

        if (id === 4) {
            const payload = bytes.subarray(offset, offset + size);
            // count=1, funcref, limits=1, min, max - all LEB after the two tag bytes.
            let i = 0;
            const skipLeb = () => {
                let n = 0;
                let s = 0;
                while (true) {
                    const b = payload[i++];
                    n |= (b & 0x7f) << s;
                    if ((b & 0x80) === 0) {
                        return n >>> 0;
                    }

                    s += 7;
                }
            };

            skipLeb();
            i++; // funcref
            i++; // limits flag
            return { min: skipLeb(), max: skipLeb() };
        }

        offset += size;
    }

    throw new Error('No table section.');
}

for (const name of builds) {
    test(`${name} is valid after the table maximum is raised`, () => {
        const original = new Uint8Array(readFileSync(join(engineDir, name)));
        const before = readTableLimits(original);
        expect(before.min, 'the vendored pin is a fixed-size table').toBe(before.max);

        const patched = raiseFunctionTableLimit(original, 16);
        expect(patched).not.toBe(original);
        expect(WebAssembly.validate(patched)).toBeTruthy();

        const after = readTableLimits(patched);
        expect(after.min).toBe(before.min);
        expect(after.max).toBe(before.min + 16);
        expect(readTableLimits(original)).toEqual(before);
    });
}
