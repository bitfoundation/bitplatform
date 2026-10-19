// The feature bits are a wire contract with BelemetryFeatures in C#: they cross the interop boundary as plain
// integers and are persisted in localStorage, so they must never be renumbered.
export const Features = {
    None: 0,
    Errors: 1,
    PageViews: 2,
    Interactions: 4,
    Performance: 8,
    Dependencies: 16,
    Sessions: 32,
    UserTracking: 64,
    Logs: 128,
    Traces: 256,
    Metrics: 512,
    Persistence: 1024,
    Events: 2048,
    Diagnostics: 1 | 128 | 256 | 16,
    Analytics: 2 | 4 | 32 | 64 | 2048,
    Monitoring: 8 | 512,
    All: 4095,
} as const;
