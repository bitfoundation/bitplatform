namespace Bit.Belemetry;

/// <summary>Exports errors to Sentry as events (Issues), posted to the project's envelope endpoint.</summary>
internal sealed class BelemetrySentryExporter : IBelemetryHttpExporter
{
    private readonly BelemetrySentryExporterOptions _options;

    /// <param name="options">The options.</param>
    /// <param name="dsn">Their DSN, parsed (<see cref="BelemetrySentryExporterSettings"/>).</param>
    /// <param name="name">The exporter's name: <c>sentry</c>, <c>sentry1</c>, ...</param>
    public BelemetrySentryExporter(BelemetrySentryExporterOptions options, SentryDsn dsn, string name)
    {
        _options = options;
        Name = name;
        Dsn = dsn;
        MinimumSeverity = BelemetryLogger.ToMinimumSeverity(options.MinimumEventLevel);
        Context = new SentryEnvelopeSerializer.Context(Dsn, string.IsNullOrWhiteSpace(options.Release) ? null : options.Release.Trim(),
                                                       MinimumSeverity, options.IncludeUnhandledExceptions, options.MaskIp);
    }

    public string Name { get; }

    public SentryDsn Dsn { get; }

    internal SentryEnvelopeSerializer.Context Context { get; }

    public TimeSpan Timeout => _options.Timeout;

    // The key is in the URL (SentryDsn.EnvelopeUrl): public, and what a page or a stored request needs.
    public IReadOnlyDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The minimum event level: plain log records below it are left out of the batch before it is serialized. Events
    /// (exceptions included) pass that step, and <see cref="SentryEnvelopeSerializer.IsEvent(BelemetryLogRecord, SentryEnvelopeSerializer.Context)"/>
    /// holds them to the level too.
    /// </summary>
    public BelemetrySeverity? MinimumSeverity { get; }

    public IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch)
    {
        // One event per envelope, as Sentry takes them.
        foreach (var body in SentryEnvelopeSerializer.Serialize(batch, Context))
        {
            yield return new BelemetryHttpRequest
            {
                Exporter = Name,
                Url = Dsn.EnvelopeUrl,
                ContentType = "application/x-sentry-envelope",
                ContentEncoding = _options.Compression == BelemetryCompression.Gzip ? "gzip" : null,
                Body = body,
                Count = 1,
            };
        }
    }

    public int CountItems(BelemetryBatch batch) => SentryEnvelopeSerializer.Count(batch, Context);

    public bool PostsTo(string url) => url == Dsn.EnvelopeUrl;

    public ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    // A rate limit (429, with Retry-After) and an overloaded or unreachable Relay are transient; anything else is final.
    public bool IsRetryable(int statusCode) => BelemetryHttpSender.IsOtlpRetryable(statusCode);

    public string? DescribePartialSuccess(string? mediaType, ReadOnlySpan<byte> body, out long rejected)
    {
        rejected = 0;
        return null;
    }

    public BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody, out int rejected)
    {
        rejected = 0;
        return null;
    }
}
