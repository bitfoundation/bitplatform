import { debug } from '../log';
import type { RecordBatch } from '../model';

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
 */
export function relayBatch(ref: DotNetRef, batch: RecordBatch, sessionRatio: number): Promise<void> {
    try {
        return ref.invokeMethodAsync(DotNetCallbacks.relay, { ...batch, sessionRatio }).then(() => undefined, e => debug('relay failed', e));
    } catch (e) {
        debug('relay failed', e);
        return Promise.resolve();
    }
}
