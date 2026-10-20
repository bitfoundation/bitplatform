import type { TelemetryRecord } from '../model';

/** Groups records by instrumentation scope, keeping the first-seen order. */
export function groupByScope<T extends TelemetryRecord>(records: T[]): [string, T[]][] {
    const groups: Record<string, T[]> = {};
    const order: string[] = [];
    for (const record of records) {
        const scope = record.scope || 'bit.belemetry';
        if (!groups[scope]) {
            groups[scope] = [];
            order.push(scope);
        }
        groups[scope].push(record);
    }
    return order.map(scope => [scope, groups[scope]]);
}

/** Whether a number is sent as int_value (a safe integer) rather than double_value. */
export function isInt(value: number): boolean {
    return Number.isSafeInteger(value) && !Object.is(value, -0);
}

/** Epoch milliseconds to unix nanoseconds as a decimal string (microsecond precision, exact). */
export function msToNanosString(ms: number): string {
    const micros = Math.max(0, Math.round(ms * 1000));
    return micros === 0 ? '0' : micros + '000';
}
