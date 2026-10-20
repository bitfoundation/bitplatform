function randomHex(byteCount: number): string {
    const bytes = new Uint8Array(byteCount);
    try {
        crypto.getRandomValues(bytes);
    } catch {
        // Very old or locked-down environments: ids only need to be unique, not unguessable.
        for (let i = 0; i < byteCount; i++) bytes[i] = Math.floor(Math.random() * 256);
    }
    let hex = '';
    for (let i = 0; i < byteCount; i++) hex += (bytes[i] < 16 ? '0' : '') + bytes[i].toString(16);
    return hex;
}

function nonZero(byteCount: number): string {
    // W3C trace context treats an all-zero id as invalid.
    let id = randomHex(byteCount);
    while (/^0+$/.test(id)) id = randomHex(byteCount);
    return id;
}

/** A random 16-byte trace id as 32 lowercase hex chars. */
export function newTraceId(): string {
    return nonZero(16);
}

/** A random 8-byte span id as 16 lowercase hex chars. */
export function newSpanId(): string {
    return nonZero(8);
}

/** A W3C `traceparent` header value with the sampled flag. */
export function traceparent(traceId: string, spanId: string): string {
    return '00-' + traceId + '-' + spanId + '-01';
}
