namespace Bit.Belemetry;

/// <summary>What creates a capture automatically (<see cref="BelemetryDiagnosticsOptions.AutoCaptureTriggers"/>).</summary>
[Flags]
public enum BelemetryDiagnosticTriggers
{
    /// <summary>Nothing: captures are only made on request.</summary>
    None = 0,

    /// <summary>An exception no one handled: logged by the renderer (WebAssembly, Hybrid) or the circuit (Server).</summary>
    UnhandledExceptions = 1,

    /// <summary>An exception an <c>ErrorBoundary</c> caught.</summary>
    ErrorBoundary = 2,

    /// <summary>A JavaScript error or unhandled promise rejection in the page.</summary>
    BrowserErrors = 4,

    /// <summary>A circuit failure: an unhandled circuit error, or a reconnection that failed or was rejected.</summary>
    CircuitFailures = 8,

    /// <summary>Any exception event (logged at Error or above, tracked, or from the browser).</summary>
    AllExceptions = 16,

    /// <summary>Exceptions an <see cref="IBelemetryExceptionClassifier"/> marks critical.</summary>
    Critical = 32,

    /// <summary>The usual production choice: unhandled exceptions, circuit failures and critical exceptions.</summary>
    Default = UnhandledExceptions | CircuitFailures | Critical,
}
