import type { ResolvedConfig } from '../config';
import type { TelemetryRecord } from '../model';

export interface InstrumentationContext {
    config: ResolvedConfig;
    /** Hands a record to the pipeline (consent, privacy, enrichment, queue). Never throws. */
    emit(record: TelemetryRecord): void;
    /** Whether a URL is an export target, which must never be traced. */
    isExporterUrl(url: string): boolean;
}

export type Uninstall = () => void;

export const noop: Uninstall = () => { };

/** Runs an instrumentation callback without ever letting an exception reach the host page. */
export function guard<A extends unknown[]>(fn: (...args: A) => void): (...args: A) => void {
    return (...args: A) => {
        try {
            fn(...args);
        } catch {
            // Never throw into the host page.
        }
    };
}

/** Compiles regex sources, skipping invalid ones. */
export function compilePatterns(sources: string[] | null | undefined): RegExp[] {
    const list: RegExp[] = [];
    for (const source of sources || []) {
        try {
            list.push(new RegExp(source));
        } catch {
            // An invalid pattern from configuration is ignored.
        }
    }
    return list;
}

export function matchesAny(patterns: RegExp[], text: string): boolean {
    for (const pattern of patterns) {
        if (pattern.test(text)) return true;
    }
    return false;
}

export function absoluteUrl(url: string): string {
    try {
        return new URL(url, location.href).href;
    } catch {
        return url;
    }
}
