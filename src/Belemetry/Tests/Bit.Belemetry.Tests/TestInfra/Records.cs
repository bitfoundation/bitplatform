namespace Bit.Belemetry.Tests;

/// <summary>Factories for model records with well-known values.</summary>
internal static class Records
{
    public const string TraceId = "0af7651916cd43dd8448eb211c80319c";
    public const string SpanId = "b7ad6b7169203331";
    public const string ParentSpanId = "00f067aa0ba902b7";
    public const string LinkTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
    public const string LinkSpanId = "53995c3f42cd8ad8";

    public const long Start = 1_700_000_000_123_456_789;
    public const long End = Start + 1_500_000_000; // 1.5 s

    public static BelemetryResource Resource(string? ns = "shop")
    {
        var attributes = new BelemetryAttributes()
            .Set(BelemetrySemanticConventions.ServiceName, "checkout")
            .Set(BelemetrySemanticConventions.ServiceVersion, "2.0.0")
            .Set(BelemetrySemanticConventions.ServiceInstanceId, "instance-42")
            .Set(BelemetrySemanticConventions.TelemetrySdkLanguage, "dotnet")
            .Set(BelemetrySemanticConventions.TelemetrySdkVersion, "10.6.2")
            .Set(BelemetrySemanticConventions.OsType, "windows");
        if (ns is not null) attributes.Set(BelemetrySemanticConventions.ServiceNamespace, ns);
        return new BelemetryResource(attributes);
    }

    public static BelemetrySpan Span(BelemetrySpanKind kind = BelemetrySpanKind.Client, string? name = null)
    {
        return new BelemetrySpan
        {
            TraceId = TraceId,
            SpanId = SpanId,
            ParentSpanId = ParentSpanId,
            Name = name ?? "GET /api/orders",
            Kind = kind,
            StartTimeUnixNano = Start,
            EndTimeUnixNano = End,
            Scope = new BelemetryScope("System.Net.Http", "10.0.0"),
        };
    }

    public static BelemetryLogRecord Log(string? body = "hello", string? eventName = null, BelemetrySeverity severity = BelemetrySeverity.Info)
    {
        return new BelemetryLogRecord
        {
            TimeUnixNano = Start,
            ObservedTimeUnixNano = Start + 5,
            Severity = severity,
            SeverityText = severity.ToString().ToUpperInvariant(),
            Body = body,
            EventName = eventName,
            Scope = new BelemetryScope("MyApp.Orders"),
        };
    }

    public static byte[] Hex(string hex) => Convert.FromHexString(hex);
}
