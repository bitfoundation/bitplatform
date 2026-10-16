namespace Bit.Belemetry;

/// <summary>Settings of the console exporter, which prints every exported record - for development only.</summary>
public sealed class BelemetryConsoleExporterOptions
{
    /// <summary>Whether the browser script prints its records to the browser console too. Defaults to <see langword="true"/>.</summary>
    public bool IncludeInBrowser { get; set; } = true;

    /// <summary>The signals printed. Defaults to all three.</summary>
    public BelemetrySignals Signals { get; set; } = BelemetrySignals.All;
}
