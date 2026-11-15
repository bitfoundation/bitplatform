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
 * a record that alone is larger is dropped (`size_limit`).
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

const encoder = new TextEncoder();
const jsonBytes = (value: unknown) => encoder.encode(JSON.stringify(value)).length;

/** The batch as parts of at most maxBytes of JSON each, every part with the batch's resource; an empty batch stays one part. */
export function splitRelayBatch(batch: RecordBatch, sessionRatio: number, maxBytes: number): (RecordBatch & { sessionRatio: number })[] {
    const envelope = jsonBytes({ ...batch, spans: [], logs: [], sessionRatio });
    const parts: (RecordBatch & { sessionRatio: number })[] = [];
    let part = { resource: batch.resource, spans: [] as RecordBatch['spans'], logs: [] as RecordBatch['logs'], sessionRatio };
    let size = envelope;
    const add = (record: object, list: 'spans' | 'logs') => {
        const bytes = jsonBytes(record) + 1;
        if (envelope + bytes > maxBytes) {
            countDropped('size_limit');
            return;
        }
        if (size + bytes > maxBytes) {
            parts.push(part);
            part = { resource: batch.resource, spans: [], logs: [], sessionRatio };
            size = envelope;
        }
        (part[list] as object[]).push(record);
        size += bytes;
    };
    for (const span of batch.spans) add(span, 'spans');
    for (const log of batch.logs) add(log, 'logs');
    if (part.spans.length > 0 || part.logs.length > 0 || parts.length === 0) parts.push(part);
    return parts;
}
