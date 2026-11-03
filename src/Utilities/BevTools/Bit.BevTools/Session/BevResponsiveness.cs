using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>A long animation frame (or long task) as the page hub reports it.</summary>
public sealed class BevLongFramePayload
{
    public double AgeMs { get; set; }

    public double DurationMs { get; set; }

    public double BlockingMs { get; set; }

    public BevFrameScript[]? Scripts { get; set; }
}

/// <summary>One script a long animation frame ran: what invoked it, where it is written, how long it took.</summary>
public sealed class BevFrameScript
{
    public string? Invoker { get; set; }

    public string? Type { get; set; }

    public string? Source { get; set; }

    public string? Fn { get; set; }

    public double DurationMs { get; set; }
}

/// <summary>One user interaction (Event Timing) as the page hub reports it.</summary>
public sealed class BevInteractionPayload
{
    public double AgeMs { get; set; }

    public string? Name { get; set; }

    public double DurationMs { get; set; }

    public double InputDelayMs { get; set; }

    public double ProcessingMs { get; set; }

    public double PresentationMs { get; set; }

    public double ProcessingStartAgeMs { get; set; }

    public double ProcessingEndAgeMs { get; set; }

    public string? Target { get; set; }
}

/// <summary>Which of the browser's timing entries the page's browser has.</summary>
public sealed class BevResponsivenessSupport
{
    public bool LongAnimationFrame { get; set; }

    public bool LongTask { get; set; }

    public bool EventTiming { get; set; }
}

/// <summary>A frame the browser's main thread could not paint on time, and the application work that ran in it.</summary>
public sealed record BevLongFrame(DateTimeOffset At, double DurationMs, double BlockingMs, IReadOnlyList<BevFrameScript> Scripts)
{
    /// <summary>The UI event, navigation or render batch that started inside the frame, on WebAssembly, where .NET runs on that thread.</summary>
    public IReadOnlyList<string> During { get; init; } = [];

    public long? TriggerEventId { get; init; }

    /// <summary>
    /// How much of the frame BevTools' own panel spent rendering, on WebAssembly, where it renders on the same thread;
    /// zero elsewhere. A frame mostly the panel's is not recorded at all: it is not the application's.
    /// </summary>
    public double OwnMs { get; init; }

    /// <summary>At least half the frame was BevTools' own rendering.</summary>
    public bool IsMostlyBevTools => OwnMs > 0 && OwnMs >= DurationMs / 2;

    public long TimelineEventId { get; internal set; }
}

/// <summary>
/// One user interaction, from input to the next paint: the input delay (the main thread busy with something else), the
/// processing (its event handlers), and the presentation delay (rendering the frame after them).
/// </summary>
public sealed record BevInteraction(DateTimeOffset At, string Name, double DurationMs, double InputDelayMs, double ProcessingMs, double PresentationMs, string? Target)
{
    /// <summary>The UI event whose handler ran inside the interaction's processing, on WebAssembly; null elsewhere or when none or several did.</summary>
    public long? HandlerEventId { get; init; }

    public string? HandlerTitle { get; init; }

    public long TimelineEventId { get; internal set; }
}

/// <summary>The page's responsiveness as the hub reported it: recent long frames and interactions, and session-long totals.</summary>
public sealed class BevResponsiveness
{
    private const int Capacity = 200;
    private readonly RingBuffer<BevLongFrame> _frames = new(Capacity);
    private readonly RingBuffer<BevInteraction> _interactions = new(Capacity);
    private readonly object _lock = new();
    // The percentile needs every duration; a session's interactions are few, and past this many the slowest of the
    // last ones says as much. Made with the first one: a session that reports none (a server's request scope) pays nothing.
    private RingBuffer<double>? _durations;

    /// <summary>What the browser supports; null until the hub attached.</summary>
    public BevResponsivenessSupport? Support { get; internal set; }

    public int LongFrameCount { get; private set; }

    public double TotalBlockingMs { get; private set; }

    public double LongestFrameMs { get; private set; }

    public int InteractionCount { get; private set; }

    public BevInteraction? SlowestInteraction { get; private set; }

    public BevLongFrame[] Frames => _frames.ToArray();

    public BevInteraction[] Interactions => _interactions.ToArray();

    /// <summary>
    /// The interaction latency below which 98 % of this session's interactions fell (the slowest one under 50
    /// interactions): how the Web Vitals define Interaction to Next Paint, over what BevTools saw. Null without any.
    /// </summary>
    public double? InteractionToNextPaintMs
    {
        get
        {
            double[] sorted;
            lock (_lock)
            {
                sorted = _durations?.ToArray() ?? [];
            }

            if (sorted.Length == 0)
            {
                return null;
            }

            Array.Sort(sorted);
            // One outlier is ignored for every 50 interactions, as the metric does.
            return sorted[Math.Max(0, sorted.Length - 1 - sorted.Length / 50)];
        }
    }

    internal void Add(BevLongFrame frame)
    {
        lock (_lock)
        {
            LongFrameCount++;
            TotalBlockingMs += frame.BlockingMs;
            LongestFrameMs = Math.Max(LongestFrameMs, frame.DurationMs);
        }

        _frames.Add(frame);
    }

    internal void Add(BevInteraction interaction)
    {
        lock (_lock)
        {
            InteractionCount++;
            (_durations ??= new(10_000)).Add(interaction.DurationMs);
            if (SlowestInteraction is null || interaction.DurationMs > SlowestInteraction.DurationMs)
            {
                SlowestInteraction = interaction;
            }
        }

        _interactions.Add(interaction);
    }

    internal void Clear()
    {
        lock (_lock)
        {
            LongFrameCount = 0;
            TotalBlockingMs = 0;
            LongestFrameMs = 0;
            InteractionCount = 0;
            SlowestInteraction = null;
            _durations?.Clear();
        }

        _frames.Clear();
        _interactions.Clear();
    }

    /// <summary>
    /// Records what the hub reported: each long frame and each interaction of 50 ms or more in the timeline, dated from its
    /// age, and on WebAssembly - where .NET runs on the browser's main thread - the application work that started inside
    /// it: the UI event whose handler ran in an interaction's processing, the events and batches that ran in a frame. On a
    /// circuit the .NET side runs on the server, on another clock, and nothing is matched.
    /// </summary>
    internal static void Record(BevToolsSession session, BevLongFramePayload[]? frames, BevInteractionPayload[]? interactions)
    {
        if (!session.IsCapturing)
        {
            return;
        }

        var nowTicks = Stopwatch.GetTimestamp();
        var now = DateTimeOffset.UtcNow;
        var sameThread = session.Platform == "WebAssembly";
        var events = sameThread ? session.Timeline.Snapshot() : [];
        var state = session.Browser.Responsiveness;

        foreach (var f in (frames ?? []).Take(Capacity))
        {
            if (!double.IsFinite(f.AgeMs) || !double.IsFinite(f.DurationMs) || f.DurationMs <= 0)
            {
                continue;
            }

            var startTicks = nowTicks - Ticks(f.AgeMs);
            var ownMs = Math.Min(sameThread ? session.RenderTracker.OwnRenderMsWithin(startTicks - Tolerance, startTicks + Ticks(f.DurationMs) + Tolerance) : 0, f.DurationMs);
            if (ownMs > 0 && ownMs >= f.DurationMs / 2)
            {
                // Mostly BevTools' own panel: not the application's frame, in the timeline or in the totals.
                continue;
            }

            var during = sameThread ? StartedWithin(events, startTicks, startTicks + Ticks(f.DurationMs), BevToolsEventKind.UiEvent, BevToolsEventKind.Navigation, BevToolsEventKind.RenderBatch) : [];
            var trigger = during.FirstOrDefault(e => e.Kind is BevToolsEventKind.UiEvent or BevToolsEventKind.Navigation);
            var scripts = (f.Scripts ?? []).Take(3).Select(Clean).ToArray();
            var frame = new BevLongFrame(now - TimeSpan.FromMilliseconds(f.AgeMs), f.DurationMs, Math.Max(0, f.BlockingMs), scripts)
            {
                During = during.Take(3).Select(e => e.Title).ToArray(),
                TriggerEventId = trigger?.Id,
                OwnMs = ownMs,
            };
            frame.TimelineEventId = session.Timeline.Record(new BevToolsEvent
            {
                Timestamp = frame.At,
                StartTicks = startTicks,
                Kind = BevToolsEventKind.Browser,
                Category = "long-frame",
                Title = "Long frame: main thread busy " + BevFmt.Ms(f.DurationMs) + (frame.BlockingMs > 0 ? " (" + BevFmt.Ms(frame.BlockingMs) + " blocking)" : ""),
                Detail = Describe(frame),
                DurationMs = f.DurationMs,
                Severity = frame.BlockingMs >= session.Options.Diagnostics.LongFrameBlockingMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
                ParentEventId = trigger?.Id,
            });
            state.Add(frame);
        }

        foreach (var i in (interactions ?? []).Take(Capacity))
        {
            if (!double.IsFinite(i.AgeMs) || !double.IsFinite(i.DurationMs) || i.DurationMs <= 0)
            {
                continue;
            }

            BevToolsEvent? handler = null;
            if (sameThread)
            {
                var handlers = StartedWithin(events, nowTicks - Ticks(i.ProcessingStartAgeMs), nowTicks - Ticks(i.ProcessingEndAgeMs), BevToolsEventKind.UiEvent);
                // One handler ran in it, or the match says nothing.
                handler = handlers.Count == 1 ? handlers[0] : null;
            }

            var name = Clip(i.Name, 40) ?? "event";
            var interaction = new BevInteraction(now - TimeSpan.FromMilliseconds(i.AgeMs), name, i.DurationMs, Math.Max(0, i.InputDelayMs), Math.Max(0, i.ProcessingMs), Math.Max(0, i.PresentationMs), Clip(i.Target, 200))
            {
                HandlerEventId = handler?.Id,
                HandlerTitle = handler?.Title,
            };
            if (i.DurationMs >= 50)
            {
                interaction.TimelineEventId = session.Timeline.Record(new BevToolsEvent
                {
                    Timestamp = interaction.At,
                    StartTicks = nowTicks - Ticks(i.AgeMs),
                    Kind = BevToolsEventKind.Browser,
                    Category = "interaction",
                    Title = name + (interaction.Target is null ? "" : " on " + interaction.Target) + ": next paint after " + BevFmt.Ms(i.DurationMs),
                    Detail = Breakdown(interaction),
                    DurationMs = i.DurationMs,
                    Severity = i.DurationMs >= session.Options.Diagnostics.SlowInteractionMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
                    ParentEventId = handler?.Id,
                });
            }

            state.Add(interaction);
        }

        session.Touch();
    }

    internal static string Breakdown(BevInteraction i) =>
        "input delay " + BevFmt.Ms(i.InputDelayMs) + " · processing " + BevFmt.Ms(i.ProcessingMs) + " · presentation " + BevFmt.Ms(i.PresentationMs)
        + (i.HandlerTitle is null ? "" : " · ran " + i.HandlerTitle);

    internal static string Describe(BevLongFrame frame)
    {
        var parts = new List<string>();
        if (frame.OwnMs > 0)
        {
            parts.Add(BevFmt.Ms(frame.OwnMs) + " of it BevTools' own panel rendering");
        }

        if (frame.During.Count > 0)
        {
            parts.Add("during " + string.Join(", ", frame.During));
        }

        foreach (var s in frame.Scripts)
        {
            parts.Add(BevFmt.Ms(s.DurationMs) + " " + (string.IsNullOrEmpty(s.Invoker) ? s.Type : s.Invoker) + (string.IsNullOrEmpty(s.Source) ? "" : " (" + s.Source + (string.IsNullOrEmpty(s.Fn) ? "" : " " + s.Fn) + ")"));
        }

        return parts.Count == 0 ? "no script attribution (this browser reports long tasks, not long animation frames)" : string.Join("; ", parts);
    }

    private static BevFrameScript Clean(BevFrameScript s) => new()
    {
        Invoker = Clip(s.Invoker, 200),
        Type = Clip(s.Type, 40),
        Source = Clip(s.Source, 200),
        Fn = Clip(s.Fn, 200),
        DurationMs = double.IsFinite(s.DurationMs) ? s.DurationMs : 0,
    };

    private static string? Clip(string? text, int max) => text is null ? null : BevRedactor.RedactText(text.Length <= max ? text : text[..max]);

    private static long Ticks(double ms) => (long)(Math.Max(0, ms) / 1000 * Stopwatch.Frequency);

    // The page's clock and the runtime's are read a moment apart (the report's transit): a margin of a few
    // milliseconds, no more, or the match would stop being evidence.
    private static readonly long Tolerance = Ticks(4);

    private static List<BevToolsEvent> StartedWithin(BevToolsEvent[] events, long from, long to, params BevToolsEventKind[] kinds)
    {
        var found = new List<BevToolsEvent>();
        for (var index = events.Length - 1; index >= 0; index--)
        {
            var e = events[index];
            if (Array.IndexOf(kinds, e.Kind) >= 0 && e.StartTicks >= from - Tolerance && e.StartTicks <= to + Tolerance)
            {
                found.Add(e);
            }
        }

        found.Reverse();
        return found;
    }
}
