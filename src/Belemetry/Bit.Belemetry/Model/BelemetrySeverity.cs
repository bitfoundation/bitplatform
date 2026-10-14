namespace Bit.Belemetry;

/// <summary>The OTLP log severity number (numbered as in <c>opentelemetry/proto/logs/v1/logs.proto</c>).</summary>
public enum BelemetrySeverity
{
    /// <summary>Unspecified.</summary>
    Unspecified = 0,
    /// <summary>TRACE.</summary>
    Trace = 1,
    /// <summary>DEBUG.</summary>
    Debug = 5,
    /// <summary>INFO.</summary>
    Info = 9,
    /// <summary>WARN.</summary>
    Warn = 13,
    /// <summary>ERROR.</summary>
    Error = 17,
    /// <summary>FATAL.</summary>
    Fatal = 21,
}
