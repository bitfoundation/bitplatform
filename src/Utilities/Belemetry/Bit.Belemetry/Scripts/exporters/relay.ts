import { debug } from '../log';
import type { RecordBatch } from '../model';
import { countDropped } from '../stats';

/** The .NET object reference (`DotNetObjectReference<BelemetryJsCallbacks>`). */
export interface DotNetRef {
    invokeMethodAsync<T = unknown>(identifier: string, ...args: unknown[]): Promise<T>;
    invokeMethod<T = unknown>(identifier: string, ...args: unknown[]): T;
}

export const DotNetCallbacks = {
    relay: 'Belemetry.Relay',
    contextChanged: 'Belemetry.ContextChanged',
    drainForUnload: 'Belemetry.DrainForUnload',
    diagnosticTrigger: 'Belemetry.DiagnosticTrigger',
} as const;

/**
 * Hands a batch to .NET (transport "relay"). The batch is the browser record model as-is (C# BelemetryRelayBatch), with
 * the ratio the page sampled its sessions at: a page another side of an Auto app started first keeps that side's
 * configuration, which the pipeline it relays to may not share. In WebAssembly the call reaches .NET synchronously,
 * which is what lets the unload drain pick the records up.
 *
 * On a Blazor Server circuit the call is one message of the circuit's connection, which closes on a message over
 * HubOptions.MaximumReceiveMessageSize (32 KB by default) - taking the circuit, and the records, with it. There .NET
 * passes `maxBytes` (C# `Browser.MaxRelayMessageBytes`) and the batch goes in parts of at most that many bytes of JSON;
 * a record that alone is larger is dropped (`size_limit`), and so is every record when the resource alone fills the limit.
 */
export function relayBatch(ref: DotNetRef, batch: RecordBatch, sessionRatio: number, maxBytes = 0): Promise<void> {
    const parts = maxBytes > 0 ? splitRelayBatch(batch, sessionRatio, maxBytes) : [{ ...batch, sessionRatio }];
    return Promise.all(parts.map(part => relayPart(ref, part))).then(() => undefined);
}

function relayPart(ref: DotNetRef, part: RecordBatch & { sessionRatio: number }): Promise<void> {
    try {
        return ref.invokeMethodAsync(DotNetCallbacks.relay, part).then(() => undefined, e => debug('relay failed', e));
    } catch (e) {
        debug('relay failed', e);
        return Promise.resolve();
    }
}

/**
 * The UTF-8 length of a string, without encoding it. A surrogate pair is 4 bytes; a lone surrogate is counted as the 3
 * bytes of its replacement character, though JSON.stringify never leaves one (it writes it as an escape).
 */
export function utf8Length(text: string): number {
    let bytes = text.length;
    for (let i = 0; i < text.length; i++) {
        const c = text.charCodeAt(i);
        if (c < 0x80) continue;
        if (c < 0x800) bytes += 1;
        else if (c >= 0xd800 && c <= 0xdbff && i + 1 < text.length && (text.charCodeAt(i + 1) & 0xfc00) === 0xdc00) {
            bytes += 2; // The pair's two code units are 4 bytes.
            i++;
        } else bytes += 2;
    }
    return bytes;
}

const jsonBytes = (value: unknown) => utf8Length(JSON.stringify(value));

/**
 * The batch as parts of at most maxBytes of JSON each, every part with the batch's resource; an empty batch stays one
 * part. A batch that fits is measured once and sent whole: most do, and only a larger one is measured record by record.
 * When the resource alone leaves no room for a record, nothing is sent - every record is dropped (`size_limit`), as a
 * part over the limit would close the circuit.
 */
export function splitRelayBatch(batch: RecordBatch, sessionRatio: number, maxBytes: number): (RecordBatch & { sessionRatio: number })[] {
    const whole = { ...batch, sessionRatio };
    if (jsonBytes(whole) <= maxBytes) return [whole];

    const envelope = jsonBytes({ ...batch, spans: [], logs: [], sessionRatio });
    if (envelope >= maxBytes) {
        countDropped('size_limit', batch.spans.length + batch.logs.length);
        debug(`relay: the resource alone is ${envelope} bytes of JSON, not under the limit of ${maxBytes}; the batch was dropped`);
        return [];
    }

    const parts: (RecordBatch & { sessionRatio: number })[] = [];
    let part = { resource: batch.resource, spans: [] as RecordBatch['spans'], logs: [] as RecordBatch['logs'], sessionRatio };
    let size = envelope;
    const add = (record: object, list: 'spans' | 'logs') => {
        // A record alone in its array needs no comma; one after another needs one.
        const bytes = jsonBytes(record);
        if (envelope + bytes > maxBytes) {
            countDropped('size_limit');
            return;
        }
        let comma = part[list].length > 0 ? 1 : 0;
        if (size + comma + bytes > maxBytes) {
            parts.push(part);
            part = { resource: batch.resource, spans: [], logs: [], sessionRatio };
            size = envelope;
            comma = 0;
        }
        (part[list] as object[]).push(record);
        size += comma + bytes;
    };
    for (const span of batch.spans) add(span, 'spans');
    for (const log of batch.logs) add(log, 'logs');
    // Not an empty part when every record was dropped: a batch that did not fit had records.
    if (part.spans.length > 0 || part.logs.length > 0) parts.push(part);
    return parts;
}
