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
    /// <c>bmotion.strategy</c>.</item>
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

    /// <summary>
    /// Reports a part of an animation starting and, once <paramref name="end"/> resolves, ending - <c>true</c> for a
    /// natural finish. A part with no completion to watch (nobody waits for it) reports its start alone.
    /// </summary>
    private void ReportPart(
        string elementId, IEnumerable<string> keys, BmPlaybackStrategy strategy, string? reason, Task<bool>? end)
    {
        string[] properties = [.. keys];
        string strategyTag = StrategyTag(strategy);
        long startedAt = Stopwatch.GetTimestamp();
        if (s_started.Enabled) s_started.Add(1, new KeyValuePair<string, object?>("bmotion.strategy", strategyTag));
        Raise(new BmotionAnimationEvent(BmAnimationEventKind.Started, elementId, properties, strategy, reason));

        if (end is null) return;
        if (end.IsCompleted) Ended(end);
        else ((Task)end).OnCallerContext(_ => Ended(end));

        void Ended(Task<bool> settled)
        {
            bool finished = settled.IsCompletedSuccessfully && settled.Result;
            double elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            if (s_ended.Enabled)
                s_ended.Add(1,
                    new KeyValuePair<string, object?>("bmotion.strategy", strategyTag),
                    new KeyValuePair<string, object?>("bmotion.outcome", finished ? "finished" : "interrupted"));
            if (finished && s_duration.Enabled)
                s_duration.Record(elapsedMs / 1000, new KeyValuePair<string, object?>("bmotion.strategy", strategyTag));
            Raise(new BmotionAnimationEvent(
                finished ? BmAnimationEventKind.Finished : BmAnimationEventKind.Interrupted,
                elementId, properties, strategy, reason)
            { ElapsedMs = elapsedMs });
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

    /// <summary>
    /// Why <paramref name="keys"/> are not played by the browser: the first condition of <see cref="Partition"/> that
    /// keeps one of them on the frame loop, or the browser being unable to reproduce them exactly.
    /// </summary>
    private string LoopReason(
        BmotionElementAnimationState state, IEnumerable<string> keys, BmotionTransitionConfig? transition, bool browserDeclined)
    {
        var config = transition ?? new BmotionTransitionConfig();
        bool inProcess = _interop.IsInProcess;
        if (config.OnUpdate is not null || (config.Properties?.Values.Any(p => p.OnUpdate is not null) ?? false))
            return "a per-frame callback (OnUpdate) reports every frame, which only the frame loop produces";
        if (inProcess && state.OnFrame is not null) return "a per-frame callback (OnFrame) is attached to the element";
        if (inProcess && state.PlaybackRate != 1)
            return $"the element plays at a rate of {state.PlaybackRate:0.###} (paused or re-timed controls)";
        foreach (var key in keys)
        {
            if (key is "d" or "points") return $"SVG '{key}' is an attribute, which Web Animations can't animate";
            if (BmotionTransformComposer.IsTransformProp(key) && state.IsDragging)
                return "a drag owns the element's transform";
            if (BmotionTransformComposer.IsTransformProp(key) && inProcess && state.HasActiveTransformDriver())
                return "another transform component is already animating on the frame loop (transform is one CSS property)";
        }
        return browserDeclined
            ? "the browser can't reproduce it exactly (for example a recording longer than the recorder takes, or a curve without linear() support)"
            : "it needs the frame loop";
    }
}
