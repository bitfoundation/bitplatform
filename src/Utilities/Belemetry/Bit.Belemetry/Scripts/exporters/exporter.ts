import type { RecordBatch } from '../model';

/** The signal bits of an exporter's `signals` (C# BelemetrySignals). Metrics never come from the browser. */
export const Signals = {
    Traces: 1,
    Logs: 2,
    Metrics: 4,
    All: 7,
} as const;

/** The signals an exporter config asks for: absent or null means all of them. */
export function signalsOf(config: { signals?: number | null }): number {
    return config.signals == null ? Signals.All : config.signals;
}

export interface Exporter {
    /** The URLs this exporter posts to (prefixes): the fetch/XHR instrumentation never traces them. */
    urls: string[];
    /** Headers to send when a persisted request to one of `urls` is resent (stored requests carry none). */
    headers?: Record<string, string> | null;
    /** Serializes the batch and hands it to the transport. Resolves once handed over, not when delivered. */
    export(batch: RecordBatch, unload: boolean): Promise<void>;
}
