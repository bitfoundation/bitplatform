using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>What <see cref="BevTraceExporter.ToChromeTrace"/> includes.</summary>
public sealed class BevTraceExportOptions
{
    /// <summary>Only activity that started at or after this moment, such as the profiler baseline. Null: everything buffered.</summary>
    public DateTimeOffset? Since { get; set; }

    /// <summary>Arrows from each UI event or navigation to the render batches and requests it caused. Default true.</summary>
    public bool IncludeFlows { get; set; } = true;
}

/// <summary>
/// Writes a session's timeline as a performance trace in the Chrome Trace Event format, which ui.perfetto.dev and the
/// Chrome DevTools Performance panel open as they are: render batches with every component render nested inside them
/// on a renderer track, UI events and navigations, HTTP requests and JS interop calls on tracks of their own, state
/// changes, errors and findings as markers, and arrows from each event to what it caused. It reads the same buffers
/// the panels do, already redacted at capture time, so a trace never shows more than the timeline.
/// </summary>
public static class BevTraceExporter
{
    /// <summary>Written into the trace's metadata so readers can tell versions apart.</summary>
    public const string Format = "bit-bevtools-trace/1";

    private const int RendererTrack = 1;
    private const int EventLanes = 10;
    private const int NetworkLanes = 30;
    private const int InteropLanes = 50;
    private const int StateTrack = 70;
    private const int ErrorTrack = 71;
    private const int ConnectionTrack = 72;
    private const int DiagnosticsTrack = 73;
    private const int BevToolsTrack = 74;
    private const int MaxLanes = 16;

    /// <summary>
    /// An event whose timestamp and high-resolution start disagree by more than this was recorded after it happened (a
    /// UI event adopted when its activity stopped, a JS → .NET call reported in a batch), so its wall-clock time is used.
    /// </summary>
    private static readonly TimeSpan RecordedLateThreshold = TimeSpan.FromMilliseconds(5);

    public static string ToChromeTrace(BevToolsSession session, BevTraceExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        options ??= new BevTraceExportOptions();
        var pid = session.Id;
        var events = session.Timeline.Snapshot();
        if (options.Since is { } since)
        {
            events = events.Where(e => e.Timestamp >= since).ToArray();
        }

        var clock = new TraceClock(session);
        var lanes = new Dictionary<int, List<double>>();
        var slices = new Dictionary<long, (int Tid, double Ts)>();
        var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            w.WriteStartObject();
            w.WriteStartArray("traceEvents");
            WriteProcess(w, pid, session);

            // Renders nest inside their batch: the batch's span is the span of its renders, which is what one batch
            // of renderer work occupied; its own event only carries the summed build time.
            var renders = events.Where(e => e.Kind == BevToolsEventKind.Render).ToArray();
            var spans = renders.Where(e => e.TryGetData("batch", out var b) && b is long)
                .GroupBy(e => (long)e.Data!["batch"]!)
                .ToDictionary(g => g.Key, g => (Start: g.Min(clock.Start), End: g.Max(e => clock.Start(e) + Micro(e.DurationMs ?? 0))));

            foreach (var e in events)
            {
                switch (e.Kind)
                {
                    case BevToolsEventKind.RenderBatch:
                        var batch = e.TryGetData("batch", out var id) && id is long batchId ? batchId : 0;
                        if (spans.TryGetValue(batch, out var span))
                        {
                            Complete(w, e, pid, RendererTrack, span.Start, Math.Max(span.End - span.Start, 1), session);
                            slices[e.Id] = (RendererTrack, span.Start);
                        }
                        else
                        {
                            // Its renders were evicted from the buffer: where they ran is no longer known, so no span is drawn.
                            Instant(w, e, pid, RendererTrack, clock.Start(e), session);
                        }

                        break;
                    case BevToolsEventKind.Render:
                        Complete(w, e, pid, RendererTrack, clock.Start(e), Math.Max(Micro(e.DurationMs ?? 0), 1), session);
                        break;
                    case BevToolsEventKind.UiEvent or BevToolsEventKind.Navigation:
                        Laned(w, e, pid, EventLanes, lanes, clock, slices, session);
                        break;
                    case BevToolsEventKind.Http:
                        Laned(w, e, pid, NetworkLanes, lanes, clock, slices, session);
                        break;
                    case BevToolsEventKind.JsInterop:
                        Laned(w, e, pid, InteropLanes, lanes, clock, slices, session);
                        break;
                    case BevToolsEventKind.StateChange or BevToolsEventKind.Lifecycle:
                        Instant(w, e, pid, StateTrack, clock.Start(e), session);
                        break;
                    case BevToolsEventKind.Error:
                        Instant(w, e, pid, ErrorTrack, clock.Start(e), session);
                        break;
                    case BevToolsEventKind.Circuit or BevToolsEventKind.Browser:
                        Instant(w, e, pid, ConnectionTrack, clock.Start(e), session);
                        break;
                    case BevToolsEventKind.Diagnostic:
                        Instant(w, e, pid, DiagnosticsTrack, clock.Start(e), session);
                        break;
                    default:
                        if (e.DurationMs is > 0)
                        {
                            Laned(w, e, pid, BevToolsTrack, lanes, clock, slices, session, maxLanes: 1);
                        }
                        else
                        {
                            Instant(w, e, pid, BevToolsTrack, clock.Start(e), session);
                        }

                        break;
                }
            }

            if (options.IncludeFlows)
            {
                WriteFlows(w, pid, events, slices, clock);
            }

            WriteTrackNames(w, pid, lanes);
            w.WriteEndArray();
            w.WriteString("displayTimeUnit", "ms");
            w.WriteStartObject("otherData");
            w.WriteString("format", Format);
            w.WriteNumber("sessionId", session.Id);
            w.WriteString("platform", session.Platform);
            w.WriteString("renderMode", session.RenderModeName);
            w.WriteString("sessionStartedAt", session.StartedAt);
            w.WriteString("exportedAt", DateTimeOffset.UtcNow);
            w.WriteString("bevToolsVersion", typeof(BevTraceExporter).Assembly.GetName().Version?.ToString());
            w.WriteString("runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            w.WriteNumber("events", events.Length);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>Offers <see cref="ToChromeTrace"/> as a file download (About panel and the <c>trace.export</c> command).</summary>
    internal static async Task DownloadAsync(BevToolsSession session, IBevToolsCommandContext context, DateTimeOffset? since = null)
    {
        var name = $"bit-bevtools-trace-{session.Id}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
        var downloaded = await context.DownloadTextAsync(name, ToChromeTrace(session, new BevTraceExportOptions { Since = since }), "application/json");
        context.Notify(downloaded ? "Trace downloaded: " + name + " (open it in ui.perfetto.dev or the Chrome DevTools Performance panel)" : "Export unavailable: the browser bridge is not attached");
    }

    /// <summary>Puts an event with a duration on the first lane of its group that is free at its start, so slices on one track never overlap.</summary>
    private static void Laned(Utf8JsonWriter w, BevToolsEvent e, long pid, int firstLane, Dictionary<int, List<double>> lanes, TraceClock clock, Dictionary<long, (int Tid, double Ts)> slices, BevToolsSession session, int maxLanes = MaxLanes)
    {
        var ts = clock.Start(e);
        if (e.DurationMs is not { } ms)
        {
            // Still in progress at export: a marker, since its end is unknown.
            Instant(w, e, pid, firstLane, ts, session);
            return;
        }

        var dur = Math.Max(Micro(ms), 1);
        if (!lanes.TryGetValue(firstLane, out var ends))
        {
            lanes[firstLane] = ends = [];
        }

        var lane = ends.FindIndex(end => end <= ts);
        if (lane < 0 && ends.Count < maxLanes)
        {
            ends.Add(0);
            lane = ends.Count - 1;
        }
        else if (lane < 0)
        {
            // More overlapping activity than lanes: the earliest-ending lane takes it, overlapping its last slice.
            lane = ends.IndexOf(ends.Min());
        }

        ends[lane] = ts + dur;
        var tid = firstLane + lane;
        Complete(w, e, pid, tid, ts, dur, session);
        slices[e.Id] = (tid, ts);
    }

    private static void Complete(Utf8JsonWriter w, BevToolsEvent e, long pid, int tid, double ts, double dur, BevToolsSession session)
    {
        w.WriteStartObject();
        w.WriteString("name", e.Title);
        w.WriteString("cat", e.Category);
        w.WriteString("ph", "X");
        w.WriteNumber("ts", Round(ts));
        w.WriteNumber("dur", Round(dur));
        w.WriteNumber("pid", pid);
        w.WriteNumber("tid", tid);
        WriteArgs(w, e, session);
        w.WriteEndObject();
    }

    private static void Instant(Utf8JsonWriter w, BevToolsEvent e, long pid, int tid, double ts, BevToolsSession session)
    {
        w.WriteStartObject();
        w.WriteString("name", e.Title);
        w.WriteString("cat", e.Category);
        w.WriteString("ph", "i");
        w.WriteString("s", "t");
        w.WriteNumber("ts", Round(ts));
        w.WriteNumber("pid", pid);
        w.WriteNumber("tid", tid);
        WriteArgs(w, e, session);
        w.WriteEndObject();
    }

    private static void WriteArgs(Utf8JsonWriter w, BevToolsEvent e, BevToolsSession session)
    {
        w.WriteStartObject("args");
        w.WriteNumber("eventId", e.Id);
        w.WriteString("kind", e.Kind.ToString());
        w.WriteString("severity", e.Severity.ToString());
        if (e.Detail is not null)
        {
            w.WriteString("detail", e.Detail);
        }

        if (e.ComponentName is not null)
        {
            w.WriteString("component", e.ComponentName);
        }

        if (e.ComponentInstanceId is { } component)
        {
            w.WriteNumber("componentInstanceId", component);
        }

        if (e.ParentEventId is { } parent)
        {
            w.WriteNumber("causedBy", parent);
        }

        if (e.DurationMs is { } ms)
        {
            w.WriteNumber("durationMs", Math.Round(ms, 3));
        }

        if (e.Data is { Count: > 0 } data)
        {
            foreach (var (key, value) in data)
            {
                if (value is null or string or bool or int or long or double)
                {
                    // Through the inspector like every exported value, so a sensitive key or a credential-shaped value
                    // reads here as it does in the panel.
                    var node = session.Inspector.DescribeMetadata(key, value);
                    if (node.Kind == BevNodeKind.Redacted)
                    {
                        w.WriteString(key, node.Display);
                        continue;
                    }

                    switch (value)
                    {
                        case null: w.WriteNull(key); break;
                        case string s: w.WriteString(key, s.Length > 300 ? s[..300] + "…" : s); break;
                        case bool b: w.WriteBoolean(key, b); break;
                        case int i: w.WriteNumber(key, i); break;
                        case long l: w.WriteNumber(key, l); break;
                        case double d when double.IsFinite(d): w.WriteNumber(key, Math.Round(d, 3)); break;
                    }
                }
                else if (value is IReadOnlyList<string> { Count: > 0 } list)
                {
                    // Changed parameters and fields: already displays, and already redacted when they were captured.
                    w.WriteString(key, string.Join("; ", list.Take(10)) + (list.Count > 10 ? " …" : ""));
                }
            }
        }

        w.WriteEndObject();
    }

    /// <summary>Arrows from a UI event or navigation to the batches and requests it caused, so the trace answers "what did this click cost?".</summary>
    private static void WriteFlows(Utf8JsonWriter w, long pid, BevToolsEvent[] events, Dictionary<long, (int Tid, double Ts)> slices, TraceClock clock)
    {
        var flow = 0;
        foreach (var e in events)
        {
            if (e.Kind is not (BevToolsEventKind.RenderBatch or BevToolsEventKind.Http) || e.ParentEventId is not { } parentId
                || !slices.TryGetValue(parentId, out var source) || !slices.TryGetValue(e.Id, out var target))
            {
                continue;
            }

            flow++;
            w.WriteStartObject();
            w.WriteString("name", "caused");
            w.WriteString("cat", "cause");
            w.WriteString("ph", "s");
            w.WriteNumber("id", flow);
            w.WriteNumber("ts", Round(source.Ts));
            w.WriteNumber("pid", pid);
            w.WriteNumber("tid", source.Tid);
            w.WriteEndObject();

            w.WriteStartObject();
            w.WriteString("name", "caused");
            w.WriteString("cat", "cause");
            w.WriteString("ph", "f");
            w.WriteString("bp", "e");
            w.WriteNumber("id", flow);
            w.WriteNumber("ts", Round(Math.Max(target.Ts, source.Ts)));
            w.WriteNumber("pid", pid);
            w.WriteNumber("tid", target.Tid);
            w.WriteEndObject();
        }
    }

    private static void WriteProcess(Utf8JsonWriter w, long pid, BevToolsSession session)
    {
        Metadata(w, pid, 0, "process_name", "BevTools session #" + session.Id + " (" + (session.Platform ?? "unknown") + (session.RenderModeName is null ? "" : ", " + session.RenderModeName) + ")");
        Metadata(w, pid, RendererTrack, "thread_name", "Renderer: batches and component renders");
        Metadata(w, pid, StateTrack, "thread_name", "State changes");
        Metadata(w, pid, ErrorTrack, "thread_name", "Errors");
        Metadata(w, pid, ConnectionTrack, "thread_name", "Circuit and browser");
        Metadata(w, pid, DiagnosticsTrack, "thread_name", "Diagnostics findings");
        Metadata(w, pid, BevToolsTrack, "thread_name", "BevTools");
    }

    private static void WriteTrackNames(Utf8JsonWriter w, long pid, Dictionary<int, List<double>> lanes)
    {
        foreach (var (first, used) in lanes)
        {
            var label = first switch
            {
                EventLanes => "UI events and navigation",
                NetworkLanes => "HTTP",
                InteropLanes => "JS interop",
                _ => null,
            };
            if (label is null)
            {
                continue;
            }

            for (var i = 0; i < used.Count; i++)
            {
                Metadata(w, pid, first + i, "thread_name", used.Count == 1 ? label : label + " " + (i + 1));
            }
        }
    }

    private static void Metadata(Utf8JsonWriter w, long pid, int tid, string name, string value)
    {
        w.WriteStartObject();
        w.WriteString("name", name);
        w.WriteString("ph", "M");
        w.WriteNumber("pid", pid);
        w.WriteNumber("tid", tid);
        w.WriteStartObject("args");
        w.WriteString("name", value);
        w.WriteEndObject();
        w.WriteEndObject();

        // Keeps the tracks in the order they are declared in rather than alphabetical.
        w.WriteStartObject();
        w.WriteString("name", name == "process_name" ? "process_sort_index" : "thread_sort_index");
        w.WriteString("ph", "M");
        w.WriteNumber("pid", pid);
        w.WriteNumber("tid", tid);
        w.WriteStartObject("args");
        w.WriteNumber("sort_index", tid);
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static double Micro(double ms) => ms * 1000;

    private static double Round(double value) => Math.Round(value, 3);

    /// <summary>Microseconds since the session started, from each event's high-resolution start unless it was recorded late.</summary>
    private sealed class TraceClock(BevToolsSession session)
    {
        public double Start(BevToolsEvent e)
        {
            var fromTicks = (e.StartTicks - session.StartedTicks) * 1_000_000.0 / Stopwatch.Frequency;
            var fromWall = (e.Timestamp - session.StartedAt).TotalMicroseconds;
            return Math.Abs(fromTicks - fromWall) > RecordedLateThreshold.TotalMicroseconds ? fromWall : fromTicks;
        }
    }
}
