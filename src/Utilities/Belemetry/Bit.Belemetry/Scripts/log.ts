// The console methods are captured when the module is evaluated, before the optional console instrumentation
// patches them, so the SDK's own output never feeds back into console capture.
const originalDebug = console.debug;
const originalLog = console.log;

let enabled = false;

export function setDebug(on: boolean): void {
    enabled = on;
}

export function debug(...args: unknown[]): void {
    if (!enabled) return;
    try {
        originalDebug.apply(console, ['[bit-belemetry]', ...args]);
    } catch {
        // Never throw into the host page.
    }
}

export function print(...args: unknown[]): void {
    try {
        originalLog.apply(console, args);
    } catch {
        // Never throw into the host page.
    }
}
