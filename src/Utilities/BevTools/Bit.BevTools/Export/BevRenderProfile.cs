using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>Render totals of one component type's instances that the registry let go after their retention.</summary>
internal sealed record BevGoneRenderTotals
{
    public required string TypeName { get; init; }

    public required string DisplayName { get; init; }

    public int Instances { get; set; }

    public int Renders { get; set; }

    public double RenderMs { get; set; }

    public double MaxRenderMs { get; set; }

    public int InstancesSinceBaseline { get; set; }

    public int RendersSinceBaseline { get; set; }

    public double RenderMsSinceBaseline { get; set; }

    public double MaxRenderMsSinceBaseline { get; set; }
}

/// <summary>What one component type rendered in a profile's window. <see cref="Type"/> is <see cref="BevRenderProfile.TypeKey"/>.</summary>
internal sealed record BevProfileTypeTotals(string Type, string DisplayName, int Instances, int Renders, double RenderMs, double MaxRenderMs, double? P95RenderMs, int RetainedSamples);

/// <summary>
/// What rendered, per component type, in the profiler's window: since the baseline when one was set, else since the
/// session started. The unit of comparison between two recordings, where instance ids mean nothing. Counted from the
/// session-long counters (and the totals of components already let go), so it adds up however few samples are left;
/// only the percentile comes from the retained samples.
/// </summary>
internal sealed class BevRenderProfile
{
    public bool SinceBaseline { get; init; }

    public DateTimeOffset? BaselineAt { get; init; }

    public DateTimeOffset? RecordedAt { get; init; }

    public bool Truncated { get; init; }

    public string? Runtime { get; init; }

    public string? RenderMode { get; init; }

    /// <summary>
    /// Read from the per-instance figures of an export that has no profile section: it misses components let go before
    /// the export, and its maxima are not limited to the reset window.
    /// </summary>
    public bool FromComponents { get; init; }

    public required IReadOnlyList<BevProfileTypeTotals> Types { get; init; }

    private static readonly Regex AssemblyQualification = new(@",(?>\s*[^,\[\]]+),\s*Version=[^,\]]*,\s*Culture=[^,\]]*,\s*PublicKeyToken=[^,\]]*", RegexOptions.CultureInvariant);

    /// <summary>
    /// The name two recordings match a type by: the full name without the assembly versions a generic type's arguments
    /// carry (<c>Grid`1[[App.Product, App, Version=1.2.0.0, …]]</c>), which change with every stamped build.
    /// </summary>
    internal static string TypeKey(string typeName) => typeName.Contains("Version=", StringComparison.Ordinal) ? AssemblyQualification.Replace(typeName, "") : typeName;

    public static BevRenderProfile Of(BevToolsSession session)
    {
        var baseline = session.Components.BaselineAt;
        var since = baseline is not null;
        var types = new Dictionary<string, Accumulator>();
        Accumulator For(string type, string displayName) => types.TryGetValue(TypeKey(type), out var known) ? known : types[TypeKey(type)] = new Accumulator(displayName);

        foreach (var record in session.Components.All)
        {
            var renders = since ? record.RendersSinceBaseline : record.RenderCount;
            if (record.IsBevTools || record.IsHidden || renders == 0)
            {
                continue;
            }

            var type = For(record.TypeName, record.DisplayName);
            type.Instances++;
            type.Renders += renders;
            type.Ms += since ? record.RenderMsSinceBaseline : record.TotalRenderMs;
            type.Max = Math.Max(type.Max, since ? record.MaxRenderMsSinceBaseline : record.MaxRenderMs);
            var window = since ? record.BaselineAt : null;
            type.Samples.AddRange(record.Renders.Where(s => window is not { } at || s.At >= at).Select(s => s.DurationMs));
        }

        foreach (var gone in session.Components.GoneTotals())
        {
            var renders = since ? gone.RendersSinceBaseline : gone.Renders;
            if (renders == 0)
            {
                continue;
            }

            var type = For(gone.TypeName, gone.DisplayName);
            type.Instances += since ? gone.InstancesSinceBaseline : gone.Instances;
            type.Renders += renders;
            type.Ms += since ? gone.RenderMsSinceBaseline : gone.RenderMs;
            type.Max = Math.Max(type.Max, since ? gone.MaxRenderMsSinceBaseline : gone.MaxRenderMs);
        }

        return new BevRenderProfile
        {
            SinceBaseline = since,
            BaselineAt = baseline,
            RecordedAt = DateTimeOffset.UtcNow,
            Truncated = session.Components.IsTruncated,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            RenderMode = session.RenderModeName,
            Types = types.Select(t =>
            {
                t.Value.Samples.Sort();
                return new BevProfileTypeTotals(t.Key, t.Value.DisplayName, t.Value.Instances, t.Value.Renders, t.Value.Ms, t.Value.Max,
                    t.Value.Samples.Count == 0 ? null : BevStats.Percentile(t.Value.Samples, 95), t.Value.Samples.Count);
            }).OrderByDescending(t => t.RenderMs).ToList(),
        };
    }

    private sealed class Accumulator(string displayName)
    {
        public string DisplayName { get; } = displayName;

        public int Instances { get; set; }

        public int Renders { get; set; }

        public double Ms { get; set; }

        public double Max { get; set; }

        public List<double> Samples { get; } = [];
    }

    /// <summary>Writes the <c>profile</c> section of an export.</summary>
    public void Write(Utf8JsonWriter w)
    {
        w.WriteStartObject("profile");
        w.WriteBoolean("sinceBaseline", SinceBaseline);
        if (BaselineAt is { } baseline)
        {
            w.WriteString("baselineAt", baseline);
        }

        w.WriteBoolean("truncated", Truncated);
        w.WriteStartArray("types");
        foreach (var t in Types)
        {
            w.WriteStartObject();
            w.WriteString("type", t.Type);
            w.WriteString("displayName", t.DisplayName);
            w.WriteNumber("instances", t.Instances);
            w.WriteNumber("renders", t.Renders);
            w.WriteNumber("renderMs", Math.Round(t.RenderMs, 3));
            w.WriteNumber("maxRenderMs", Math.Round(t.MaxRenderMs, 3));
            if (t.P95RenderMs is { } p95)
            {
                w.WriteNumber("p95RenderMs", Math.Round(p95, 3));
            }

            w.WriteNumber("retainedSamples", t.RetainedSamples);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 256 };

    /// <summary>
    /// Reads the profile of a session export: its <c>profile</c> section, or, for an export written before there was
    /// one, the per-instance figures of its <c>components</c> section added up per type.
    /// </summary>
    /// <exception cref="FormatException">The text is not a BevTools export, or has neither section.</exception>
    public static BevRenderProfile Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            return Read(document);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Not JSON: " + ex.Message, ex);
        }
    }

    /// <inheritdoc cref="Parse(string)"/>
    /// <remarks>Reads the UTF-8 stream as it comes, without a string copy of the whole file.</remarks>
    public static async Task<BevRenderProfile> ParseAsync(Stream utf8Json, CancellationToken cancellationToken = default)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(utf8Json, DocumentOptions, cancellationToken);
            return Read(document);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Not JSON: " + ex.Message, ex);
        }
    }

    private static BevRenderProfile Read(JsonDocument document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
            || format.GetString()?.StartsWith("bit-bevtools/", StringComparison.Ordinal) != true)
        {
            throw new FormatException("Not a BevTools session export: it has no \"format\": \"bit-bevtools/…\".");
        }

        var session = root.TryGetProperty("session", out var s) && s.ValueKind == JsonValueKind.Object ? s : default;
        var recordedAt = Date(root, "exportedAt");
        var runtime = Line(String(session, "runtime"));
        var renderMode = Line(String(session, "renderMode"));
        if (root.TryGetProperty("profile", out var profile) && profile.ValueKind == JsonValueKind.Object)
        {
            return new BevRenderProfile
            {
                SinceBaseline = profile.TryGetProperty("sinceBaseline", out var since) && since.ValueKind == JsonValueKind.True,
                BaselineAt = Date(profile, "baselineAt"),
                RecordedAt = recordedAt,
                Truncated = profile.TryGetProperty("truncated", out var truncated) && truncated.ValueKind == JsonValueKind.True,
                Runtime = runtime,
                RenderMode = renderMode,
                Types = Items(profile, "types").Select(t => new BevProfileTypeTotals(
                    TypeKey(String(t, "type") ?? "?"), Line(String(t, "displayName") ?? String(t, "type")) ?? "?", Int(t, "instances"), Int(t, "renders"),
                    Number(t, "renderMs") ?? 0, Number(t, "maxRenderMs") ?? 0, Number(t, "p95RenderMs"), Int(t, "retainedSamples"))).ToList(),
            };
        }

        if (root.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Object)
        {
            var baseline = Date(components, "baselineAt");
            var since = baseline is not null;
            var types = Items(components, "items")
                .Where(c => !(c.TryGetProperty("hidden", out var hidden) && hidden.ValueKind == JsonValueKind.True))
                .Select(c => (Type: TypeKey(String(c, "type") ?? "?"), Display: Line(String(c, "displayName") ?? String(c, "type")) ?? "?",
                    // Components created after the baseline write no since-baseline figures: all of theirs count.
                    Renders: since && c.TryGetProperty("rendersSinceBaseline", out _) ? Int(c, "rendersSinceBaseline") : Int(c, "renders"),
                    Ms: since && c.TryGetProperty("renderMsSinceBaseline", out _) ? Number(c, "renderMsSinceBaseline") ?? 0 : Number(c, "totalRenderMs") ?? 0,
                    Max: Number(c, "maxRenderMs") ?? 0))
                .Where(c => c.Renders > 0)
                .GroupBy(c => c.Type)
                .Select(g => new BevProfileTypeTotals(g.Key, g.First().Display, g.Count(), g.Sum(c => c.Renders), g.Sum(c => c.Ms), g.Max(c => c.Max), null, 0))
                .ToList();
            return new BevRenderProfile
            {
                SinceBaseline = since,
                BaselineAt = baseline,
                RecordedAt = recordedAt,
                Truncated = components.TryGetProperty("truncated", out var truncated) && truncated.ValueKind == JsonValueKind.True,
                Runtime = runtime,
                RenderMode = renderMode,
                FromComponents = true,
                Types = types,
            };
        }

        throw new FormatException("The export has neither a \"profile\" nor a \"components\" section: export with one of them included.");
    }

    /// <summary>Text from a file shown in the panel and in Markdown: one line, bounded.</summary>
    private static string? Line(string? text) => text is null ? null : text.ReplaceLineEndings(" ") is var line && line.Length > 300 ? line[..300] + "…" : line;

    private static IEnumerable<JsonElement> Items(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object)
            : [];

    private static string? String(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double? Number(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    private static int Int(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;

    private static DateTimeOffset? Date(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.TryGetDateTimeOffset(out var date) ? date : null;
}
