namespace Bit.Belemetry;

/// <summary>The OTLP span status code (numbered as in <c>opentelemetry/proto/trace/v1/trace.proto</c>).</summary>
public enum BelemetryStatusCode
{
    /// <summary>The default status.</summary>
    Unset = 0,
    /// <summary>The operation completed successfully.</summary>
    Ok = 1,
    /// <summary>The operation failed.</summary>
    Error = 2,
}
