namespace Bit.Belemetry;

/// <summary>The OTLP transport encoding.</summary>
public enum BelemetryOtlpProtocol
{
    /// <summary>OTLP/HTTP with binary protobuf payloads (<c>application/x-protobuf</c>) - the most widely supported encoding.</summary>
    HttpProtobuf,

    /// <summary>OTLP/HTTP with JSON payloads (<c>application/json</c>).</summary>
    HttpJson,

    /// <summary>
    /// OTLP/gRPC: protobuf payloads over HTTP/2 (<c>application/grpc</c>), the default of the OpenTelemetry SDKs and
    /// what the .NET Aspire AppHost hands its resources. Belemetry speaks it without a gRPC library: unary calls
    /// are plain HTTP/2 POSTs with a 5-byte message prefix, and the status comes back in the response trailers.
    /// .NET side only - a browser cannot send gRPC, so the browser script posts to <see cref="BelemetryOtlpExporterOptions.BrowserEndpoint"/>
    /// over OTLP/HTTP instead, and gets nothing when that is unset.
    /// </summary>
    Grpc,
}
