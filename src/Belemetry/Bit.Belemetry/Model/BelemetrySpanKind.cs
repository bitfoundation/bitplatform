namespace Bit.Belemetry;

/// <summary>The OTLP span kind (numbered as in <c>opentelemetry/proto/trace/v1/trace.proto</c>).</summary>
public enum BelemetrySpanKind
{
    /// <summary>Unspecified.</summary>
    Unspecified = 0,
    /// <summary>An internal operation.</summary>
    Internal = 1,
    /// <summary>The server side of a synchronous request.</summary>
    Server = 2,
    /// <summary>The client side of a synchronous request.</summary>
    Client = 3,
    /// <summary>The initiator of an asynchronous request.</summary>
    Producer = 4,
    /// <summary>The receiver of an asynchronous request.</summary>
    Consumer = 5,
}
