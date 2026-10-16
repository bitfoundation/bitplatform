namespace Bit.Belemetry;

/// <summary>How browser telemetry leaves the page.</summary>
public enum BelemetryBrowserTransport
{
    /// <summary><see cref="Relay"/> in Blazor Hybrid, <see cref="Direct"/> everywhere else.</summary>
    Auto,

    /// <summary>The script exports straight to the configured endpoints with <c>fetch</c>. The endpoints must allow the page origin (CORS).</summary>
    Direct,

    /// <summary>
    /// The script hands its records to .NET over JS interop and the .NET pipeline exports them. Needs an
    /// interactive component (<c>&lt;BelemetryInitializer /&gt;</c>); avoids CORS and keeps exporter credentials
    /// out of the page. On Blazor Server the records travel over the circuit.
    /// </summary>
    Relay,
}
