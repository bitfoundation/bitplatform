import { Features } from './features';
import type { Attributes } from './model';

// Mirrors C# BelemetryBrowserConfig (serialized camelCase). See "Browser config" in CLAUDE.md.

export interface ServiceConfig {
    name: string;
    version: string | null;
    namespace: string | null;
    instanceId: string | null;
    environment: string | null;
}

export interface OtlpExporterConfig {
    type: 'otlp';
    endpoint?: string | null;
    tracesEndpoint?: string | null;
    logsEndpoint?: string | null;
    protocol?: 'http/protobuf' | 'http/json' | null;
    headers?: Record<string, string> | null;
    compression?: 'none' | 'gzip' | null;
}

export interface AzureMonitorExporterConfig {
    type: 'azureMonitor';
    connectionString: string;
    maskIp?: boolean | null;
}

export interface ConsoleExporterConfig {
    type: 'console';
}

export type ExporterConfig = OtlpExporterConfig | AzureMonitorExporterConfig | ConsoleExporterConfig;

export interface ConsentConfig {
    default: number;
    persist: boolean;
    honorGlobalPrivacyControl: boolean;
    pendingLimit: number;
}

export interface PageViewsConfig {
    trackHashChanges: boolean;
}

export interface InteractionsConfig {
    events: string[];
    captureText: boolean;
    maxTextLength: number;
}

export interface PerformanceConfig {
    webVitals: boolean;
    documentLoad: boolean;
    resources: boolean;
    longTasks: boolean;
}

export interface DependenciesConfig {
    fetch: boolean;
    xhr: boolean;
    propagateTraceContextTo: string[];
    ignoreUrls: string[];
}

export interface ErrorsConfig {
    ignoreMessages: string[];
}

export interface LogsConfig {
    captureConsole: boolean;
    consoleLevels: string[];
}

export interface PrivacyConfig {
    redactQueryParameters: string[];
    stripQueryString: boolean;
    stripFragment: boolean;
    scrubPatterns: boolean;
    collectUserAgent: boolean;
}

export interface SessionConfig {
    inactivityTimeoutMinutes: number;
    maxDurationMinutes: number;
}

export interface SamplingConfig {
    sessionRatio: number;
}

export interface BatchConfig {
    maxQueueSize: number;
    maxBatchSize: number;
    flushIntervalMs: number;
}

export interface PersistenceConfig {
    enabled: boolean;
    maxBytes: number;
    maxAgeHours: number;
}

export interface ResolvedConfig {
    service: ServiceConfig;
    resource: Attributes;
    sdkVersion: string;
    transport: 'direct' | 'relay';
    exporters: ExporterConfig[];
    features: number;
    consent: ConsentConfig;
    pageViews: PageViewsConfig;
    interactions: InteractionsConfig;
    performance: PerformanceConfig;
    dependencies: DependenciesConfig;
    errors: ErrorsConfig;
    logs: LogsConfig;
    privacy: PrivacyConfig;
    session: SessionConfig;
    sampling: SamplingConfig;
    batch: BatchConfig;
    persistence: PersistenceConfig;
    debug: boolean;
}

type Section<T> = T extends unknown[] ? T : T extends object ? { [P in keyof T]?: T[P] | null } : T;

/** The config as it arrives from .NET or the page: every field optional, nulls mean "use the default". */
export type BelemetryConfig = { [K in keyof ResolvedConfig]?: Section<ResolvedConfig[K]> | null };

export function createDefaults(): ResolvedConfig {
    return {
        service: { name: 'unknown_service:webjs', version: null, namespace: null, instanceId: null, environment: null },
        resource: {},
        sdkVersion: '0.0.0',
        transport: 'direct',
        exporters: [],
        features: Features.All,
        consent: { default: Features.None, persist: true, honorGlobalPrivacyControl: true, pendingLimit: 500 },
        pageViews: { trackHashChanges: false },
        interactions: { events: ['click'], captureText: false, maxTextLength: 64 },
        performance: { webVitals: true, documentLoad: true, resources: false, longTasks: false },
        dependencies: { fetch: true, xhr: true, propagateTraceContextTo: ['same-origin'], ignoreUrls: ['/_framework/', '/_blazor', '/_content/'] },
        errors: { ignoreMessages: ['ResizeObserver loop'] },
        logs: { captureConsole: false, consoleLevels: ['error', 'warn'] },
        privacy: {
            redactQueryParameters: ['token', 'code', 'password', 'pwd', 'secret', 'key', 'apikey', 'api_key', 'auth', 'authorization', 'session', 'sid', 'sig',
                'signature', 'email', 'access_token', 'id_token', 'refresh_token'],
            stripQueryString: false,
            stripFragment: true,
            scrubPatterns: true,
            collectUserAgent: true,
        },
        session: { inactivityTimeoutMinutes: 30, maxDurationMinutes: 1440 },
        sampling: { sessionRatio: 1 },
        batch: { maxQueueSize: 1024, maxBatchSize: 100, flushIntervalMs: 5000 },
        persistence: { enabled: true, maxBytes: 1048576, maxAgeHours: 48 },
        debug: false,
    };
}

/** Merges a (partial) config over the defaults, one level deep. Null and undefined values keep the default. */
export function resolveConfig(input?: BelemetryConfig | null): ResolvedConfig {
    const target = createDefaults() as unknown as Record<string, unknown>;
    const source = (input || {}) as Record<string, unknown>;
    for (const key of Object.keys(source)) {
        const value = source[key];
        if (value == null) continue;
        const current = target[key];
        if (isPlainObject(current) && isPlainObject(value)) {
            for (const prop of Object.keys(value)) {
                if (value[prop] != null) current[prop] = value[prop];
            }
        } else {
            target[key] = value;
        }
    }
    return target as unknown as ResolvedConfig;
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}
