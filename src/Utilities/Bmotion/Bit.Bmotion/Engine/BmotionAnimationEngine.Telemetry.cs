using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Bit.Bmotion;

// What the engine reports about the animations it plays: an in-process event for tooling (an inspector, a devtools
// adapter, a test) and BCL metrics for telemetry. Both carry property names and strategies only - never animated values
// or page content - and cost nothing while nobody listens.
public sealed partial class BmotionAnimationEngine
{
    /// <summary>
    /// The name of the <see cref="Meter"/> the engine reports to, for a metrics pipeline to subscribe to (OpenTelemetry's
    /// <c>AddMeter("Bit.Bmotion")</c>, Bit.Belemetry's <c>options.Metrics.Meters.Add("Bit.Bmotion")</c>, a
    /// <see cref="MeterListener"/>). Its instruments:
    /// <list type="bullet">
    /// <item><c>bmotion.animation.started</c> - a counter, one per part of an animation that starts, tagged
    /// <c>bmotion.strategy</c> (<c>instant</c>, <c>browser_timeline</c>, <c>browser_recorded</c>, <c>frame_loop</c>,
    /// <c>instant_without_frame_loop</c>).</item>
    /// <item><c>bmotion.animation.ended</c> - a counter, one per part that ends, tagged <c>bmotion.strategy</c> and
    /// <c>bmotion.outcome</c> (<c>finished</c> or <c>interrupted</c>).</item>
    /// <item><c>bmotion.animation.duration</c> - a histogram of how long the parts that finished ran, in seconds, tagged
    /// <c>bmotion.strategy</c>. Instant parts, which run no time, aren't recorded.</item>
    /// </list>
    /// </summary>
    public const string MeterName = "Bit.Bmotion";

    private static readonly Meter s_meter = new(MeterName, typeof(BmotionAnimationEngine).Assembly.GetName().Version?.ToString());
    private static readonly Counter<long> s_started = s_meter.CreateCounter<long>(
        "bmotion.animation.started", "{animation}", "Animation parts started, by playback strategy.");
    private static readonly Counter<long> s_ended = s_meter.CreateCounter<long>(
        "bmotion.animation.ended", "{animation}", "Animation parts ended, by playback strategy and outcome.");
    private static readonly Histogram<double> s_duration = s_meter.CreateHistogram<double>(
        "bmotion.animation.duration", "s", "How long the animation parts that finished ran.");

    /// <summary>
    /// Raised as each part of an animation starts and ends, with how it plays and - when it runs on the frame loop or
    /// lands without one - why. An animation whose properties play in different ways reports each part on its own.
    /// It is raised on the thread the engine runs on (the renderer's), so a handler can update a component; keep it
    /// cheap - it runs inline with the animation start. An exception from a handler is logged and goes no further.
    /// <code>
    /// Engine.AnimationEvent += e =&gt;
    /// {
    ///     if (e.Strategy == BmPlaybackStrategy.InstantWithoutFrameLoop) Logger.LogWarning("{Reason}", e.Reason);
    /// };
    /// </code>
    /// The event and the <see cref="MeterName"/> meter carry property names, never the animated values.
    /// </summary>
    public event Action<BmotionAnimationEvent>? AnimationEvent;

    /// <summary>Whether anybody listens: only then are parts tracked and their ends watched.</summary>
    private bool IsObserved => AnimationEvent is not null || s_started.Enabled || s_ended.Enabled || s_duration.Enabled;

    // The reported parts still running, per element: a start that takes over some of their properties ends them
    // (interrupted) before it reports its own start, so a consumer always hears the old part end before the new one
    // starts - the supersession itself settles their completions only later, asynchronously. The engine runs on one
    // context (the renderer's), where the lock is uncontended; it keeps a host without one (a test, a background
    // service) from ending a part twice or hearing the two out of order.
    private readonly Dictionary<string, List<ReportedPart>> _runningParts = new(StringComparer.Ordinal);
    private readonly Lock _partsLock = new();

    private sealed class ReportedPart(
        string elementId, string[] properties, BmPlaybackStrategy strategy, string? reason, long startedAt)
    {
        public string ElementId { get; } = elementId;
        public string[] Properties { get; } = properties;
        public BmPlaybackStrategy Strategy { get; } = strategy;
        public string? Reason { get; } = reason;
        public long StartedAt { get; } = startedAt;
        public bool Ended { get; set; }

        public bool Overlaps(string[] properties)
        {
            foreach (var p in properties)
                if (Array.IndexOf(Properties, p) >= 0) return true;
            return false;
        }
    }

    /// <summary>
    /// Reports a part of an animation starting and, once <paramref name="end"/> resolves, ending - <c>true</c> for a
    /// natural finish. A part with no completion to watch (nobody waits for it) reports its start alone. A running
    /// part of the same element that animates any of these properties ends first, as interrupted.
    /// </summary>
    private void ReportPart(
        string elementId, IEnumerable<string> keys, BmPlaybackStrategy strategy, string? reason, Task<bool>? end)
    {
        string[] properties = [.. keys];
        var part = new ReportedPart(elementId, properties, strategy, reason, Stopwatch.GetTimestamp());
        lock (_partsLock)
        {
            if (_runningParts.TryGetValue(elementId, out var running))
                foreach (var superseded in running.Where(p => p.Overlaps(properties)).ToList())
                    EndPart(superseded, finished: false);

            if (s_started.Enabled) s_started.Add(1, new KeyValuePair<string, object?>("bmotion.strategy", StrategyTag(strategy)));
            Raise(new BmotionAnimationEvent(BmAnimationEventKind.Started, elementId, properties, strategy, reason));

            if (end is null) return;
            if (end.IsCompleted)
            {
                EndPart(part, end.IsCompletedSuccessfully && end.Result);
                return;
            }
            // Fetched again: ending the superseded parts may have dropped the element's list.
            if (!_runningParts.TryGetValue(elementId, out running)) _runningParts[elementId] = running = new List<ReportedPart>();
            running.Add(part);
        }
        ((Task)end).OnCallerContext(_ => EndPart(part, end.IsCompletedSuccessfully && end.Result));
    }

    /// <summary>Reports <paramref name="part"/> ending, once: a later end of an already ended part is ignored.</summary>
    private void EndPart(ReportedPart part, bool finished)
    {
        lock (_partsLock)
        {
            if (part.Ended) return;
            part.Ended = true;
            if (_runningParts.TryGetValue(part.ElementId, out var running) && running.Remove(part) && running.Count == 0)
                _runningParts.Remove(part.ElementId);

            string strategyTag = StrategyTag(part.Strategy);
            double elapsedMs = Stopwatch.GetElapsedTime(part.StartedAt).TotalMilliseconds;
            if (s_ended.Enabled)
                s_ended.Add(1,
                    new KeyValuePair<string, object?>("bmotion.strategy", strategyTag),
                    new KeyValuePair<string, object?>("bmotion.outcome", finished ? "finished" : "interrupted"));
            // An instant part runs no time: recording its ~0 s would only drag the distribution of real runs down.
            if (finished && part.Strategy != BmPlaybackStrategy.Instant && s_duration.Enabled)
                s_duration.Record(elapsedMs / 1000, new KeyValuePair<string, object?>("bmotion.strategy", strategyTag));
            Raise(new BmotionAnimationEvent(
                finished ? BmAnimationEventKind.Finished : BmAnimationEventKind.Interrupted,
                part.ElementId, part.Properties, part.Strategy, part.Reason)
            { ElapsedMs = elapsedMs });
        }
    }

    /// <summary>Ends every part of <paramref name="elementId"/> still running, as interrupted (its element is gone).</summary>
    private void EndRunningParts(string elementId)
    {
        lock (_partsLock)
        {
            if (!_runningParts.TryGetValue(elementId, out var running)) return;
            foreach (var part in running.ToList()) EndPart(part, finished: false);
            _runningParts.Remove(elementId);
        }
    }

    private void Raise(BmotionAnimationEvent e)
    {
        if (AnimationEvent is not { } handlers) return;
        foreach (var handler in handlers.GetInvocationList())
        {
            try { ((Action<BmotionAnimationEvent>)handler)(e); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Bit.Bmotion: an AnimationEvent handler threw for '{ElementId}'.", e.ElementId);
            }
        }
    }

    private static string StrategyTag(BmPlaybackStrategy strategy) => strategy switch
    {
        BmPlaybackStrategy.Instant => "instant",
        BmPlaybackStrategy.BrowserTimeline => "browser_timeline",
        BmPlaybackStrategy.BrowserRecorded => "browser_recorded",
        BmPlaybackStrategy.FrameLoop => "frame_loop",
        _ => "instant_without_frame_loop",
    };

    // Why a part plays on the frame loop: Partition picks the reason as it routes the first key there, from the
    // condition that routed it - so the report is the decision itself, not a re-derivation after it.
    private const string OnUpdateReason = "a per-frame callback (OnUpdate) reports every frame, which only the frame loop produces";
    private const string OnFrameReason = "a per-frame callback (OnFrame) is attached to the element";
    private const string DragReason = "a drag owns the element's transform";
    private const string TransformDriverReason =
        "another transform component is already animating on the frame loop (transform is one CSS property)";
    private const string BrowserDeclinedReason =
        "the browser can't reproduce it exactly (for example a recording longer than the recorder takes, or a curve without linear() support)";
    private const string NeverStartedReason =
        "the browser didn't start it (the element was missing or an easing was rejected), so it replays on the frame loop";
    private const string HandOffReason =
        "another animation took over part of its browser animation (transform is one CSS property), so the rest carries on on the frame loop";

    private static string PlaybackRateReason(double rate) => $"the element plays at a rate of {rate:0.###} (paused or re-timed controls)";

    private static string SvgAttributeReason(string key) => $"SVG '{key}' is an attribute, which Web Animations can't animate";
}
