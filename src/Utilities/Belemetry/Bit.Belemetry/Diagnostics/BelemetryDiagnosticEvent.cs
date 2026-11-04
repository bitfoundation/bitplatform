namespace Bit.Belemetry;

/// <summary>
/// One entry of the diagnostic timeline: what happened, when, where (route, component) and how it relates to other
/// evidence (trace and span ids, exception fingerprint). Immutable, already sanitized, and made of plain values only -
/// never a live component, delegate, service or object graph.
/// </summary>
/// <remarks>
/// A missing value means it was not observed, never that it was guessed: <see cref="RouteSource"/> and
/// <see cref="ComponentSource"/> say whether a route or a component was observed by the framework or inferred.
/// </remarks>
public sealed class BelemetryDiagnosticEvent
{
    /// <summary>
    /// The order the timeline received it in, per source (<see cref="Source"/> <see cref="BelemetryDiagnosticSource.Browser"/>
    /// numbers its own). Arrival order: an asynchronous callback may arrive after an event that happened later, so the
    /// timeline is ordered by <see cref="TimeUnixNano"/>, and this breaks ties.
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>When it happened (its start, for something with a duration), in nanoseconds since the Unix epoch.</summary>
    public long TimeUnixNano { get; init; }

    /// <summary>
    /// Milliseconds since the source's clock started (the .NET process, or the page load in the browser), from a
    /// monotonic clock: comparable between events of one source even when the wall clock was adjusted.
    /// </summary>
    public double? ElapsedMilliseconds { get; init; }

    /// <summary>The kind of event.</summary>
    public BelemetryDiagnosticCategories Category { get; init; }

    /// <summary>A short, low-cardinality name: <c>navigate</c>, <c>handle_event</c>, <c>GET</c>, <c>exception</c>, <c>reconnect.failed</c>, ...</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The severity (OTLP numbers).</summary>
    public BelemetrySeverity Severity { get; init; } = BelemetrySeverity.Info;

    /// <summary>How it ended, when it says.</summary>
    public BelemetryDiagnosticOutcome Outcome { get; init; }

    /// <summary>Where it was observed.</summary>
    public BelemetryDiagnosticSource Source { get; init; }

    /// <summary>The consent feature of the record it came from (the one a revoke purges it by).</summary>
    public BelemetryFeatures Feature { get; init; }

    /// <summary>The W3C trace id, when the event belongs to a trace.</summary>
    public string? TraceId { get; init; }

    /// <summary>The span id, for a span, or the active span of a log entry.</summary>
    public string? SpanId { get; init; }

    /// <summary>The parent span id of a span, when it has one: never invented.</summary>
    public string? ParentSpanId { get; init; }

    /// <summary>The route: a template (<c>/orders/{id}</c>) - never a raw URL with its values.</summary>
    public string? Route { get; init; }

    /// <summary><c>framework</c> when the router reported the template; <c>inferred</c> when Belemetry templated a path (numbers and ids replaced).</summary>
    public string? RouteSource { get; init; }

    /// <summary>The component type (full name) the event belongs to, when known.</summary>
    public string? ComponentType { get; init; }

    /// <summary><c>framework</c> when the framework's activity named it; <c>stack</c> when inferred from the first application frame of an exception.</summary>
    public string? ComponentSource { get; init; }

    /// <summary>The duration, for something that took time.</summary>
    public double? DurationMilliseconds { get; init; }

    /// <summary>The exception, for an exception event.</summary>
    public BelemetryDiagnosticException? Exception { get; init; }

    /// <summary>A short, sanitized description (a log message template, an error type); never user-entered text.</summary>
    public string? Message { get; init; }

    /// <summary>Allowlisted, sanitized attributes: strings, numbers and booleans only.</summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = EmptyAttributes;

    /// <summary>How many values the sanitizer replaced or removed.</summary>
    public int Redactions { get; init; }

    /// <summary>Whether a value was cut to a size limit.</summary>
    public bool Truncated { get; init; }

    /// <summary>Whether it was recorded before the page last reloaded (browser timeline kept with <see cref="BelemetryDiagnosticsOptions.PersistBrowserTimeline"/>).</summary>
    public bool PreviousPage { get; init; }

    internal static readonly IReadOnlyDictionary<string, object?> EmptyAttributes = new Dictionary<string, object?>();

    /// <summary>The same event under another sequence: one numbered by another timeline (the server's trace index) joining a capture.</summary>
    internal BelemetryDiagnosticEvent WithSequence(long sequence) => new()
    {
        Sequence = sequence,
        TimeUnixNano = TimeUnixNano,
        ElapsedMilliseconds = ElapsedMilliseconds,
        Category = Category,
        Name = Name,
        Severity = Severity,
        Outcome = Outcome,
        Source = Source,
        Feature = Feature,
        TraceId = TraceId,
        SpanId = SpanId,
        ParentSpanId = ParentSpanId,
        Route = Route,
        RouteSource = RouteSource,
        ComponentType = ComponentType,
        ComponentSource = ComponentSource,
        DurationMilliseconds = DurationMilliseconds,
        Exception = Exception,
        Message = Message,
        Attributes = Attributes,
        Redactions = Redactions,
        Truncated = Truncated,
        PreviousPage = PreviousPage,
    };

    /// <summary>Whether this is a failure: an exception, a failed outcome, or Error severity and above.</summary>
    public bool IsFailure => Exception is not null || Outcome == BelemetryDiagnosticOutcome.Failure || Severity >= BelemetrySeverity.Error;

    /// <summary>The estimated memory it holds, in bytes (strings at two bytes per character plus a fixed overhead).</summary>
    internal int EstimatedBytes
    {
        get
        {
            var size = 160 + Size(Name) + Size(TraceId) + Size(SpanId) + Size(ParentSpanId) + Size(Route) + Size(ComponentType) + Size(Message);
            foreach (var (key, value) in Attributes) size += 32 + Size(key) + (value is string s ? Size(s) : 8);
            if (Exception is not null) size += Exception.EstimatedBytes;
            return size;

            static int Size(string? value) => value is null ? 0 : value.Length * 2;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"#{Sequence} [{Category}] {Name}";
}
