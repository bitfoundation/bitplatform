using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// The collection configuration the Live Metrics service sends (<c>CollectionConfigurationInfo</c>) while someone watches:
/// the portal's sample telemetry streams with their filters (<c>DocumentStreams</c>), its custom charts
/// (<c>Metrics</c>, derived metrics), and how many samples a stream may take (<c>QuotaInfo</c>). What Belemetry cannot
/// apply is reported back (<c>CollectionConfigurationErrors</c>) and left out; the rest still applies.
/// </summary>
/// <remarks>Read by every thread that records, and by the stream's loop: its state is locked per stream and per metric.</remarks>
internal sealed class AzureMonitorLiveConfiguration
{
    /// <summary>The samples a stream may take of each telemetry type at first, as the Application Insights SDK allows.</summary>
    internal const double DefaultInitialQuota = 3;

    /// <summary>The most samples a stream may save up of each telemetry type; it earns them back at a 60th of that per second.</summary>
    internal const double DefaultMaxQuota = 30;

    /// <summary>The telemetry types, in the order of their numbers in the older protocol.</summary>
    private static readonly string[] _telemetryTypes = ["Request", "Dependency", "Exception", "Event", "Metric", "PerformanceCounter", "Trace"];

    private static readonly string[] _aggregations = ["Avg", "Sum", "Min", "Max"];

    private readonly DocumentStream[] _streams;
    private readonly DerivedMetric[] _metrics;
    private readonly Error[] _errors;
    private readonly int _types;

    // A custom chart that failed on an item (a dimension that is no number): reported once per data point.
    private readonly ConcurrentDictionary<string, string> _failures = new(StringComparer.Ordinal);

    private AzureMonitorLiveConfiguration(string etag, DocumentStream[] streams, DerivedMetric[] metrics, Error[] errors)
    {
        ETag = etag;
        _streams = streams;
        _metrics = metrics;
        _errors = errors;

        foreach (var stream in streams) _types |= stream.Types;
        foreach (var metric in metrics) _types |= 1 << (int)metric.Type;
    }

    /// <summary>The configuration's etag, which the requests carry back (<c>x-ms-qps-configuration-etag</c>).</summary>
    public string ETag { get; }

    /// <summary>The ids of the document streams, in the configuration's order.</summary>
    internal IEnumerable<string> StreamIds => _streams.Select(s => s.Id);

    /// <summary>The ids of the custom charts Belemetry computes, in the configuration's order.</summary>
    internal IEnumerable<string> MetricIds => _metrics.Select(m => m.Id);

    /// <summary>Reads the body of a ping or post response.</summary>
    /// <exception cref="JsonException">It is no JSON object.</exception>
    public static AzureMonitorLiveConfiguration Parse(byte[] json, string etag)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("The collection configuration is not a JSON object.");

        var errors = new List<Error>();
        var (initialQuota, maxQuota, accrualRate) = ReadQuota(Property(root, "QuotaInfo"));

        var streams = new List<DocumentStream>();
        foreach (var info in Items(Property(root, "DocumentStreams")))
        {
            var id = String(Property(info, "Id"));
            if (string.IsNullOrEmpty(id))
            {
                errors.Add(new("DocumentStreamFailureToCreate", "A document stream must have an id.", "DocumentStreamInfoId", string.Empty));
                continue;
            }
            if (streams.Any(s => s.Id == id))
            {
                errors.Add(new("DocumentStreamDuplicateIds", $"Duplicate document stream id: {id}", "DocumentStreamInfoId", id));
                continue;
            }

            var groups = new List<(AzureMonitorLiveTelemetryType, AzureMonitorLiveFilter[])>();
            foreach (var group in Items(Property(info, "DocumentFilterGroups")))
            {
                var typeName = EnumName(Property(group, "TelemetryType"), _telemetryTypes);
                if (ToTelemetryType(typeName) is not { } type)
                {
                    errors.Add(new("DocumentStreamFailureToCreate", $"The telemetry type '{typeName}' is not supported: Belemetry streams no samples of it.", "DocumentStreamInfoId", id));
                    continue;
                }

                // The group's filters are one object here (FilterConjunctionGroupInfo), not a list of them as in a metric.
                if (TryCreateFilters(type, Property(Property(group, "Filters"), "Filters"), out var filters, out var error))
                {
                    groups.Add((type, filters));
                }
                else
                {
                    // Only this group is left out: the stream's others still take what they match.
                    errors.Add(new("DocumentStreamFailureToCreateFilterUnexpected", error, "DocumentStreamInfoId", id));
                }
            }

            streams.Add(new DocumentStream(id, [.. groups], initialQuota, maxQuota, accrualRate));
        }

        var metrics = new List<DerivedMetric>();
        foreach (var info in Items(Property(root, "Metrics")))
        {
            var id = String(Property(info, "Id"));
            if (string.IsNullOrEmpty(id))
            {
                errors.Add(new("MetricFailureToCreate", "A metric must have an id.", "MetricId", string.Empty));
                continue;
            }
            if (metrics.Any(m => m.Id == id))
            {
                errors.Add(new("MetricDuplicateIds", $"Duplicate metric id: {id}", "MetricId", id));
                continue;
            }

            var typeName = EnumName(Property(info, "TelemetryType"), _telemetryTypes);
            if (ToTelemetryType(typeName) is not { } type)
            {
                errors.Add(new("MetricTelemetryTypeUnsupported", $"The telemetry type '{typeName}' is not supported: Belemetry has no items of it.", "MetricId", id));
                continue;
            }

            var projection = String(Property(info, "Projection"));
            if (projection is null || IsSupportedProjection(type, projection) is false)
            {
                errors.Add(new("MetricFailureToCreate", $"The projection '{projection}' is not supported for the telemetry type {type}.", "MetricId", id));
                continue;
            }

            var aggregationName = EnumName(Property(info, "Aggregation"), _aggregations);
            var aggregation = Array.IndexOf(_aggregations, aggregationName);
            if (aggregation < 0)
            {
                errors.Add(new("MetricFailureToCreate", $"The aggregation '{aggregationName}' is not supported.", "MetricId", id));
                continue;
            }

            // Its filters are a list of groups (OR), each an AND of filters: one that cannot apply would change what the
            // metric counts, so the whole metric is left out.
            var groups = new List<AzureMonitorLiveFilter[]>();
            string? failure = null;
            foreach (var group in Items(Property(info, "FilterGroups")))
            {
                if (TryCreateFilters(type, Property(group, "Filters"), out var filters, out var error))
                {
                    groups.Add(filters);
                }
                else
                {
                    failure = error;
                    break;
                }
            }
            if (failure is not null)
            {
                errors.Add(new("MetricFailureToCreateFilterUnexpected", failure, "MetricId", id));
                continue;
            }

            metrics.Add(new DerivedMetric(id, type, [.. groups], projection, (Aggregation)aggregation));
        }

        return new AzureMonitorLiveConfiguration(etag, [.. streams], [.. metrics], [.. errors]);
    }

    /// <summary>Whether a stream or a custom chart takes items of <paramref name="type"/>: only then is it worth making one.</summary>
    public bool Takes(AzureMonitorLiveTelemetryType type) => (_types & (1 << (int)type)) != 0;

    /// <summary>
    /// The ids of the document streams that take <paramref name="item"/> as a sample - a group of theirs for its type
    /// matches, and they have quota left for it - or null for none.
    /// </summary>
    public List<string>? Sample(AzureMonitorLiveItem item)
    {
        List<string>? ids = null;
        foreach (var stream in _streams)
        {
            if (stream.Takes(item)) (ids ??= []).Add(stream.Id);
        }
        return ids;
    }

    /// <summary>Adds <paramref name="item"/> to the custom charts whose filters it matches.</summary>
    public void Project(AzureMonitorLiveItem item)
    {
        foreach (var metric in _metrics)
        {
            if (metric.Matches(item) is false) continue;

            if (metric.TryProject(item, out var value, out var error)) metric.Add(value);
            else _failures.TryAdd(metric.Id, error);
        }
    }

    /// <summary>Writes a point (<c>MetricPoint</c>) per custom chart, with what it gathered since the last one, and starts over.</summary>
    public void WriteMetrics(Utf8JsonWriter w)
    {
        foreach (var metric in _metrics)
        {
            w.WriteStartObject();
            w.WriteString("Name", metric.Id);
            var value = metric.Take();
            w.WriteNumber("Value", double.IsFinite(value) ? value : 0);
            w.WriteNumber("Weight", 1);
            w.WriteEndObject();
        }
    }

    /// <summary>
    /// Writes what Belemetry could not apply (<c>CollectionConfigurationError</c>): the parts of the configuration it left
    /// out, with every data point while it lasts, and the custom charts that failed on an item since the last one.
    /// </summary>
    public void WriteErrors(Utf8JsonWriter w)
    {
        foreach (var error in _errors) WriteError(w, error);

        foreach (var id in _failures.Keys)
        {
            if (_failures.TryRemove(id, out var message)) WriteError(w, new Error("MetricFailureToCreate", message, "MetricId", id));
        }
    }

    private void WriteError(Utf8JsonWriter w, Error error)
    {
        w.WriteStartObject();
        w.WriteString("CollectionConfigurationErrorType", error.Type);
        w.WriteString("Message", error.Message);
        w.WriteString("FullException", string.Empty);
        w.WriteStartArray("Data");
        w.WriteStartObject();
        w.WriteString("key", error.Key);
        w.WriteString("value", error.Id);
        w.WriteEndObject();
        w.WriteStartObject();
        w.WriteString("key", "ETag");
        w.WriteString("value", ETag);
        w.WriteEndObject();
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static bool TryCreateFilters(AzureMonitorLiveTelemetryType type, JsonElement? list, out AzureMonitorLiveFilter[] filters, out string error)
    {
        var created = new List<AzureMonitorLiveFilter>();
        foreach (var filter in Items(list))
        {
            try
            {
                created.Add(AzureMonitorLiveFilter.Create(type, String(Property(filter, "FieldName")),
                                                          EnumName(Property(filter, "Predicate"), AzureMonitorLiveFilter.Predicates),
                                                          String(Property(filter, "Comparand"))));
            }
            catch (FormatException ex)
            {
                filters = [];
                error = ex.Message;
                return false;
            }
        }

        filters = [.. created];
        error = string.Empty;
        return true;
    }

    private static bool IsSupportedProjection(AzureMonitorLiveTelemetryType type, string projection) =>
        projection == "Count()"
        || (projection == "Duration" && type is AzureMonitorLiveTelemetryType.Request or AzureMonitorLiveTelemetryType.Dependency)
        || (projection.StartsWith(DerivedMetric.DimensionPrefix, StringComparison.Ordinal) && projection.Length > DerivedMetric.DimensionPrefix.Length);

    private static (double Initial, double Max, double AccrualRate) ReadQuota(JsonElement? info)
    {
        if (Number(Property(info, "MaxQuota")) is { } max && max >= 0 && Number(Property(info, "QuotaAccrualRatePerSec")) is { } rate && rate >= 0)
        {
            var initial = Number(Property(info, "InitialQuota")) is { } i && i >= 0 ? i : Math.Min(DefaultInitialQuota, max);
            return (Math.Min(initial, max), max, rate);
        }

        return (DefaultInitialQuota, DefaultMaxQuota, DefaultMaxQuota / 60);
    }

    private static AzureMonitorLiveTelemetryType? ToTelemetryType(string? name) => name switch
    {
        "Request" => AzureMonitorLiveTelemetryType.Request,
        "Dependency" => AzureMonitorLiveTelemetryType.Dependency,
        "Exception" => AzureMonitorLiveTelemetryType.Exception,
        "Event" => AzureMonitorLiveTelemetryType.Event,
        "Trace" => AzureMonitorLiveTelemetryType.Trace,
        _ => null,
    };

    /// <summary>A property by name, ignoring its case; null when <paramref name="element"/> is no object or has none.</summary>
    private static JsonElement? Property(JsonElement? element, string name)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value) return null;

        foreach (var property in value.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }
        return null;
    }

    private static IEnumerable<JsonElement> Items(JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];

    private static string? String(JsonElement? element) => element is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static double? Number(JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) && double.IsFinite(number) ? number : null;

    /// <summary>
    /// An enum value: its name, as this API version sends it, or its number, as the older protocol's serializer wrote
    /// it. The name's case is the API's.
    /// </summary>
    private static string? EnumName(JsonElement? element, string[] names)
    {
        if (String(element) is { } name)
        {
            return Array.Find(names, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) ?? name;
        }

        return element is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var index) && index >= 0 && index < names.Length ? names[index] : null;
    }

    private readonly record struct Error(string Type, string Message, string Key, string Id);

    private enum Aggregation
    {
        Avg, Sum, Min, Max,
    }

    /// <summary>A stream of sample telemetry (<c>DocumentStreamInfo</c>): filter groups (OR) per telemetry type, and a quota per type.</summary>
    private sealed class DocumentStream
    {
        private readonly (AzureMonitorLiveTelemetryType Type, AzureMonitorLiveFilter[] Filters)[] _groups;
        private readonly Quota[] _quotas;

        public DocumentStream(string id, (AzureMonitorLiveTelemetryType, AzureMonitorLiveFilter[])[] groups, double initialQuota, double maxQuota, double accrualRate)
        {
            Id = id;
            _groups = groups;
            _quotas = new Quota[Enum.GetValues<AzureMonitorLiveTelemetryType>().Length];
            for (var i = 0; i < _quotas.Length; i++) _quotas[i] = new Quota(initialQuota, maxQuota, accrualRate);
            foreach (var (type, _) in groups) Types |= 1 << (int)type;
        }

        public string Id { get; }

        /// <summary>The telemetry types it has a filter group for, as bits.</summary>
        public int Types { get; }

        public bool Takes(AzureMonitorLiveItem item)
        {
            foreach (var (type, filters) in _groups)
            {
                if (type == item.Type && filters.All(f => f.Matches(item))) return _quotas[(int)type].TryTake();
            }
            return false;
        }
    }

    /// <summary>
    /// How many samples a stream may take of one telemetry type: it starts with some, spends one per sample, and earns
    /// them back over time up to a cap - a burst shows at once, a flood does not swamp the portal.
    /// </summary>
    private sealed class Quota(double initial, double max, double accrualRatePerSecond)
    {
        private readonly object _lock = new();
        private double _available = initial;
        private long _last = Stopwatch.GetTimestamp();

        public bool TryTake()
        {
            lock (_lock)
            {
                var now = Stopwatch.GetTimestamp();
                _available = Math.Min(max, _available + Stopwatch.GetElapsedTime(_last, now).TotalSeconds * accrualRatePerSecond);
                _last = now;

                if (_available < 1) return false;
                _available--;
                return true;
            }
        }
    }

    /// <summary>A custom chart (<c>DerivedMetricInfo</c>): the items of one type its filter groups (OR) match, projected and aggregated per data point.</summary>
    private sealed class DerivedMetric(string id, AzureMonitorLiveTelemetryType type, AzureMonitorLiveFilter[][] groups, string projection, Aggregation aggregation)
    {
        internal const string DimensionPrefix = "CustomDimensions.";

        private readonly object _lock = new();
        private double _sum, _min, _max;
        private long _count;

        public string Id => id;

        public AzureMonitorLiveTelemetryType Type => type;

        /// <summary>Whether it takes <paramref name="item"/>: of its type, and matching a filter group (any, without one).</summary>
        public bool Matches(AzureMonitorLiveItem item) => item.Type == type && (groups.Length == 0 || groups.Any(g => g.All(f => f.Matches(item))));

        public bool TryProject(AzureMonitorLiveItem item, out double value, out string error)
        {
            error = string.Empty;
            value = 1;
            if (projection == "Count()") return true;

            if (projection == "Duration")
            {
                value = item.Duration.TotalMilliseconds;
                return true;
            }

            var key = projection[DimensionPrefix.Length..];
            if (item.TryGetDimension(key, out var text) is false)
            {
                error = $"Could not calculate the projection because the custom dimension '{key}' was not found in the telemetry item.";
                return false;
            }
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) is false || double.IsFinite(value) is false)
            {
                error = $"Could not calculate the projection because the custom dimension value '{text}' for the dimension '{key}' is not a valid number.";
                return false;
            }
            return true;
        }

        public void Add(double value)
        {
            lock (_lock)
            {
                _min = _count == 0 ? value : Math.Min(_min, value);
                _max = _count == 0 ? value : Math.Max(_max, value);
                _sum += value;
                _count++;
            }
        }

        /// <summary>The aggregate since the last call (0 without any item), and starts over.</summary>
        public double Take()
        {
            lock (_lock)
            {
                var value = _count == 0 ? 0 : aggregation switch
                {
                    Aggregation.Sum => _sum,
                    Aggregation.Min => _min,
                    Aggregation.Max => _max,
                    _ => _sum / _count,
                };

                _sum = _min = _max = 0;
                _count = 0;
                return value;
            }
        }
    }
}
