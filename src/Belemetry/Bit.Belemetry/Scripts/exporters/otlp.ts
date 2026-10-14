import type { OtlpExporterConfig } from '../config';
import type { Transport } from '../transport';
import { gzip } from '../transport';
import type { Exporter } from './exporter';
import { logsToJson, tracesToJson } from './otlp-json';
import { encodeLogs, encodeTraces } from './otlp-proto';
import { encodeUtf8 } from './protobuf';

export const DEFAULT_OTLP_ENDPOINT = 'http://localhost:4318';

/** Resolves the signal URLs: `tracesEndpoint`/`logsEndpoint` verbatim, otherwise `endpoint` + `/v1/<signal>`. */
export function otlpUrls(config: OtlpExporterConfig): { base: string; traces: string; logs: string } {
    const base = (config.endpoint || DEFAULT_OTLP_ENDPOINT).replace(/\/+$/, '');
    return { base, traces: config.tracesEndpoint || base + '/v1/traces', logs: config.logsEndpoint || base + '/v1/logs' };
}

export function createOtlpExporter(config: OtlpExporterConfig, transport: Transport, sdkVersion: string): Exporter {
    const urls = otlpUrls(config);
    const json = config.protocol === 'http/json';
    const contentType = json ? 'application/json' : 'application/x-protobuf';
    const compress = config.compression === 'gzip';

    const send = async (url: string, body: Uint8Array, unload: boolean): Promise<void> => {
        let payload = body;
        let encoding: string | null = null;
        // Compression is async, and nothing async survives unload, so unload batches go uncompressed.
        if (compress && !unload) {
            const zipped = await gzip(body);
            if (zipped) {
                payload = zipped;
                encoding = 'gzip';
            }
        }
        transport.send({ exporter: 'otlp', url, contentType, contentEncoding: encoding, headers: config.headers, body: payload, raw: encoding ? body : null }, unload);
    };

    return {
        urls: [urls.base, urls.traces, urls.logs],
        headers: config.headers,
        export(batch, unload) {
            // Both sends start synchronously (no await in between), which matters during unload.
            const sends: Promise<void>[] = [];
            if (batch.spans.length) {
                const body = json ? encodeUtf8(JSON.stringify(tracesToJson(batch.resource, batch.spans, sdkVersion))) : encodeTraces(batch.resource, batch.spans, sdkVersion);
                sends.push(send(urls.traces, body, unload));
            }
            if (batch.logs.length) {
                const body = json ? encodeUtf8(JSON.stringify(logsToJson(batch.resource, batch.logs, sdkVersion))) : encodeLogs(batch.resource, batch.logs, sdkVersion);
                sends.push(send(urls.logs, body, unload));
            }
            return Promise.all(sends).then(() => undefined);
        },
    };
}
