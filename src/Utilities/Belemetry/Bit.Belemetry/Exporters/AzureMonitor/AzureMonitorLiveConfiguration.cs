using System.Collections.Concurrent;
using System.Globalization;
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

    private readonly DocumentStream[] _streams;
    private readonly DerivedMetric[] _metrics;
    private readonly Error[] _errors;
    private readonly int _types;

    // A custom chart that failed on an item (a dimension that is no number): reported once per data point.
    private readonly ConcurrentDictionary<string, string> _failures = new(StringComparer.Ordinal);

    /// <summary>A configuration read by <see cref="Parse"/>: what applies, and what is reported back as left out.</summary>
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
    /// <param name="json">The body.</param>
    /// <param name="etag">The response's configuration etag.</param>
    /// <param name="time">The clock the streams' quotas earn samples back by; the system's when null.</param>
    /// <param name="previous">
    /// The configuration applied until now: a document stream or custom chart it has unchanged (the same id, definition
    /// and quotas) is carried over as it is - a stream with the quota it has left, a chart with what it gathered since
    /// the last data point - so a change elsewhere in the portal neither lets a burst of samples through again nor drops
    /// a chart's values.
    /// </param>
    /// <exception cref="JsonException">It is no JSON object.</exception>
    public static AzureMonitorLiveConfiguration Parse(byte[] json, string etag, TimeProvider? time = null, AzureMonitorLiveConfiguration? previous = null)
    {
        time ??= TimeProvider.System;
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

            var groupList = Property(info, "DocumentFilterGroups");
            if (IsList(groupList) is false)
            {
                errors.Add(new("DocumentStreamFailureToCreate", $"The filter groups are not a list ({groupList!.Value.ValueKind}).", "DocumentStreamInfoId", id));
                continue;
            }

            var groups = new List<(AzureMonitorLiveTelemetryType, AzureMonitorLiveFilter[])>();
            var index = -1;
            foreach (var group in Items(groupList))
            {
                index++;
                var typeName = EnumName<TelemetryType>(Property(group, "TelemetryType"));
                if (ToTelemetryType(typeName) is not { } type)
                {
                    errors.Add(new("DocumentStreamFailureToCreate", $"The telemetry type '{typeName}' is not supported: Belemetry streams no samples of it.", "DocumentStreamInfoId", id));
                    continue;
                }

                // The group's filters are one object here (FilterConjunctionGroupInfo), not a list of them as in a metric.
                // Only a group that cannot apply is left out: the stream's others still take what they match.
                var conjunction = Property(group, "Filters");
                if (IsObject(conjunction) is false)
                {
                    errors.Add(new("DocumentStreamFailureToCreateFilterUnexpected", $"Filter group {index}: the filters are not an object ({conjunction!.Value.ValueKind}).",
                                   "DocumentStreamInfoId", id));
                }
                else if (TryCreateFilters(type, Property(conjunction, "Filters"), out var filters, out var error))
                {
                    groups.Add((type, filters));
                }
                else
                {
                    errors.Add(new("DocumentStreamFailureToCreateFilterUnexpected", $"Filter group {index}: {error}", "DocumentStreamInfoId", id));
                }
            }

            var definition = string.Create(CultureInfo.InvariantCulture, $"{initialQuota} {maxQuota} {accrualRate} {info.GetRawText()}");
            streams.Add(previous?.FindStream(id) is { } kept && kept.Definition == definition
                ? kept
                : new DocumentStream(id, definition, [.. groups], initialQuota, maxQuota, accrualRate, time));
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

            var typeName = EnumName<TelemetryType>(Property(info, "TelemetryType"));
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

            var aggregationName = EnumName<Aggregation>(Property(info, "Aggregation"));
            if (Named<Aggregation>(aggregationName) is not { } aggregation)
            {
                errors.Add(new("MetricFailureToCreate", $"The aggregation '{aggregationName}' is not supported.", "MetricId", id));
                continue;
            }

            // Its filters are a list of groups (OR), each an AND of filters: one that cannot apply would change what the
            // metric counts, so the whole metric is left out.
            var groupList = Property(info, "FilterGroups");
            var groups = new List<AzureMonitorLiveFilter[]>();
            var failure = IsList(groupList) ? null : $"The filter groups are not a list ({groupList!.Value.ValueKind}).";
            var index = 0;
            foreach (var group in Items(groupList))
            {
                if (group.ValueKind != JsonValueKind.Object) failure = $"Filter group {index}: it is not an object ({group.ValueKind}).";
                else if (TryCreateFilters(type, Property(group, "Filters"), out var filters, out var error)) groups.Add(filters);
                else failure = $"Filter group {index}: {error}";

                if (failure is not null) break;
                index++;
            }
            if (failure is not null)
            {
                errors.Add(new("MetricFailureToCreateFilterUnexpected", failure, "MetricId", id));
                continue;
            }

            var definition = info.GetRawText();
            metrics.Add(previous?.FindMetric(id) is { } kept && kept.Definition == definition
                ? kept
                : new DerivedMetric(id, definition, type, [.. groups], projection, aggregation));
        }

        return new AzureMonitorLiveConfiguration(etag, [.. streams], [.. metrics], [.. errors]);
    }

    /// <summary>
    /// The configuration that stands for one the service sent and Belemetry cannot read: nothing to stream or chart, and
    /// the error reported back with every data point while it lasts. Its etag is the one the requests carry back, so the
    /// service does not send the same body again with every response.
    /// </summary>
    /// <param name="etag">The response's configuration etag.</param>
    /// <param name="reason">Why it cannot be read.</param>
    public static AzureMonitorLiveConfiguration Unreadable(string etag, string reason) =>
        new(etag, [], [], [new("CollectionConfigurationFailureToCreateUnexpected", $"The collection configuration could not be read: {reason}", string.Empty, string.Empty)]);

    /// <summary>Whether a stream or a custom chart takes items of <paramref name="type"/>: only then is it worth making one.</summary>
    public bool Takes(AzureMonitorLiveTelemetryType type) => (_types & (1 << (int)type)) != 0;

    /// <summary>
    /// The document streams that take <paramref name="item"/> as a sample - a group of theirs for its type matches, and
    /// they have quota left for it, which this spends - or null for none.
    /// </summary>
    public List<DocumentStream>? Sample(AzureMonitorLiveItem item)
    {
        List<DocumentStream>? taken = null;
        foreach (var stream in _streams)
        {
            if (stream.TryTake(item)) (taken ??= []).Add(stream);
        }
        return taken;
    }

    /// <summary>Whether every one of <paramref name="streams"/> is one of its own: a sample they took is one it would have taken.</summary>
    public bool Has(IReadOnlyList<DocumentStream> streams)
    {
        foreach (var stream in streams)
        {
            if (Array.IndexOf(_streams, stream) < 0) return false;
        }
        return true;
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
            var (value, count) = metric.Take();
            w.WriteNumber("Value", double.IsFinite(value) ? value : 0);
            // Weighed by the items it aggregated, as the Application Insights SDK weighs a derived metric's point: 0 for a
            // data point without any, which the service's own aggregation across instances then leaves out.
            w.WriteNumber("Weight", (int)Math.Min(count, int.MaxValue));
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

    /// <summary>Writes one <c>CollectionConfigurationError</c>, with the id of what it left out and the configuration's etag.</summary>
    private void WriteError(Utf8JsonWriter w, Error error)
    {
        w.WriteStartObject();
        w.WriteString("CollectionConfigurationErrorType", error.Type);
        w.WriteString("Message", error.Message);
        w.WriteString("FullException", string.Empty);
        w.WriteStartArray("Data");
        if (error.Key.Length > 0)
        {
            w.WriteStartObject();
            w.WriteString("key", error.Key);
            w.WriteString("value", error.Id);
            w.WriteEndObject();
        }
        w.WriteStartObject();
        w.WriteString("key", "ETag");
        w.WriteString("value", ETag);
        w.WriteEndObject();
        w.WriteEndArray();
        w.WriteEndObject();
    }

    /// <summary>
    /// The filters of a group (an AND of them), or false with the reason when one cannot apply - or when they are no list:
    /// taken as none, they would match every item.
    /// </summary>
    private static bool TryCreateFilters(AzureMonitorLiveTelemetryType type, JsonElement? list, out AzureMonitorLiveFilter[] filters, out string error)
    {
        if (IsList(list) is false)
        {
            filters = [];
            error = $"The filters are not a list ({list!.Value.ValueKind}).";
            return false;
        }

        var created = new List<AzureMonitorLiveFilter>();
        foreach (var filter in Items(list))
        {
            try
            {
                created.Add(AzureMonitorLiveFilter.Create(type, String(Property(filter, "FieldName")), EnumName<AzureMonitorLiveFilter.Predicate>(Property(filter, "Predicate")),
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
        || (projection.StartsWith(AzureMonitorLiveFilter.DimensionPrefix, StringComparison.Ordinal) && projection.Length > AzureMonitorLiveFilter.DimensionPrefix.Length);

    /// <summary>The quota of each stream (<c>QuotaInfo</c>), or the Application Insights SDK's when the service sets none it can read.</summary>
    private static (double Initial, double Max, double AccrualRate) ReadQuota(JsonElement? info)
    {
        if (Number(Property(info, "MaxQuota")) is { } max && max >= 0 && Number(Property(info, "QuotaAccrualRatePerSec")) is { } rate && rate >= 0)
        {
            var initial = Number(Property(info, "InitialQuota")) is { } i && i >= 0 ? i : Math.Min(DefaultInitialQuota, max);
            return (Math.Min(initial, max), max, rate);
        }

        return (DefaultInitialQuota, DefaultMaxQuota, DefaultMaxQuota / 60);
    }

    /// <summary>The telemetry type Belemetry has items of, by its name; null for the others (metrics, performance counters).</summary>
    private static AzureMonitorLiveTelemetryType? ToTelemetryType(string? name) => Named<TelemetryType>(name) switch
    {
        TelemetryType.Request => AzureMonitorLiveTelemetryType.Request,
        TelemetryType.Dependency => AzureMonitorLiveTelemetryType.Dependency,
        TelemetryType.Exception => AzureMonitorLiveTelemetryType.Exception,
        TelemetryType.Event => AzureMonitorLiveTelemetryType.Event,
        TelemetryType.Trace => AzureMonitorLiveTelemetryType.Trace,
        _ => null,
    };

    /// <summary>The document stream of id <paramref name="id"/>; null for none.</summary>
    private DocumentStream? FindStream(string id) => Array.Find(_streams, s => s.Id == id);

    /// <summary>The custom chart of id <paramref name="id"/>; null for none.</summary>
    private DerivedMetric? FindMetric(string id) => Array.Find(_metrics, m => m.Id == id);

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

    /// <summary>The items of an array; none when <paramref name="element"/> is no array (see <see cref="IsList"/>).</summary>
    private static IEnumerable<JsonElement> Items(JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];

    /// <summary>Whether <paramref name="element"/> reads as a list: an array, or nothing at all (absent or null), an empty one.</summary>
    private static bool IsList(JsonElement? element) => element is null or { ValueKind: JsonValueKind.Array or JsonValueKind.Null };

    /// <summary>Whether <paramref name="element"/> reads as an object: one, or nothing at all (absent or null), an empty one.</summary>
    private static bool IsObject(JsonElement? element) => element is null or { ValueKind: JsonValueKind.Object or JsonValueKind.Null };

    /// <summary>A string value; null for anything else.</summary>
    private static string? String(JsonElement? element) => element is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    /// <summary>A finite number; null for anything else.</summary>
    private static double? Number(JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) && double.IsFinite(number) ? number : null;

    /// <summary>
    /// The name of a value of <typeparamref name="TEnum"/>: by its name, in any case, as this API version sends it, or by
    /// its number, as the older protocol's serializer wrote it - the enum's own names and numbers, which cannot drift from
    /// it. Any other string is returned as it is, for the error that reports it; anything else is null.
    /// </summary>
    private static string? EnumName<TEnum>(JsonElement? element) where TEnum : struct, Enum
    {
        if (String(element) is { } name)
        {
            foreach (var value in Enum.GetValues<TEnum>())
            {
                if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase)) return value.ToString();
            }
            return name;
        }

        if (element is { ValueKind: JsonValueKind.Number } number && number.TryGetInt32(out var n))
        {
            foreach (var value in Enum.GetValues<TEnum>())
            {
                if (Convert.ToInt32(value, CultureInfo.InvariantCulture) == n) return value.ToString();
            }
        }
        return null;
    }

    /// <summary>The value of <typeparamref name="TEnum"/> named <paramref name="name"/>, in its case; null for none - a number included.</summary>
    internal static TEnum? Named<TEnum>(string? name) where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (value.ToString() == name) return value;
        }
        return null;
    }

    /// <summary>A <c>CollectionConfigurationError</c>: its type, message, and the key and id of what it is about (none when the key is empty).</summary>
    private readonly record struct Error(string Type, string Message, string Key, string Id);

    /// <summary>The telemetry types (<c>TelemetryType</c>), by their names and numbers in the protocol.</summary>
    private enum TelemetryType
    {
        Request, Dependency, Exception, Event, Metric, PerformanceCounter, Trace,
    }

    /// <summary>The aggregations (<c>AggregationType</c>), by their names and numbers in the protocol.</summary>
    private enum Aggregation
    {
        Avg, Sum, Min, Max,
    }

    /// <summary>A stream of sample telemetry (<c>DocumentStreamInfo</c>): filter groups (OR) per telemetry type, and a quota per type.</summary>
    internal sealed class DocumentStream
    {
        private readonly (AzureMonitorLiveTelemetryType Type, AzureMonitorLiveFilter[] Filters)[] _groups;
        private readonly Quota[] _quotas;

        /// <summary>A stream with its filter groups, and a quota per telemetry type that earns samples back by <paramref name="time"/>.</summary>
        public DocumentStream(string id, string definition, (AzureMonitorLiveTelemetryType, AzureMonitorLiveFilter[])[] groups, double initialQuota, double maxQuota,
                              double accrualRate, TimeProvider time)
        {
            Id = id;
            Definition = definition;
            _groups = groups;
            _quotas = new Quota[Enum.GetValues<AzureMonitorLiveTelemetryType>().Length];
            for (var i = 0; i < _quotas.Length; i++) _quotas[i] = new Quota(initialQuota, maxQuota, accrualRate, time);
            foreach (var (type, _) in groups) Types |= 1 << (int)type;
        }

        public string Id { get; }

        /// <summary>What it was made of - its JSON and the quotas - which a configuration that carries it over has the same.</summary>
        public string Definition { get; }

        /// <summary>The telemetry types it has a filter group for, as bits.</summary>
        public int Types { get; }

        /// <summary>Takes <paramref name="item"/> as a sample when a group of its type matches and it has quota left for it, which this spends.</summary>
        public bool TryTake(AzureMonitorLiveItem item)
        {
            foreach (var (type, filters) in _groups)
            {
                if (type == item.Type && AzureMonitorLiveFilter.MatchesAll(filters, item)) return _quotas[(int)type].TryTake();
            }
            return false;
        }
    }

    /// <summary>
    /// How many samples a stream may take of one telemetry type: it starts with some, spends one per sample, and earns
    /// them back over time up to a cap - a burst shows at once, a flood does not swamp the portal.
    /// </summary>
    private sealed class Quota(double initial, double max, double accrualRatePerSecond, TimeProvider time)
    {
        private readonly object _lock = new();
        private double _available = initial;
        private long _last = time.GetTimestamp();

        /// <summary>Spends one sample, after adding what it earned since the last call; false when none is left.</summary>
        public bool TryTake()
        {
            lock (_lock)
            {
                var now = time.GetTimestamp();
                _available = Math.Min(max, _available + time.GetElapsedTime(_last, now).TotalSeconds * accrualRatePerSecond);
                _last = now;

                if (_available < 1) return false;
                _available--;
                return true;
            }
        }
    }

    /// <summary>A custom chart (<c>DerivedMetricInfo</c>): the items of one type its filter groups (OR) match, projected and aggregated per data point.</summary>
    private sealed class DerivedMetric(string id, string definition, AzureMonitorLiveTelemetryType type, AzureMonitorLiveFilter[][] groups, string projection,
                                       Aggregation aggregation)
    {
        private readonly object _lock = new();
        private double _sum, _min, _max;
        private long _count;

        public string Id => id;

        /// <summary>Its JSON, which a configuration that carries it over has the same.</summary>
        public string Definition => definition;

        public AzureMonitorLiveTelemetryType Type => type;

        /// <summary>Whether it takes <paramref name="item"/>: of its type, and matching a filter group (any, without one).</summary>
        public bool Matches(AzureMonitorLiveItem item)
        {
            if (item.Type != type) return false;
            if (groups.Length == 0) return true;

            foreach (var group in groups)
            {
                if (AzureMonitorLiveFilter.MatchesAll(group, item)) return true;
            }
            return false;
        }

        /// <summary>The value its projection takes of <paramref name="item"/>, or false with the reason when it has none.</summary>
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

            var key = projection[AzureMonitorLiveFilter.DimensionPrefix.Length..];
            if (item.TryGetDimension(key, out var text) is false)
            {
                error = $"Could not calculate the projection because the custom dimension '{key}' was not found in the telemetry item.";
                return false;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) is false || double.IsFinite(value) is false)
            {
                error = $"Could not calculate the projection because the custom dimension value '{text}' for the dimension '{key}' is not a valid number.";
                return false;
            }
            return true;
        }

        /// <summary>Adds a projected value to the data point's aggregate.</summary>
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

        /// <summary>The aggregate since the last call (0 without any item) and the number of items in it, and starts over.</summary>
        public (double Value, long Count) Take()
        {
            lock (_lock)
            {
                var count = _count;
                var value = count == 0 ? 0 : aggregation switch
                {
                    Aggregation.Sum => _sum,
                    Aggregation.Min => _min,
                    Aggregation.Max => _max,
                    _ => _sum / count,
                };

                _sum = _min = _max = 0;
                _count = 0;
                return (value, count);
            }
        }
    }
}
