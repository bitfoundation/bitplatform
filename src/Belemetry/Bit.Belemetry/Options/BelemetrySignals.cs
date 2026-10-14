namespace Bit.Belemetry;

/// <summary>The OpenTelemetry signals.</summary>
[Flags]
public enum BelemetrySignals
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Spans.</summary>
    Traces = 1,
    /// <summary>Log records and events.</summary>
    Logs = 2,
    /// <summary>Metrics.</summary>
    Metrics = 4,
    /// <summary>Every signal.</summary>
    All = Traces | Logs | Metrics,
}
