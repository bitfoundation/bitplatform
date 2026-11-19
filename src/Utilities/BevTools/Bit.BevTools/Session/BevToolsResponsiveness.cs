using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>The page's responsiveness as the hub reported it: recent long frames and interactions, and session-long totals.</summary>
public sealed class BevToolsResponsiveness
{
    private const int Capacity = 200;

    /// <summary>How many of the newest interactions <see cref="InteractionToNextPaintMs"/> is worked out over.</summary>
    internal const int InpWindow = 10_000;

    private readonly BevToolsRingBuffer<BevToolsLongFrame> _frames = new(Capacity);
    private readonly BevToolsRingBuffer<BevToolsInteraction> _interactions = new(Capacity);
    private readonly object _lock = new();
    // The percentile needs every duration; a session's interactions are few, and past this many the slowest of the
    // last ones says as much. Made with the first one: a session that reports none (a server's request scope) pays nothing.
    private BevToolsRingBuffer<double>? _durations;

    /// <summary>What the browser supports; null until the hub attached.</summary>
    public BevToolsResponsivenessSupport? Support { get; internal set; }

    public int LongFrameCount { get; private set; }

    public double TotalBlockingMs { get; private set; }

    public double LongestFrameMs { get; private set; }

    public int InteractionCount { get; private set; }

    public BevToolsInteraction? SlowestInteraction { get; private set; }

    public BevToolsLongFrame[] Frames => _frames.ToArray();

    public BevToolsInteraction[] Interactions => _interactions.ToArray();

    /// <summary>
    /// The interaction latency below which 98 % of this session's interactions fell (the slowest one under 50
    /// interactions): how the Web Vitals define Interaction to Next Paint, over what BevTools saw - the newest
    /// <see cref="InpWindow"/> of them, where <see cref="InteractionCount"/> counts them all. Null without any.
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

    internal void Add(BevToolsLongFrame frame)
    {
        lock (_lock)
        {
            LongFrameCount++;
            TotalBlockingMs += frame.BlockingMs;
            LongestFrameMs = Math.Max(LongestFrameMs, frame.DurationMs);
        }

        _frames.Add(frame);
    }

    internal void Add(BevToolsInteraction interaction)
    {
        lock (_lock)
        {
            InteractionCount++;
            (_durations ??= new(InpWindow)).Add(interaction.DurationMs);
            if (SlowestInteraction is null || interaction.DurationMs > SlowestInteraction.DurationMs)
            {
                SlowestInteraction = interaction;
            }
        }

        _interactions.Add(interaction);
    }

    /// <summary>The interaction of this browser id among the recent ones; null for 0 or one no longer kept.</summary>
    internal BevToolsInteraction? Find(long interactionId) =>
        interactionId > 0 ? _interactions.FindLast(i => i.InteractionId == interactionId) : null;

    /// <summary>Puts the whole of an interaction in place of the part of it reported before: one interaction, counted once.</summary>
    internal void Replace(BevToolsInteraction earlier, BevToolsInteraction whole)
    {
        lock (_lock)
        {
            // The same multiset either way when another interaction took exactly as long.
            _durations?.TryReplaceLast(d => d == earlier.DurationMs, _ => whole.DurationMs);
            if (SlowestInteraction is null || ReferenceEquals(SlowestInteraction, earlier) || whole.DurationMs > SlowestInteraction.DurationMs)
            {
                SlowestInteraction = whole;
            }
        }

        _interactions.TryReplaceLast(i => ReferenceEquals(i, earlier), _ => whole);
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
    /// <para>
    /// Each entry is dated as its arrival less its age. On a circuit the report's way to the server - the network, and
    /// the wait for the circuit's dispatcher - is not in that age, so an entry reads that much later than it happened:
    /// milliseconds, against the minute or two the responsiveness rules look back over. The page and the server share no
    /// clock to measure it with.
    /// </para>
    /// </summary>
    internal static void Record(BevToolsSession session, BevToolsLongFramePayload[]? frames, BevToolsInteractionPayload[]? interactions)
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
            var ownComplete = true;
            var ownMs = Math.Min(sameThread ? session.RenderTracker.OwnRenderMsWithin(startTicks - Tolerance, startTicks + Ticks(f.DurationMs) + Tolerance, out ownComplete) : 0, f.DurationMs);
            if (ownMs > 0 && ownMs >= f.DurationMs / 2)
            {
                // Mostly BevTools' own panel: not the application's frame, in the timeline or in the totals.
                continue;
            }

            var during = sameThread ? StartedWithin(events, startTicks, startTicks + Ticks(f.DurationMs), BevToolsEventKind.UiEvent, BevToolsEventKind.Navigation, BevToolsEventKind.RenderBatch) : [];
            var trigger = during.FirstOrDefault(e => e.Kind is BevToolsEventKind.UiEvent or BevToolsEventKind.Navigation);
            var scripts = (f.Scripts ?? []).Take(3).Select(Clean).ToArray();
            var frame = new BevToolsLongFrame(now - TimeSpan.FromMilliseconds(f.AgeMs), f.DurationMs, Math.Max(0, f.BlockingMs), scripts)
            {
                During = during.Take(3).Select(e => e.Title).ToArray(),
                TriggerEventId = trigger?.Id,
                OwnMs = ownMs,
                OwnMsIsLowerBound = !ownComplete,
            };
            frame.TimelineEventId = session.Timeline.Record(new BevToolsEvent
            {
                Timestamp = frame.At,
                StartTicks = startTicks,
                Kind = BevToolsEventKind.Browser,
                Category = "long-frame",
                Title = "Long frame: main thread busy " + BevToolsFmt.Ms(f.DurationMs) + (frame.BlockingMs > 0 ? " (" + BevToolsFmt.Ms(frame.BlockingMs) + " blocking)" : ""),
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

            var interaction = new BevToolsInteraction(now - TimeSpan.FromMilliseconds(i.AgeMs), Clip(i.Name, 40) ?? "event", i.DurationMs, Math.Max(0, i.InputDelayMs), Math.Max(0, i.ProcessingMs), Math.Max(0, i.PresentationMs), Clip(i.Target, 200))
            {
                InteractionId = Math.Max(0, i.InteractionId),
            };

            // The hub puts the entries of one interaction together until it reports, but they can straddle two reports
            // (a pointerdown reported before its click): the part reported before and this one are one interaction.
            var earlier = state.Find(interaction.InteractionId);
            if (earlier is not null)
            {
                interaction = Merge(earlier, interaction);
            }

            BevToolsEvent? handler = null;
            if (sameThread)
            {
                var processingStart = interaction.At + TimeSpan.FromMilliseconds(interaction.InputDelayMs);
                var handlers = StartedWithin(events, TicksAt(processingStart), TicksAt(processingStart + TimeSpan.FromMilliseconds(interaction.ProcessingMs)), BevToolsEventKind.UiEvent);
                // One handler ran in it, or the match says nothing.
                handler = handlers.Count == 1 ? handlers[0] : null;
                interaction = interaction with { HandlerEventId = handler?.Id, HandlerTitle = handler?.Title };
            }

            if (interaction.DurationMs >= 50)
            {
                var evt = new BevToolsEvent
                {
                    Timestamp = interaction.At,
                    StartTicks = TicksAt(interaction.At),
                    Kind = BevToolsEventKind.Browser,
                    Category = "interaction",
                    Title = interaction.Name + (interaction.Target is null ? "" : " on " + interaction.Target) + ": next paint after " + BevToolsFmt.Ms(interaction.DurationMs),
                    Detail = Breakdown(interaction),
                    DurationMs = interaction.DurationMs,
                    Severity = interaction.DurationMs >= session.Options.Diagnostics.SlowInteractionMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
                    ParentEventId = handler?.Id,
                };
                interaction.TimelineEventId = earlier is { TimelineEventId: > 0 } && session.Timeline.Replace(earlier.TimelineEventId, evt) ? earlier.TimelineEventId : session.Timeline.Record(evt);
            }

            if (earlier is not null)
            {
                state.Replace(earlier, interaction);
            }
            else
            {
                state.Add(interaction);
            }
        }

        session.Touch();

        long TicksAt(DateTimeOffset at) => nowTicks - Ticks((now - at).TotalMilliseconds);
    }

    /// <summary>
    /// Two parts of one interaction as the hub puts its entries together: from the first input to the paint after the
    /// last, the processing spanning both, named after the part whose handlers took longer.
    /// </summary>
    private static BevToolsInteraction Merge(BevToolsInteraction a, BevToolsInteraction b)
    {
        static DateTimeOffset After(DateTimeOffset at, double ms) => at + TimeSpan.FromMilliseconds(ms);
        var start = a.At < b.At ? a.At : b.At;
        var end = After(a.At, a.DurationMs) > After(b.At, b.DurationMs) ? After(a.At, a.DurationMs) : After(b.At, b.DurationMs);
        var processingStart = After(a.At, a.InputDelayMs) < After(b.At, b.InputDelayMs) ? After(a.At, a.InputDelayMs) : After(b.At, b.InputDelayMs);
        var processingEnd = After(a.At, a.InputDelayMs + a.ProcessingMs) > After(b.At, b.InputDelayMs + b.ProcessingMs) ? After(a.At, a.InputDelayMs + a.ProcessingMs) : After(b.At, b.InputDelayMs + b.ProcessingMs);
        var named = b.ProcessingMs > a.ProcessingMs ? b : a;
        return new BevToolsInteraction(start, named.Name, (end - start).TotalMilliseconds, Math.Max(0, (processingStart - start).TotalMilliseconds),
            Math.Max(0, (processingEnd - processingStart).TotalMilliseconds), Math.Max(0, (end - processingEnd).TotalMilliseconds), named.Target ?? a.Target ?? b.Target)
        {
            InteractionId = a.InteractionId,
        };
    }

    internal static string Breakdown(BevToolsInteraction i) =>
        "input delay " + BevToolsFmt.Ms(i.InputDelayMs) + " · processing " + BevToolsFmt.Ms(i.ProcessingMs) + " · presentation " + BevToolsFmt.Ms(i.PresentationMs)
        + (i.HandlerTitle is null ? "" : " · ran " + i.HandlerTitle);

    internal static string Describe(BevToolsLongFrame frame)
    {
        var parts = new List<string>();
        if (frame.OwnMsIsLowerBound)
        {
            parts.Add("at least " + BevToolsFmt.Ms(frame.OwnMs) + " of it BevTools' own panel rendering (it rendered more often than BevTools keeps count of)");
        }
        else if (frame.OwnMs > 0)
        {
            parts.Add(BevToolsFmt.Ms(frame.OwnMs) + " of it BevTools' own panel rendering");
        }

        if (frame.During.Count > 0)
        {
            parts.Add("during " + string.Join(", ", frame.During));
        }

        foreach (var s in frame.Scripts)
        {
            parts.Add(BevToolsFmt.Ms(s.DurationMs) + " " + (string.IsNullOrEmpty(s.Invoker) ? s.Type : s.Invoker) + (string.IsNullOrEmpty(s.Source) ? "" : " (" + s.Source + (string.IsNullOrEmpty(s.Fn) ? "" : " " + s.Fn) + ")"));
        }

        return parts.Count == 0 ? "no script attribution" : string.Join("; ", parts);
    }

    private static BevToolsFrameScript Clean(BevToolsFrameScript s) => new()
    {
        Invoker = Clip(s.Invoker, 200),
        Type = Clip(s.Type, 40),
        Source = Clip(s.Source, 200),
        Fn = Clip(s.Fn, 200),
        DurationMs = double.IsFinite(s.DurationMs) ? s.DurationMs : 0,
    };

    private static string? Clip(string? text, int max) => text is null ? null : BevToolsRedactor.RedactText(text.Length <= max ? text : text[..max]);

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
