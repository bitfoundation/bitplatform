namespace Bit.Belemetry;

/// <summary>The kinds of diagnostic events. An event has exactly one; the options take any combination.</summary>
[Flags]
public enum BelemetryDiagnosticCategories
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Route changes: the framework's <c>Navigate</c> activity (with the route template) and browser page views.</summary>
    Navigation = 1,

    /// <summary>Component activity: UI events handled by a component (<c>HandleEvent</c>), render statistics, error boundaries.</summary>
    Component = 2,

    /// <summary>Exceptions: .NET (logged, unhandled, caught by an error boundary, tracked) and browser (errors, unhandled rejections).</summary>
    Exception = 4,

    /// <summary>
    /// HTTP requests: browser fetch/XHR and .NET <c>HttpClient</c> client spans, server request spans. Belemetry collects
    /// the .NET spans only with an exporter configured: without one this category holds the browser's requests alone,
    /// even when another tracer (an OpenTelemetry SDK) records the app's.
    /// </summary>
    Http = 8,

    /// <summary>JS interop failures (<c>JSException</c>, <c>JSDisconnectedException</c>, interop timeouts).</summary>
    JsInterop = 16,

    /// <summary>Browser facts: interactions (event type and element only), page visibility, reloads.</summary>
    Browser = 32,

    /// <summary>Blazor Server circuit and reconnection transitions.</summary>
    Circuit = 64,

    /// <summary><c>ILogger</c> entries at or above <see cref="BelemetryDiagnosticsOptions.MinimumLogLevel"/>.</summary>
    Log = 128,

    /// <summary>
    /// Other spans (an app's own <c>ActivitySource</c>). Belemetry collects them only with an exporter configured: without
    /// one this category stays empty, even when another tracer (an OpenTelemetry SDK) records them.
    /// </summary>
    Trace = 256,

    /// <summary>Events the app records itself (<see cref="IBelemetryDiagnostics.Record"/>), and recorded snapshots.</summary>
    Custom = 512,

    /// <summary>Every category.</summary>
    All = Navigation | Component | Exception | Http | JsInterop | Browser | Circuit | Log | Trace | Custom,
}
