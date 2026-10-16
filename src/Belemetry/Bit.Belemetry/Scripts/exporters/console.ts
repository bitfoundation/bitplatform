import { print } from '../log';
import { isSpan, type TelemetryRecord } from '../model';
import type { Exporter } from './exporter';

function describe(record: TelemetryRecord): string {
    if (isSpan(record)) return 'span ' + record.name + ' (' + Math.round(record.endTime - record.startTime) + ' ms)';
    return record.severityText + ' ' + (record.eventName ? record.eventName + ' ' : '') + record.body;
}

/** Development output: one console line per record, through the original console (never captured). */
export function createConsoleExporter(): Exporter {
    return {
        urls: [],
        export(batch) {
            for (const record of [...batch.spans, ...batch.logs]) print('[bit-belemetry] ' + describe(record), record.attributes);
            return Promise.resolve();
        },
    };
}
