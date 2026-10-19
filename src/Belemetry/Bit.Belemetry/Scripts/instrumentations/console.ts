import { Features } from '../features';
import { Severity, type Attributes } from '../model';
import { createLog } from '../records';
import type { InstrumentationContext, Uninstall } from './context';

export const CONSOLE_SCOPE = 'bit.belemetry.console';

const SEVERITY: Record<string, number> = { error: Severity.Error, warn: Severity.Warn, info: Severity.Info, log: Severity.Info, debug: Severity.Debug, trace: Severity.Trace };

// Guards against recursion when something downstream logs to the console.
let busy = false;

function format(value: unknown): string {
    if (typeof value === 'string') return value;
    if (value instanceof Error) return value.name + ': ' + value.message;
    try {
        const json = JSON.stringify(value);
        return json === undefined ? String(value) : json.slice(0, 1024);
    } catch {
        return String(value);
    }
}

/** Console capture: `error` is feature Errors, the other levels are feature Logs. */
export function installConsole(ctx: InstrumentationContext): Uninstall {
    const target = console as unknown as Record<string, unknown>;
    const restore: (() => void)[] = [];
    for (const level of ctx.config.logs.consoleLevels || []) {
        const original = target[level];
        if (typeof original !== 'function' || !(level in SEVERITY)) continue;
        const wrapper = function (this: unknown, ...args: unknown[]) {
            const result = original.apply(this, args);
            if (!busy) {
                busy = true;
                try {
                    const attributes: Attributes = {};
                    const error = args.find(a => a instanceof Error) as Error | undefined;
                    if (error) {
                        attributes['exception.type'] = error.name;
                        attributes['exception.message'] = error.message;
                        if (error.stack) attributes['exception.stacktrace'] = error.stack;
                    }
                    const feature = level === 'error' ? Features.Errors : Features.Logs;
                    ctx.emit(createLog(CONSOLE_SCOPE, feature, SEVERITY[level], args.map(format).join(' ').slice(0, 8192), null, attributes));
                } catch {
                    // Never throw into the host page.
                } finally {
                    busy = false;
                }
            }
            return result;
        };
        target[level] = wrapper;
        restore.push(() => {
            if (target[level] === wrapper) target[level] = original;
        });
    }
    return () => {
        for (const undo of restore) undo();
    };
}
