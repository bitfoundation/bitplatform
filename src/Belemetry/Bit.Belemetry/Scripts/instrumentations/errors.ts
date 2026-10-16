import { createException } from '../records';
import { compilePatterns, guard, matchesAny, type InstrumentationContext, type Uninstall } from './context';

export const ERRORS_SCOPE = 'bit.belemetry.errors';

interface ErrorLike {
    name?: string;
    message?: string;
    stack?: string;
}

function describeReason(reason: unknown): { type: string; message: string; stack?: string } {
    if (reason && typeof reason === 'object' && ('message' in reason || 'stack' in reason)) {
        const e = reason as ErrorLike;
        return { type: e.name || 'Error', message: String(e.message || ''), stack: e.stack };
    }
    let message: string;
    try {
        message = typeof reason === 'string' ? reason : JSON.stringify(reason);
    } catch {
        message = String(reason);
    }
    return { type: 'UnhandledRejection', message: message || String(reason) };
}

/** `error` (script errors only: resource errors do not bubble to window) and `unhandledrejection`. */
export function installErrors(ctx: InstrumentationContext): Uninstall {
    const ignore = compilePatterns(ctx.config.errors.ignoreMessages);
    const report = (type: string, message: string, stack?: string) => {
        if (matchesAny(ignore, message)) return;
        ctx.emit(createException(ERRORS_SCOPE, type, message, stack));
    };
    const onError = guard((event: ErrorEvent) => {
        const error = event.error as ErrorLike | null | undefined;
        const message = event.message || (error && error.message) || '';
        if (!message && !error) return;
        const stack = (error && error.stack) || (event.filename ? 'at ' + event.filename + ':' + event.lineno + ':' + event.colno : undefined);
        report((error && error.name) || 'Error', String(message), stack);
    });
    const onRejection = guard((event: PromiseRejectionEvent) => {
        const reason = describeReason(event.reason);
        report(reason.type, reason.message, reason.stack);
    });
    addEventListener('error', onError);
    addEventListener('unhandledrejection', onRejection);
    return () => {
        removeEventListener('error', onError);
        removeEventListener('unhandledrejection', onRejection);
    };
}
