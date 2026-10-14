namespace Bit.Belemetry;

/// <summary>The OTLP transport encoding.</summary>
public enum BelemetryOtlpProtocol
{
    /// <summary>OTLP/HTTP with binary protobuf payloads (<c>application/x-protobuf</c>) - the most widely supported encoding.</summary>
    HttpProtobuf,

    /// <summary>OTLP/HTTP with JSON payloads (<c>application/json</c>).</summary>
    HttpJson,
}
