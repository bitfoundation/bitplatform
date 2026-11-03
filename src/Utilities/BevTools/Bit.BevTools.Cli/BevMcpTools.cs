using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bit.BevTools;

/// <summary>
/// The tools of <c>bevtools mcp</c>. Every one reads the loaded exports and nothing else, so all of them are read-only,
/// idempotent and closed-world, which lets a client run them without asking a person first. Each answer is compact JSON
/// taken from an export as it is - BevTools redacted the values when it captured them, and nothing is added here that
/// the file does not hold - and bounded three ways: a limit on items, a cap on each string (named in
/// <c>truncatedFields</c>), and a size bound on the whole answer (<c>truncatedBy</c>).
/// </summary>
internal sealed class BevMcpTools(BevMcpExports exports)
{
    // About 16k tokens: room for a useful page, not for an export.
    private const int ResponseBudget = 64 * 1024;

    private const int TextLimit = 300;

    private const int LongTextLimit = 1000;

    private const int StackTraceLimit = 2000;

    private const int ArrayLimit = 20;

    private const int DepthLimit = 6;

    private const string SessionHelp = "Which export: its index from list_sessions or its file name; may be left out when one export is loaded.";

    private static readonly string[] LogLevels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    [McpServerTool(Name = "list_sessions", Title = "List the loaded session exports", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Lists the BevTools session exports this server was started with: index, file, format, when it was exported, the app's platform, render mode, runtime and value access, whether capture was on, and how many entries each section holds - so you know which tools have evidence to return. Start here.")]
    public string ListSessions() => BevCliFormats.Write(indented: false, w =>
    {
        w.WriteStartObject();
        w.WriteStartArray("sessions");
        foreach (var export in exports.All)
        {
            var root = export.Root;
            w.WriteStartObject();
            w.WriteNumber("index", export.Index);
            w.WriteString("file", export.File);
            Copy(w, root, "format");
            Copy(w, root, "exportedAt");
            if (export.Object("session") is { } session)
            {
                foreach (var name in new[] { "platform", "renderMode", "runtime", "valueAccess", "interactive", "capturing", "startedAt", "bevToolsVersion" })
                {
                    Copy(w, session, name);
                }
            }

            var other = new List<string>();
            w.WriteStartObject("sections");
            foreach (var section in root.EnumerateObject())
            {
                if (section.Name is "format" or "exportedAt" or "session")
                {
                    continue;
                }

                if (Count(section.Value) is { } count)
                {
                    w.WriteNumber(section.Name, count);
                }
                else
                {
                    other.Add(section.Name);
                }
            }

            w.WriteEndObject();
            w.WriteStartArray("otherSections");
            foreach (var name in other)
            {
                w.WriteStringValue(name);
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    });

    [McpServerTool(Name = "get_findings", Title = "Findings of BevTools' rules", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the findings BevTools' diagnostic rules recorded in an export, most severe first: rule id, severity, title, evidence (the measured facts the rule saw), suggestion, fingerprint, componentInstanceId (follow it with get_component), relatedEventIds (follow them with get_timeline), first and last seen, dismissed. Values were redacted at capture.")]
    public string GetFindings(
        [Description(SessionHelp)] string? session = null,
        [Description("Least severe finding to return: info, warning or error. Default: all.")] string? minSeverity = null,
        [Description("Only this rule (render.storm), or a rule family (render).")] string? ruleId = null,
        [Description("Also return findings someone dismissed in the session. Default false.")] bool includeDismissed = false,
        [Description("At most this many findings (default 50, at most 200).")] int limit = 50)
    {
        var export = exports.Get(session);
        if (export.Array("diagnostics") is not { } diagnostics)
        {
            return Missing(export, "diagnostics");
        }

        var least = minSeverity is null ? 0 : SeverityRank(minSeverity) ?? throw new McpException($"minSeverity takes info, warning or error, not '{Short(minSeverity)}'.");
        var matches = diagnostics
            .Where(d => includeDismissed || !Bool(d, "dismissed"))
            .Where(d => SeverityRank(String(d, "severity")) is var rank && (rank ?? 2) >= least)
            .Where(d => ruleId is null || String(d, "ruleId") is { } id && (id == ruleId || id.StartsWith(ruleId + ".", StringComparison.Ordinal)))
            // A severity this version does not know ranks as an error, as bevtools check counts it.
            .OrderByDescending(d => SeverityRank(String(d, "severity")) ?? 2)
            .ThenByDescending(d => String(d, "lastSeen"), StringComparer.Ordinal)
            .ToList();
        return Page(export, "findings", matches, Limit(limit, 200), null, (w, d) => Item(w, d));
    }

    [McpServerTool(Name = "get_errors", Title = "Recorded errors", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the errors an export recorded, most recently seen first: source, exception type, message and its template, occurrence count, the component it happened in (componentInstanceId), source location, stack trace (cut to 2000 characters), and the timeline events before it (precedingEventId, timelineEventId). Messages were redacted at capture.")]
    public string GetErrors(
        [Description(SessionHelp)] string? session = null,
        [Description("Only errors whose message, exception type, source, component or location contains this text (case-insensitive).")] string? text = null,
        [Description("At most this many errors (default 20, at most 100).")] int limit = 20)
    {
        var export = exports.Get(session);
        if (export.Array("errors") is not { } errors)
        {
            return Missing(export, "errors");
        }

        var matches = errors
            .Where(e => text is null || new[] { "message", "messageTemplate", "exceptionType", "source", "componentName", "sourceLocation", "innerError" }.Any(f => Contains(String(e, f), text)))
            .OrderByDescending(e => String(e, "lastSeen"), StringComparer.Ordinal)
            .ToList();
        return Page(export, "errors", matches, Limit(limit, 100), null, (w, e) => Item(w, e));
    }

    [McpServerTool(Name = "get_error_groups", Title = "Errors grouped into problems", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns an export's errors grouped into distinct problems (the same exception and message template with different values), most occurrences first: source, exception type, message template, occurrences, distinct messages, source location, the components it happened in, first and last seen.")]
    public string GetErrorGroups(
        [Description(SessionHelp)] string? session = null,
        [Description("At most this many groups (default 20, at most 100).")] int limit = 20)
    {
        var export = exports.Get(session);
        return export.Array("errorGroups") is { } groups
            ? Page(export, "errorGroups", groups, Limit(limit, 100), null, (w, g) => Item(w, g))
            : Missing(export, "errorGroups");
    }

    [McpServerTool(Name = "get_slowest_components", Title = "Slowest components", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the component instances that rendered, ranked by total render time (BuildRenderTree, in ms), by render count or by their slowest render: instance id, type, display name, parent, render mode, renders, total, max and last render times, renders forced by a parent with unchanged parameters, the last render's cause, errors, disposed. Times are as BevTools measured them in the recorded app.")]
    public string GetSlowestComponents(
        [Description(SessionHelp)] string? session = null,
        [Description("Rank by total (total render time, the default), renders (render count) or max (slowest single render).")] string by = "total",
        [Description("At most this many components (default 20, at most 100).")] int limit = 20)
    {
        var export = exports.Get(session);
        if (export.Object("components") is not { } components || !components.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return Missing(export, "components");
        }

        var field = by.Trim().ToLowerInvariant() switch
        {
            "total" or "time" or "totalrenderms" => "totalRenderMs",
            "renders" or "count" => "renders",
            "max" or "maxrenderms" => "maxRenderMs",
            _ => throw new McpException($"by takes total, renders or max, not '{Short(by)}'."),
        };
        var matches = items.EnumerateArray()
            .Where(c => c.ValueKind == JsonValueKind.Object && Number(c, "renders") > 0)
            .OrderByDescending(c => Number(c, field))
            .ThenBy(c => Number(c, "instanceId"))
            .ToList();
        return Page(export, "components", matches, Limit(limit, 100), w =>
        {
            w.WriteString("rankedBy", field);
            Copy(w, components, "truncated", "registryTruncated");
            Copy(w, components, "baselineAt");
        }, (w, c) => Item(w, c));
    }

    [McpServerTool(Name = "get_component", Title = "One component instance", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one component instance's entry in an export by its instance id (from get_findings, get_errors, get_timeline or get_slowest_components): type, parent, render mode, lifecycle, render counts and times, the last render's cause; with its children and the findings and errors that name it.")]
    public string GetComponent(
        [Description("The component's instance id (componentInstanceId or instanceId).")] long instanceId,
        [Description(SessionHelp)] string? session = null)
    {
        var export = exports.Get(session);
        if (export.Object("components") is not { } components || !components.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return Missing(export, "components");
        }

        var all = items.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.Object).ToList();
        if (all.FirstOrDefault(c => Long(c, "instanceId") == instanceId) is not { ValueKind: JsonValueKind.Object } component)
        {
            throw new McpException($"No component #{instanceId} in this export's {all.Count} components: it may have been let go before the export, or created after MaxTrackedComponents was reached.");
        }

        return BevCliFormats.Write(indented: false, w =>
        {
            w.WriteStartObject();
            Source(w, export);
            w.WritePropertyName("component");
            Item(w, component);
            var children = all.Where(c => Long(c, "parentInstanceId") == instanceId).ToList();
            w.WriteNumber("childrenTotal", children.Count);
            w.WriteStartArray("children");
            foreach (var child in children.Take(50))
            {
                w.WriteStartObject();
                Copy(w, child, "instanceId");
                Copy(w, child, "displayName");
                Copy(w, child, "renders");
                Copy(w, child, "disposed");
                w.WriteEndObject();
            }

            w.WriteEndArray();
            var findings = (export.Array("diagnostics") ?? []).Where(d => Long(d, "componentInstanceId") == instanceId).ToList();
            w.WriteNumber("findingsTotal", findings.Count);
            w.WriteStartArray("findings");
            foreach (var finding in findings.Take(10))
            {
                Item(w, finding);
            }

            w.WriteEndArray();
            var errors = (export.Array("errors") ?? []).Where(e => Long(e, "componentInstanceId") == instanceId).ToList();
            w.WriteNumber("errorsTotal", errors.Count);
            w.WriteStartArray("errors");
            foreach (var error in errors.Take(10))
            {
                w.WriteStartObject();
                var truncated = new List<string>();
                foreach (var name in new[] { "id", "exceptionType", "message", "count", "lastSeen", "sourceLocation" })
                {
                    if (error.TryGetProperty(name, out var value))
                    {
                        w.WritePropertyName(name);
                        Value(w, value, name, 1, truncated);
                    }
                }

                Truncated(w, truncated);
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        });
    }

    [McpServerTool(Name = "get_http", Title = "HTTP requests", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the HTTP requests an export recorded, newest first: method, URL, status, duration, sizes, error, failed/cancelled/pending, the UI event that triggered it and the component that sent it (caller), and headers when the app captured them. URLs, headers and bodies (captured only where the app turned CaptureHttpBodies on) were redacted at capture.")]
    public string GetHttp(
        [Description(SessionHelp)] string? session = null,
        [Description("Only failed requests (an error status or an exception; cancelled ones are not failures).")] bool failed = false,
        [Description("Only slow requests, by the slow-request threshold the recorded session used (its options.diagnostics.slowHttpMs).")] bool slow = false,
        [Description("Only requests that took at least this many ms (overrides slow's threshold).")] double? minDurationMs = null,
        [Description("Only requests whose URL contains this text (case-insensitive), such as /api/orders.")] string? endpoint = null,
        [Description("At most this many requests (default 25, at most 200).")] int limit = 25)
    {
        var export = exports.Get(session);
        if (export.Array("http") is not { } http)
        {
            return Missing(export, "http");
        }

        var threshold = minDurationMs;
        if (threshold is null && slow)
        {
            threshold = export.Object("options") is { } options && options.TryGetProperty("diagnostics", out var diagnostics) && diagnostics.ValueKind == JsonValueKind.Object
                && diagnostics.TryGetProperty("slowHttpMs", out var slowMs) && slowMs.TryGetDouble(out var ms)
                    ? ms
                    : throw new McpException("This export has no options section to take the slow-request threshold from: pass minDurationMs.");
        }

        if (threshold is { } t && (!double.IsFinite(t) || t < 0))
        {
            throw new McpException("minDurationMs takes a non-negative number.");
        }

        var matches = Enumerable.Reverse(http)
            .Where(r => !failed || Bool(r, "failed"))
            .Where(r => threshold is not { } min || (r.TryGetProperty("durationMs", out var d) && d.TryGetDouble(out var duration) && duration >= min))
            .Where(r => endpoint is null || Contains(String(r, "url"), endpoint))
            .ToList();
        return Page(export, "requests", matches, Limit(limit, 200), w =>
        {
            if (threshold is { } min)
            {
                w.WriteNumber("minDurationMs", min);
            }
        }, (w, r) => Item(w, r));
    }

    [McpServerTool(Name = "get_http_endpoints", Title = "HTTP requests per endpoint", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns an export's HTTP requests summarised per endpoint template (ids in the path as {id}), busiest first: requests, distinct URLs, failed, cancelled, pending, p50/p95/max duration, response bytes and the components that sent them. Useful to spot an endpoint called once per item.")]
    public string GetHttpEndpoints(
        [Description(SessionHelp)] string? session = null,
        [Description("At most this many endpoints (default 25, at most 200).")] int limit = 25)
    {
        var export = exports.Get(session);
        return export.Array("httpEndpoints") is { } endpoints
            ? Page(export, "endpoints", endpoints, Limit(limit, 200), null, (w, e) => Item(w, e))
            : Missing(export, "httpEndpoints");
    }

    [McpServerTool(Name = "get_timeline", Title = "Timeline events", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns an export's timeline events in order: id, time, kind (Render, UiEvent, Http, Navigation, Error, ...), category, title, detail, duration, severity, parent event, component, and the event's data (redacted at capture). Page with afterEventId: when truncated, nextAfterEventId continues where this answer stopped; to read event N alone (a finding's relatedEventIds), pass afterEventId N-1 and limit 1.")]
    public string GetTimeline(
        [Description(SessionHelp)] string? session = null,
        [Description("Only events of this kind (case-insensitive), such as Render, UiEvent, Http, Navigation or Error.")] string? kind = null,
        [Description("Only events after this event id.")] long? afterEventId = null,
        [Description("At most this many events (default 100, at most 500).")] int limit = 100)
    {
        var export = exports.Get(session);
        if (export.Array("timeline") is not { } timeline)
        {
            return Missing(export, "timeline");
        }

        var matches = timeline
            .Where(e => kind is null || string.Equals(String(e, "kind"), kind.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(e => afterEventId is not { } after || Long(e, "id") > after)
            .OrderBy(e => Long(e, "id"))
            .ToList();
        return Page(export, "events", matches, Limit(limit, 500), null, (w, e) => Item(w, e), (w, last, more) =>
        {
            if (more && last is { } l && Long(l, "id") is { } id)
            {
                w.WriteNumber("nextAfterEventId", id);
            }
        });
    }

    [McpServerTool(Name = "get_logs", Title = "Captured log entries", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the log entries an export captured (its logs section), most recent first: level, category, message and template, the template's values, scopes, exception, trace id, the component that logged and whatever else the entry holds; everything was redacted at capture. Says so when the export has no logs section.")]
    public string GetLogs(
        [Description(SessionHelp)] string? session = null,
        [Description("Least severe level to return: Trace, Debug, Information, Warning, Error or Critical.")] string? minLevel = null,
        [Description("Only categories that start with this (Microsoft.AspNetCore.Components).")] string? categoryPrefix = null,
        [Description("Only entries whose message contains this text (case-insensitive).")] string? text = null,
        [Description("At most this many entries (default 50, at most 500).")] int limit = 50)
    {
        var export = exports.Get(session);
        if (export.Array("logs") is not { } logs)
        {
            return Missing(export, "logs");
        }

        var least = minLevel is null ? 0 : LevelRank(minLevel) ?? throw new McpException($"minLevel takes {string.Join(", ", LogLevels)}, not '{Short(minLevel)}'.");
        // A level this version does not know is returned whatever the minimum, as an unknown severity is looked at.
        var matches = Enumerable.Reverse(logs)
            .Where(l => (LevelRank(String(l, "level") ?? String(l, "logLevel")) ?? LogLevels.Length) >= least)
            .Where(l => categoryPrefix is null || String(l, "category")?.StartsWith(categoryPrefix, StringComparison.Ordinal) == true)
            // Under value access None an entry has its template and no message.
            .Where(l => text is null || Contains(String(l, "message") ?? String(l, "messageTemplate"), text))
            .ToList();
        return Page(export, "logs", matches, Limit(limit, 500), null, (w, l) => Item(w, l));
    }

    [McpServerTool(Name = "check", Title = "Run bevtools check", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Runs the same check as `bevtools check` on an export and returns its JSON form: passed, the threshold, each finding with ruleId, severity, title, evidence, suggestion, fingerprint, componentInstanceId, whether it fails the check and whether it was dismissed, recorded errors and failed requests, and caveats (what limits a pass, such as rules that ran only while BevTools was shown).")]
    public string Check(
        [Description(SessionHelp)] string? session = null,
        [Description("The least severe finding that fails: info, warning (default), error or none.")] string failOn = "warning",
        [Description("Rule ids whose findings are listed but do not fail the check.")] string[]? ignore = null,
        [Description("Whether any recorded error fails the check. Default false.")] bool failOnErrors = false)
    {
        var export = exports.Get(session);
        var options = new BevSessionCheckOptions
        {
            FailOnErrors = failOnErrors,
            FailOn = failOn.Trim().ToLowerInvariant() switch
            {
                "none" => null,
                "info" => BevDiagnosticSeverity.Info,
                "warning" => BevDiagnosticSeverity.Warning,
                "error" => BevDiagnosticSeverity.Error,
                _ => throw new McpException($"failOn takes info, warning, error or none, not '{Short(failOn)}'."),
            },
        };
        foreach (var rule in ignore ?? [])
        {
            options.IgnoredRules.Add(rule);
        }

        try
        {
            return BevCliFormats.CheckJson(BevSessionCheck.Check(export.Json, options), options, export.File, indented: false, maxFindings: 100, maxText: LongTextLimit);
        }
        catch (FormatException ex)
        {
            throw new McpException(ex.Message, ex);
        }
    }

    [McpServerTool(Name = "compare", Title = "Run bevtools compare", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Compares the render profiles of two loaded exports of the same interaction, as `bevtools compare` does, and returns its JSON form: passed, regressions, total renders and render time in each, and per component type the change kind (Regressed, Improved, New, Gone, Unchanged) with the reason, instances, renders, total, max and p95 render times; plus warnings when the two are not comparable.")]
    public string Compare(
        [Description("The baseline export: its index from list_sessions or its file name.")] string baseline,
        [Description("The current export: its index or file name.")] string current,
        [Description("Renders a type must gain to count (default 1).")] int? minRenderChange = null,
        [Description("...and by more than this share of its baseline renders (default 0).")] double? renderChangeRatio = null,
        [Description("Render time in ms a type must gain to count (default 5).")] double? minTimeChangeMs = null,
        [Description("...and by more than this share of its baseline time (default 0.5).")] double? timeChangeRatio = null)
    {
        var before = exports.Get(baseline, "baseline");
        var after = exports.Get(current, "current");
        var options = new BevProfileComparisonOptions();
        if (minRenderChange is { } renders)
        {
            options.MinRenderChange = renders >= 0 ? renders : throw new McpException("minRenderChange takes a non-negative whole number.");
        }

        options.RenderChangeRatio = NonNegative(renderChangeRatio, nameof(renderChangeRatio)) ?? options.RenderChangeRatio;
        options.MinTimeChangeMs = NonNegative(minTimeChangeMs, nameof(minTimeChangeMs)) ?? options.MinTimeChangeMs;
        options.TimeChangeRatio = NonNegative(timeChangeRatio, nameof(timeChangeRatio)) ?? options.TimeChangeRatio;
        try
        {
            return BevCliFormats.CompareJson(BevProfileComparison.Compare(before.Json, after.Json, options), options, before.File, after.File, indented: false, maxChanges: 100);
        }
        catch (FormatException ex)
        {
            throw new McpException(ex.Message, ex);
        }
    }

    /// <summary>A list answer: where it came from, how many matched, a page of them within the size bound, and whether that was all.</summary>
    private static string Page(BevMcpExport export, string name, IReadOnlyList<JsonElement> matches, int limit, Action<Utf8JsonWriter>? header, Action<Utf8JsonWriter, JsonElement> item, Action<Utf8JsonWriter, JsonElement?, bool>? footer = null) =>
        BevCliFormats.Write(indented: false, w =>
        {
            w.WriteStartObject();
            Source(w, export);
            header?.Invoke(w);
            w.WriteNumber("total", matches.Count);
            w.WriteNumber("limit", limit);
            w.WriteStartArray(name);
            var returned = 0;
            var full = false;
            JsonElement? last = null;
            foreach (var match in matches)
            {
                if (returned == limit)
                {
                    break;
                }

                if (w.BytesCommitted + w.BytesPending > ResponseBudget)
                {
                    full = true;
                    break;
                }

                item(w, match);
                last = match;
                returned++;
            }

            w.WriteEndArray();
            w.WriteNumber("returned", returned);
            w.WriteBoolean("truncated", returned < matches.Count);
            if (full)
            {
                w.WriteString("truncatedBy", "size: this answer reached its size bound; ask for fewer items or narrow the filter");
            }

            footer?.Invoke(w, last, returned < matches.Count);
            w.WriteEndObject();
        });

    private static string Missing(BevMcpExport export, string section) => BevCliFormats.Write(indented: false, w =>
    {
        w.WriteStartObject();
        Source(w, export);
        w.WriteBoolean("present", false);
        w.WriteString("message", $"no {section} section in this export");
        w.WriteEndObject();
    });

    private static void Source(Utf8JsonWriter w, BevMcpExport export)
    {
        w.WriteNumber("session", export.Index);
        w.WriteString("file", export.File);
    }

    /// <summary>An entry of the export as it is, with each string capped and each array cut, both named in <c>truncatedFields</c>.</summary>
    private static void Item(Utf8JsonWriter w, JsonElement item)
    {
        var truncated = new List<string>();
        if (item.ValueKind != JsonValueKind.Object)
        {
            Value(w, item, "", 0, truncated);
            return;
        }

        w.WriteStartObject();
        foreach (var property in item.EnumerateObject())
        {
            w.WritePropertyName(BevCliFormats.Clip(property.Name, TextLimit) ?? property.Name);
            Value(w, property.Value, property.Name, 1, truncated);
        }

        Truncated(w, truncated);
        w.WriteEndObject();
    }

    private static void Value(Utf8JsonWriter w, JsonElement value, string path, int depth, List<string> truncated)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var text = value.GetString()!;
                if (BevCliFormats.Clip(text, TextLimitOf(path)) is { } clipped)
                {
                    Mark(truncated, path);
                    text = clipped;
                }

                w.WriteStringValue(text);
                break;
            case JsonValueKind.Object when depth < DepthLimit:
                w.WriteStartObject();
                foreach (var property in value.EnumerateObject())
                {
                    w.WritePropertyName(BevCliFormats.Clip(property.Name, TextLimit) ?? property.Name);
                    Value(w, property.Value, path + "." + property.Name, depth + 1, truncated);
                }

                w.WriteEndObject();
                break;
            case JsonValueKind.Array when depth < DepthLimit:
                w.WriteStartArray();
                var count = 0;
                foreach (var element in value.EnumerateArray())
                {
                    if (count++ == ArrayLimit)
                    {
                        Mark(truncated, path);
                        break;
                    }

                    Value(w, element, path + "[]", depth + 1, truncated);
                }

                w.WriteEndArray();
                break;
            case JsonValueKind.Object or JsonValueKind.Array:
                Mark(truncated, path);
                w.WriteStringValue("…");
                break;
            default:
                value.WriteTo(w);
                break;
        }
    }

    private static int TextLimitOf(string path) => path[(path.LastIndexOf('.') + 1)..].TrimEnd('[', ']') switch
    {
        "stackTrace" => StackTraceLimit,
        "message" or "messageTemplate" or "evidence" or "suggestion" or "innerError" or "detail" or "error" or "title" => LongTextLimit,
        _ => TextLimit,
    };

    private static void Mark(List<string> truncated, string path)
    {
        if (!truncated.Contains(path))
        {
            truncated.Add(path);
        }
    }

    private static void Truncated(Utf8JsonWriter w, List<string> truncated)
    {
        if (truncated.Count == 0)
        {
            return;
        }

        w.WriteStartArray("truncatedFields");
        foreach (var path in truncated)
        {
            w.WriteStringValue(path);
        }

        w.WriteEndArray();
    }

    /// <summary>Copies one small value of <paramref name="from"/> as it is, when it is there.</summary>
    private static void Copy(Utf8JsonWriter w, JsonElement from, string name, string? asName = null)
    {
        if (from.ValueKind == JsonValueKind.Object && from.TryGetProperty(name, out var value) && value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
        {
            w.WritePropertyName(asName ?? name);
            Value(w, value, name, 1, []);
        }
    }

    // How many entries a section holds: an array's length, or the list inside an object section (components' items,
    // the profile's types, the interop calls, the route table); null for a section that is not a list.
    private static int? Count(JsonElement section) => section.ValueKind switch
    {
        JsonValueKind.Array => section.GetArrayLength(),
        JsonValueKind.Object => new[] { "items", "types", "calls", "routes" }
            .Select(name => section.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array ? list.GetArrayLength() : (int?)null)
            .FirstOrDefault(count => count is not null),
        _ => null,
    };

    private static int Limit(int requested, int max) => Math.Clamp(requested, 1, max);

    private static int? SeverityRank(string? severity) => severity?.Trim().ToLowerInvariant() switch
    {
        "info" => 0,
        "warning" => 1,
        "error" => 2,
        _ => null,
    };

    private static int? LevelRank(string? level) =>
        level is null ? null : System.Array.FindIndex(LogLevels, l => string.Equals(l, level.Trim(), StringComparison.OrdinalIgnoreCase)) is var i and >= 0 ? i : null;

    private static double? NonNegative(double? value, string name) =>
        value is not { } number || (double.IsFinite(number) && number >= 0) ? value : throw new McpException(name + " takes a non-negative number.");

    private static string Short(string text) => BevCliFormats.Clip(text, 100) ?? text;

    private static bool Contains(string? value, string text) => value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;

    private static string? String(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Bool(JsonElement e, string name) => e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static long? Long(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static double Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : 0;
}
