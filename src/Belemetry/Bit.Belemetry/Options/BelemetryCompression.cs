namespace Bit.Belemetry;

/// <summary>The request-body compression an exporter applies.</summary>
public enum BelemetryCompression
{
    /// <summary>No compression.</summary>
    None,

    /// <summary>gzip (<c>Content-Encoding: gzip</c>).</summary>
    Gzip,
}
