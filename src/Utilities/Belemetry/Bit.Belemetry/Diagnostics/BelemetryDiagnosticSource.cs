namespace Bit.Belemetry;

/// <summary>Where a diagnostic event was observed.</summary>
public enum BelemetryDiagnosticSource
{
    /// <summary>The .NET side: <c>ILogger</c>, <c>Activity</c>, <see cref="IBelemetry"/>.</summary>
    Dotnet,

    /// <summary>The framework's own diagnostics (<c>Microsoft.AspNetCore.Components*</c> activities and meters).</summary>
    Framework,

    /// <summary>The browser script.</summary>
    Browser,

    /// <summary>The application itself (<see cref="IBelemetryDiagnostics.Record"/>, snapshots).</summary>
    Application,

    /// <summary>Belemetry's own diagnostics (dropped events, unavailable sources).</summary>
    Belemetry,
}
