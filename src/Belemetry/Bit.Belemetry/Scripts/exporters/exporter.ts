import type { RecordBatch } from '../model';

export interface Exporter {
    /** The URLs this exporter posts to (prefixes): the fetch/XHR instrumentation never traces them. */
    urls: string[];
    /** Headers to send when a persisted request to one of `urls` is resent (stored requests carry none). */
    headers?: Record<string, string> | null;
    /** Serializes the batch and hands it to the transport. Resolves once handed over, not when delivered. */
    export(batch: RecordBatch, unload: boolean): Promise<void>;
}
