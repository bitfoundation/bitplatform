namespace Bit.Belemetry.Tests.Harness.Web.Collector;

// What the capturing receiver keeps, flattened for assertions: attribute values are strings (arrays as JSON), and
// every record carries its resource, so a test can tell the browser's records (telemetry.sdk.language = webjs) from
// .NET's. Settable classes rather than positional records: the suites deserialize /test-collector/received into them.

/// <summary>One request the receiver answered.</summary>
public sealed class CollectedRequest
{
    /// <summary><c>traces</c>, <c>logs</c>, <c>metrics</c> (OTLP) or <c>azure</c>.</summary>
    public string Signal { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string? ContentEncoding { get; set; }

    public int Length { get; set; }

    public DateTimeOffset Time { get; set; }

    /// <summary>Whether the body could be decoded (OTLP/JSON or Azure envelopes). Protobuf bodies are kept undecoded.</summary>
    public bool Decoded { get; set; }

    /// <summary>The <c>User-Agent</c> header: a browser's for the script's exports, empty for .NET's HttpClient.</summary>
    public string? UserAgent { get; set; }
}

public sealed class CollectedSpan
{
    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public string? ParentSpanId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Kind { get; set; }

    public int StatusCode { get; set; }

    public string? Scope { get; set; }

    public Dictionary<string, string?> Attributes { get; set; } = [];

    public Dictionary<string, string?> Resource { get; set; } = [];
}

public sealed class CollectedLog
{
    public int SeverityNumber { get; set; }

    public string? SeverityText { get; set; }

    public string? Body { get; set; }

    public string? EventName { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? Scope { get; set; }

    public Dictionary<string, string?> Attributes { get; set; } = [];

    public Dictionary<string, string?> Resource { get; set; } = [];
}

public sealed class CollectedMetric
{
    public string Name { get; set; } = string.Empty;

    /// <summary><c>sum</c>, <c>gauge</c> or <c>histogram</c>.</summary>
    public string Type { get; set; } = string.Empty;

    public string? Scope { get; set; }

    /// <summary>The attributes of each data point.</summary>
    public List<Dictionary<string, string?>> Points { get; set; } = [];

    public Dictionary<string, string?> Resource { get; set; } = [];
}

public sealed class CollectedEnvelope
{
    /// <summary>e.g. <c>Microsoft.ApplicationInsights.PageView</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>e.g. <c>PageViewData</c>.</summary>
    public string? BaseType { get; set; }

    public string? IKey { get; set; }

    public string? Time { get; set; }

    public Dictionary<string, string?> Tags { get; set; } = [];

    /// <summary><c>baseData.properties</c>.</summary>
    public Dictionary<string, string?> Properties { get; set; } = [];

    /// <summary><c>baseData.name</c> (PageView, Event, Request, RemoteDependency), or <c>message</c> (Message).</summary>
    public string? DataName { get; set; }

    /// <summary>The whole <c>baseData</c> object as JSON.</summary>
    public string? BaseData { get; set; }
}

/// <summary>A request to <c>/test-api/echo</c>, with its headers (names lower-cased).</summary>
public sealed class CollectedEcho
{
    public DateTimeOffset Time { get; set; }

    public string? Query { get; set; }

    public Dictionary<string, string> Headers { get; set; } = [];
}

/// <summary>The body of <c>GET /test-collector/received</c>.</summary>
public sealed class CollectedTelemetry
{
    public List<CollectedRequest> Requests { get; set; } = [];

    public List<CollectedSpan> Spans { get; set; } = [];

    public List<CollectedLog> Logs { get; set; } = [];

    public List<CollectedMetric> Metrics { get; set; } = [];

    public List<CollectedEnvelope> Envelopes { get; set; } = [];

    public List<CollectedEcho> Echoes { get; set; } = [];
}
