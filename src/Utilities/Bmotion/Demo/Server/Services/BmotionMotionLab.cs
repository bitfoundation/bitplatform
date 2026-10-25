using System.Globalization;
using Bit.Bmotion.Demo.Server.Dtos;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Runs Bit.Bmotion off-screen, so the MCP tools can answer questions about motion with measurements.
/// <para>
/// Two questions about an animation cannot be answered by reading documentation, and an agent that
/// guesses at either produces code that looks right and feels wrong:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>What does this transition actually do?</b> A spring has no duration argument - how long it
///     takes and how far it overshoots fall out of the physics. <see cref="SimulateAsync"/> plays
///     the transition on the real driver and reports the settle time, the overshoot and the shape.
///   </description></item>
///   <item><description>
///     <b>Where will it play, and what will it cost?</b> The engine hands almost every animation to
///     the browser as a Web Animation - a two-keyframe timeline when one exact curve covers it, a
///     recording of the frames its own drivers draw otherwise - and both play on Blazor Server. Only
///     a few need the per-frame loop, which Blazor Server does not have, and collapse to an instant
///     change there. The rule set behind that decision is long and lives in the engine.
///     <see cref="AnalyzePlaybackAsync(IReadOnlyCollection{string}, BmTransition, string)"/> asks the
///     engine instead of restating it: it starts the animation as each runtime and watches what the
///     engine hands the browser.
///   </description></item>
/// </list>
/// <para>
/// Every run gets its own engine and its own <see cref="HeadlessBmotionInterop"/>, so one
/// simulation cannot see another's elements, and the frame clock starts at zero each time.
/// </para>
/// </summary>
public static class BmotionMotionLab
{
    /// <summary>The frame interval the browser would tick at - 60 fps, the resolution of the answer.</summary>
    private const double FrameMs = 1000.0 / 60;

    /// <summary>
    /// The longest motion that will be simulated. A badly conditioned spring (very low damping) can
    /// ring for a long time; past this the answer is "it does not settle", which is the useful
    /// finding, and there is no reason to spend the frames proving it further.
    /// </summary>
    private const double MaxSeconds = 20;

    /// <summary>How many evenly spaced samples come back, regardless of how long the motion ran.</summary>
    private const int SampleCount = 24;

    /// <summary>
    /// Plays <paramref name="transition"/> from <paramref name="from"/> to <paramref name="to"/> on
    /// the library's own driver and measures the result.
    /// </summary>
    /// <param name="spec">The transition spec as written by the caller, e.g. "spring(stiffness: 260, damping: 12)".</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The target value.</param>
    public static async Task<BmotionSimulationDto> SimulateAsync(string? spec, double from = 0, double to = 100)
    {
        var parsed = BmotionTransitionSpec.Parse(spec);

        // A spec that could not be read is a correctable mistake, not a failure: handing it back as
        // data keeps the explanation intact. Thrown instead, MCP reduces it to "an error occurred
        // invoking SimulateBmotionTransition", and the caller learns nothing about how to fix it.
        if (parsed.Transition is null) return Unreadable(spec, parsed.Error, from, to);

        var warnings = new List<string>(parsed.Warnings);
        var transition = parsed.Transition;

        // An endless animation has no settle time to measure, and pumping it would only stop at the
        // frame cap. Measure one pass instead and say so - the shape is what the caller is after.
        if (transition.Repeat is { IsForever: true })
        {
            transition.Repeat = null;
            warnings.Add("The repeat was dropped for this simulation: an endless animation has no settle time. " +
                         "The numbers below describe one pass of it.");
        }

        var (frames, settled) = await RecordAsync(transition, from, to);

        // Inertia has no target: it decelerates from its velocity and rests wherever the physics
        // puts it. Measuring it against the caller's "to" would report a meaningless overshoot, so
        // the measurement is retargeted at where it actually stopped and the caller is told why.
        if (transition is BmInertia && frames.Count > 0)
        {
            warnings.Add($"Inertia ignores the target: it decelerates from its velocity and comes to rest " +
                         $"wherever that puts it - here at {frames[^1].Value:0.##}, not at {to:0.##}. The numbers " +
                         "below measure the glide to that resting point.");

            to = frames[^1].Value;
        }

        if (settled is false)
        {
            warnings.Add($"The motion had not come to rest after {MaxSeconds:0} seconds. That is almost always " +
                         "too little damping for the stiffness - raise 'damping', or switch to bounce/duration, " +
                         "which cannot be configured into a spring that never settles.");
        }

        return Measure(parsed.Canonical, KindOf(transition), from, to, frames, warnings);
    }

    /// <summary>
    /// Samples one <see cref="BmEase"/> preset by running a one-second tween through it and reading
    /// the value back at <paramref name="points"/> evenly spaced instants.
    /// <para>
    /// The curve therefore comes from the library's own easing implementation rather than from a
    /// formula restated here - which matters most for exactly the presets nobody can picture:
    /// whether BackOut overshoots by 5% or 15%, and where BounceOut lands between its steps.
    /// </para>
    /// </summary>
    public static async Task<double[]> SampleEaseAsync(BmEase ease, int points = 11)
    {
        var (frames, _) = await RecordAsync(new BmTween { Duration = 1, Ease = ease }, 0, 1);

        if (frames.Count == 0) return [];

        // Two is the fewest points a curve can be read from, and the fraction below divides by
        // points - 1: asking for one or fewer would sample nothing, or divide by zero.
        points = Math.Max(2, points);

        var curve = new double[points];

        for (int i = 0; i < points; i++)
        {
            var at = (double)i / (points - 1);

            // The frames land on the 60 fps grid, not on the tenths being asked about, so the value
            // between two of them is interpolated the way the browser would show it.
            curve[i] = ValueAt(frames, at);
        }

        return curve;
    }

    /// <summary>
    /// Starts a real animation on the engine, once as Blazor WebAssembly and once as Blazor Server,
    /// and reports where each played it - in the browser as a timeline or a recording, on the frame
    /// loop, or at once. That is what decides whether it survives on Blazor Server, and what it costs.
    /// </summary>
    /// <param name="properties">The animated properties, by their <c>Bm.To(...)</c> names.</param>
    /// <param name="spec">The transition spec, e.g. "tween(0.4, InOut)".</param>
    public static async Task<BmotionPlaybackDto> AnalyzePlaybackAsync(IReadOnlyCollection<string> properties, string? spec)
    {
        var parsed = BmotionTransitionSpec.Parse(spec);

        if (parsed.Transition is null)
        {
            var error = parsed.Error ?? "The transition could not be read.";

            return NotAnalysed(properties, spec ?? string.Empty, error);
        }

        return await AnalyzePlaybackAsync(properties, parsed.Transition, parsed.Canonical);
    }

    /// <summary>
    /// The same analysis for a transition built in C#, which can carry what a spec string cannot
    /// write: a repeat, per-property transitions, an arc, an <c>OnUpdate</c> callback.
    /// </summary>
    /// <param name="properties">The animated properties, by their <c>Bm.To(...)</c> names.</param>
    /// <param name="transition">The transition to start them with.</param>
    /// <param name="canonical">How the answer names the transition; its type name when omitted.</param>
    public static async Task<BmotionPlaybackDto> AnalyzePlaybackAsync(
        IReadOnlyCollection<string> properties, BmTransition transition, string? canonical = null)
    {
        var name = canonical ?? transition.GetType().Name;
        var target = BmotionPropertyCatalog.BuildTarget(properties, out var unknown);

        var ignored = unknown.Length > 0
            ? $" Ignored, because Bm.To has no such argument: {string.Join(", ", unknown)}."
            : string.Empty;

        // The verdict is about the properties that were actually animated, as BuildTarget read them.
        var known = properties.Select(BmotionPropertyCatalog.Normalize)
                              .Where(property => unknown.Contains(property, StringComparer.OrdinalIgnoreCase) is false)
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToArray();

        // A verdict about an animation of nothing would be a claim about no animation at all - the
        // engine would report it as instant, which reads as a fact about the transition.
        if (known.Length == 0)
        {
            var nothing = properties.Count == 0
                ? "No properties were given, so there was nothing to animate."
                : "None of these is a property Bm.To animates, so there was nothing to run.";

            return NotAnalysed(properties, name, nothing + ignored);
        }

        // Both runs start from the same page: every property the engine has never seen shows a value of
        // the target's own shape, as it would on an element whose stylesheet or Initial gives it one.
        var starts = BmotionPropertyCatalog.StartingStyles(known);

        var onWebAssembly = await ObserveAsync(target, transition, starts, onWebAssembly: true);
        var onServer = await ObserveAsync(target, transition, starts, onWebAssembly: false);

        // Only WebAssembly has a frame loop, so that is where what needs one shows: the drivers still
        // registered as the animation starts. On Server the same properties have already landed.
        var loopKeys = onWebAssembly.FrameLoopKeys;
        var loopProperties = known.Where(property => loopKeys.Contains(BmotionPropertyCatalog.EngineKeyOf(property), StringComparer.OrdinalIgnoreCase))
                                  .ToArray();

        // A driver under a key no named property maps to (an arc's rotation, say) still means the frame
        // loop has the animation: blaming every property is truer than blaming none.
        if (loopKeys.Length > 0 && loopProperties.Length == 0) loopProperties = known;

        var browserProperties = known.Except(loopProperties, StringComparer.OrdinalIgnoreCase).ToArray();

        var browser = onWebAssembly.Browser ?? onServer.Browser;

        BmotionPlaybackPath? shape = browser is null
            ? null
            : browser.Recorded ? BmotionPlaybackPath.Recorded : BmotionPlaybackPath.Timeline;

        var path = (shape, loopProperties.Length > 0) switch
        {
            ({ } played, false) => Describe(played),
            ({ } played, true) => $"{Describe(played)} + frame loop only (instant on Blazor Server)",
            (null, true) => "Frame loop only (instant on Blazor Server)",
            _ => "Instant"
        };

        var reasons = new List<string>();
        string[]? howToOffload = null;
        string[]? howToTimeline = null;

        if (shape is { } browserShape)
        {
            // What the browser cannot interpolate exactly whatever the transition is a fact about the
            // property, which the catalog has already measured - so it is only asked for when it matters.
            var recordedOnTheirOwn = browserShape == BmotionPlaybackPath.Recorded
                ? (await BmotionPropertyCatalog.GetAsync()).Where(property => property.BrowserPlayback == "Recorded")
                                                           .Select(property => property.Name)
                                                           .ToArray()
                : [];

            var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(browserProperties, transition, browserShape, recordedOnTheirOwn);

            reasons.Add(loopProperties.Length > 0 ? $"{string.Join(", ", browserProperties)}: {reason}" : reason);

            if (browserShape == BmotionPlaybackPath.Recorded && remedies.Length > 0) howToTimeline = remedies;
        }

        if (loopProperties.Length > 0)
        {
            var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(loopProperties, transition, BmotionPlaybackPath.FrameLoop);

            reasons.Add(shape is not null ? $"{string.Join(", ", loopProperties)}: {reason}" : reason);
            howToOffload = remedies;
        }

        if (shape is null && loopProperties.Length == 0)
        {
            var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(known, transition, BmotionPlaybackPath.Instant);

            reasons.Add(reason);

            if (remedies.Length > 0) howToOffload = remedies;
        }

        return new BmotionPlaybackDto
        {
            Properties = [.. properties],
            Transition = name,
            Path = path,
            OnWebAssembly = onWebAssembly.Verdict,
            OnBlazorServer = onServer.Verdict,
            // Server plays it as WebAssembly does only when nothing waited for a frame loop, and the
            // browser was handed it on both or on neither.
            WorksOnBlazorServer = loopProperties.Length == 0 && (onWebAssembly.Browser is null) == (onServer.Browser is null),
            FrameLoopProperties = loopProperties.Length > 0 ? loopProperties : null,
            Reason = string.Join(" ", reasons) + ignored,
            BrowserKeyframes = browser?.Keyframes,
            BrowserDurationMs = browser?.DurationMs,
            BrowserEasing = browser?.Easing,
            HowToOffload = howToOffload,
            HowToPlayAsTimeline = howToTimeline
        };

        static string Describe(BmotionPlaybackPath played)
            => played == BmotionPlaybackPath.Recorded ? "Browser (recorded)" : "Browser (timeline)";
    }

    /// <summary>
    /// What one runtime did with an animation the moment it started.
    /// </summary>
    /// <param name="Browser">The Web Animation the browser was handed, or null when there was none.</param>
    /// <param name="FrameLoopKeys">The engine keys the frame loop drives - only ever any on WebAssembly.</param>
    internal sealed record PlaybackObservation(BrowserAnimation? Browser, string[] FrameLoopKeys)
    {
        /// <summary>The runtime's verdict in one phrase, as the playback answer reports it.</summary>
        public string Verdict => (Browser, FrameLoopKeys.Length > 0) switch
        {
            ({ Recorded: true }, false) => "Browser (recorded)",
            ({ }, false) => "Browser (timeline)",
            ({ Recorded: true }, true) => "Browser (recorded) + frame loop",
            ({ }, true) => "Browser (timeline) + frame loop",
            (null, true) => "Frame loop",
            _ => "Instant"
        };
    }

    /// <summary>The Web Animation the engine handed the browser, as far as the verdict needs it.</summary>
    /// <param name="Recorded">True for a recording, false for a two-keyframe timeline.</param>
    /// <param name="Keyframes">How many keyframes it carried.</param>
    /// <param name="DurationMs">Its duration, when the timing reported one readably.</param>
    /// <param name="Easing">Its easing: the curve for a timeline, <c>linear</c> for a recording.</param>
    internal sealed record BrowserAnimation(bool Recorded, int Keyframes, double? DurationMs, string? Easing);

    /// <summary>
    /// Starts <paramref name="target"/> on a fresh engine and reports what the engine did with it as it
    /// started: what it handed the browser, and what it left to the frame loop. The verdict is read
    /// before a single frame is drawn - the engine takes it while the animation starts - and the
    /// animation is stopped straight after, since nothing here waits for the motion itself.
    /// <para>
    /// Shared by <see cref="AnalyzePlaybackAsync(IReadOnlyCollection{string}, BmTransition, string)"/>
    /// and the property catalog's probe, so the two cannot measure the same thing differently.
    /// </para>
    /// </summary>
    /// <param name="target">What to animate.</param>
    /// <param name="transition">How.</param>
    /// <param name="computedStyles">What the page shows for the properties the engine has never seen.</param>
    /// <param name="onWebAssembly">True to run as Blazor WebAssembly, with a frame loop; false to run as Blazor Server.</param>
    internal static async Task<PlaybackObservation> ObserveAsync(
        BmProps target, BmTransition transition, IReadOnlyDictionary<string, string> computedStyles, bool onWebAssembly)
    {
        var interop = new HeadlessBmotionInterop { IsInProcess = onWebAssembly, ComputedStyles = computedStyles };
        await using var engine = new BmotionAnimationEngine(interop);
        var service = new BmotionAnimateService(engine, interop);

        var controls = await service.AnimateAsync(".bmotion-target", target, transition);

        // The hand-off to the browser is asynchronous; give its continuations a turn before reading it.
        await Task.Yield();
        await Task.Yield();

        // Read before anything ticks: a frame-loop driver is registered from the moment the animation
        // starts until its last frame, and no frame is ever drawn here.
        var loopKeys = engine.GetDiagnostics()
                             .SelectMany(element => element.ActiveProperties)
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToArray();

        controls.Stop();

        return new PlaybackObservation(interop.WaapiCalls.Count > 0 ? DescribeBrowserAnimation(interop.WaapiCalls[0]) : null, loopKeys);
    }

    /// <summary>
    /// Reads the shape of a Web Animation off what the engine handed the interop. A recording is the
    /// one shape whose keyframes carry their own offsets - it sends only the frames a straight line
    /// cannot reproduce, so they are unevenly spaced - and it plays them with a linear easing. A
    /// timeline's keyframes sit at the two ends and need no offsets; its easing is the curve.
    /// </summary>
    private static BrowserAnimation DescribeBrowserAnimation(HeadlessBmotionInterop.WaapiCall call)
    {
        var keyframes = (call.Keyframes as System.Collections.IEnumerable)?.Cast<object>().ToArray() ?? [];
        var timing = AsDictionary(call.Timing);

        var recorded = keyframes.Any(keyframe => keyframe is IDictionary<string, object> values && values.ContainsKey("offset"));

        // The timing is whatever the engine handed the interop layer, so it is read defensively: a
        // value of an unexpected shape leaves it unreported rather than throwing out of a tool whose
        // whole answer is the path, not the milliseconds.
        double? durationMs = timing is not null
                          && timing.TryGetValue("duration", out var duration)
                          && double.TryParse(duration?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

        var easing = timing is not null && timing.TryGetValue("easing", out var value) ? value?.ToString() : null;

        return new BrowserAnimation(recorded, keyframes.Length, durationMs, easing);
    }

    /// <summary>
    /// The answer for a playback question with nothing to run: an unreadable transition, or no property
    /// Bm.To has. The explanation goes in both <c>Error</c> and <c>Reason</c>, so a caller that only
    /// renders the human-facing field still sees it, and no verdict is claimed.
    /// </summary>
    private static BmotionPlaybackDto NotAnalysed(IReadOnlyCollection<string> properties, string transition, string error)
        => new()
        {
            Properties = [.. properties],
            Transition = transition,
            Path = "Not analysed",
            WorksOnBlazorServer = false,
            Reason = error,
            Error = error
        };

    /// <summary>
    /// Plays a transition on the engine and records every value it produced, against the frame clock
    /// the simulation drives itself. A three-second animation is measured in microseconds of real
    /// time, because nothing here waits for a real frame.
    /// </summary>
    /// <returns>The recorded frames, and whether the motion came to rest inside the frame cap.</returns>
    private static async Task<(List<(double Seconds, double Value)> Frames, bool Settled)> RecordAsync(
        BmTransition transition, double from, double to)
    {
        var frames = new List<(double Seconds, double Value)>(256);

        var interop = new HeadlessBmotionInterop();
        await using var engine = new BmotionAnimationEngine(interop);
        var service = new BmotionAnimateService(engine, interop);

        var clockMs = 0.0;
        var animation = service.AnimateAsync(from, to, value => frames.Add((clockMs / 1000, value)), transition);

        var maxFrames = (int)(MaxSeconds * 1000 / FrameMs);

        for (int frame = 0; frame < maxFrames && animation.IsCompleted is false; frame++)
        {
            engine.ComputeFrame(clockMs);
            clockMs += FrameMs;
        }

        // The driver resolves its completion on the thread pool, so the loop above can exit a frame
        // or two before the task itself reports done. Nothing is left to tick either way.
        return (frames, await WaitOrAbandonAsync(animation));
    }

    /// <summary>
    /// The answer for a transition that was asked about but never run - one past the per-call limit,
    /// say. It carries the reason where a failed spec carries its parse error, so a caller renders it
    /// beside the measured ones instead of being handed back fewer answers than questions.
    /// </summary>
    public static BmotionSimulationDto NotMeasured(string spec, string reason, double from, double to)
        => Unreadable(spec, reason, from, to);

    /// <summary>
    /// The answer for a transition spec that could not be read: no measurement, and the explanation
    /// of how to write it in both <c>Error</c> and <c>Reading</c>, so a caller that only renders the
    /// human-facing field still sees it.
    /// </summary>
    private static BmotionSimulationDto Unreadable(string? spec, string? error, double from, double to)
    {
        var message = error ?? "The transition could not be read.";

        return new BmotionSimulationDto
        {
            Transition = spec ?? string.Empty,
            Kind = "Unknown",
            From = from,
            To = to,
            SettleSeconds = 0,
            OvershootPercent = 0,
            TargetCrossings = 0,
            PeakVelocity = 0,
            TimeTo90Percent = 0,
            Samples = [],
            Sparkline = string.Empty,
            Reading = message,
            Warnings = [],
            Error = message
        };
    }

    /// <summary>The recorded value at a moment between two frames, linearly interpolated.</summary>
    private static double ValueAt(List<(double Seconds, double Value)> frames, double seconds)
    {
        if (seconds <= frames[0].Seconds) return frames[0].Value;
        if (seconds >= frames[^1].Seconds) return frames[^1].Value;

        for (int i = 1; i < frames.Count; i++)
        {
            if (frames[i].Seconds < seconds) continue;

            var (previousSeconds, previousValue) = frames[i - 1];
            var (nextSeconds, nextValue) = frames[i];
            var span = nextSeconds - previousSeconds;

            return span <= 0
                ? nextValue
                : previousValue + (nextValue - previousValue) * ((seconds - previousSeconds) / span);
        }

        return frames[^1].Value;
    }

    /// <summary>
    /// Waits briefly for the animation to acknowledge that it finished. A motion that never settles
    /// is abandoned rather than awaited: its driver died with the engine at the end of the frame
    /// loop above, so nothing is still running - only its completion will never be signalled.
    /// </summary>
    private static async Task<bool> WaitOrAbandonAsync(Task animation)
    {
        try
        {
            await animation.WaitAsync(TimeSpan.FromSeconds(2));

            return true;
        }
        catch
        {
            // Timed out, or the animation faulted. Either is the same answer to the only question
            // being asked - did it finish - and the caller reports the recording as unreadable
            // rather than letting a headless run throw out of a tool call.
            return false;
        }
    }

    private static BmotionSimulationDto Measure(
        string canonical, string kind, double from, double to,
        List<(double Seconds, double Value)> frames, List<string> warnings)
    {
        // A transition with no frames at all animated nothing - a zero-length distance, or a driver
        // that refused the values. Reporting zeroes would read as "instant", which is a different
        // thing, so the reading says which it was.
        if (frames.Count == 0)
        {
            return new BmotionSimulationDto
            {
                Transition = canonical,
                Kind = kind,
                From = from,
                To = to,
                SettleSeconds = 0,
                OvershootPercent = 0,
                TargetCrossings = 0,
                PeakVelocity = 0,
                TimeTo90Percent = 0,
                Samples = [],
                Sparkline = string.Empty,
                Reading = "Nothing was animated: the engine produced no frames for this transition.",
                Warnings = [.. warnings]
            };
        }

        var distance = to - from;
        var span = Math.Abs(distance);
        var last = frames[^1];

        var overshoot = 0.0;
        var crossings = 0;
        var peakVelocity = 0.0;
        var timeTo90 = last.Seconds;
        var reached90 = false;

        for (int i = 0; i < frames.Count; i++)
        {
            var (seconds, value) = frames[i];

            // How far past the target this frame went, in the direction of travel. A motion that
            // undershoots contributes nothing.
            var beyond = span == 0 ? 0 : (distance >= 0 ? value - to : to - value);
            if (beyond > overshoot) overshoot = beyond;

            if (reached90 is false && span > 0 && Math.Abs(value - from) >= span * 0.9)
            {
                timeTo90 = seconds;
                reached90 = true;
            }

            if (i == 0) continue;

            var (previousSeconds, previousValue) = frames[i - 1];
            var dt = seconds - previousSeconds;

            if (dt > 0) peakVelocity = Math.Max(peakVelocity, Math.Abs(value - previousValue) / dt);

            // A sign change in "which side of the target are we on" is one crossing: the count is
            // the number of wobbles a viewer sees.
            var before = previousValue - to;
            var after = value - to;

            if (before != 0 && after != 0 && Math.Sign(before) != Math.Sign(after)) crossings++;
        }

        var overshootPercent = span > 0 ? overshoot / span * 100 : 0;

        return new BmotionSimulationDto
        {
            Transition = canonical,
            Kind = kind,
            From = from,
            To = to,
            SettleSeconds = Round(last.Seconds),
            OvershootPercent = Round(overshootPercent),
            TargetCrossings = crossings,
            PeakVelocity = Round(peakVelocity),
            TimeTo90Percent = Round(timeTo90),
            Samples = Resample(frames),
            Sparkline = Sparkline([.. frames.Select(f => f.Value)], from, to),
            Reading = Read(kind, last.Seconds, overshootPercent, crossings, timeTo90),
            Warnings = [.. warnings]
        };
    }

    /// <summary>
    /// Thins the recorded frames down to a fixed number of evenly spaced samples. A 3-second spring
    /// records ~180 frames, and handing all of them to a client spends its context on a resolution
    /// nobody reads.
    /// </summary>
    private static BmotionSampleDto[] Resample(List<(double Seconds, double Value)> frames)
    {
        if (frames.Count <= SampleCount)
        {
            return [.. frames.Select(f => new BmotionSampleDto { Seconds = Round(f.Seconds), Value = Round(f.Value) })];
        }

        var samples = new BmotionSampleDto[SampleCount];

        for (int i = 0; i < SampleCount; i++)
        {
            // The last sample is pinned to the final frame, so the value the motion rests at is
            // always present rather than being rounded past.
            var index = (int)Math.Round((double)i * (frames.Count - 1) / (SampleCount - 1));
            var frame = frames[index];

            samples[i] = new BmotionSampleDto { Seconds = Round(frame.Seconds), Value = Round(frame.Value) };
        }

        return samples;
    }

    /// <summary>
    /// Draws the motion as a row of block characters, scaled so the target sits near the top and any
    /// overshoot is visible above it. It is the fastest way for a reader - person or model - to tell
    /// a critically damped spring from a bouncy one without reading two dozen numbers.
    /// </summary>
    private static string Sparkline(double[] values, double from, double to)
    {
        const string Blocks = " .:-=+*#%@";

        if (values.Length == 0) return string.Empty;

        var min = Math.Min(values.Min(), Math.Min(from, to));
        var max = Math.Max(values.Max(), Math.Max(from, to));
        var range = max - min;

        var width = Math.Min(values.Length, 48);
        var line = new char[width];

        for (int i = 0; i < width; i++)
        {
            var value = values[(int)Math.Round((double)i * (values.Length - 1) / Math.Max(1, width - 1))];
            var level = range > 0 ? (value - min) / range : 1;

            line[i] = Blocks[Math.Clamp((int)Math.Round(level * (Blocks.Length - 1)), 0, Blocks.Length - 1)];
        }

        return new string(line);
    }

    /// <summary>
    /// States what the measurements add up to. The numbers alone still leave the question an agent
    /// is really asking - is this the feel I asked for? - so the reading names the character of the
    /// motion and, when it is likely to be unwanted, what to change.
    /// </summary>
    private static string Read(string kind, double settle, double overshootPercent, int crossings, double timeTo90)
    {
        var pace = settle switch
        {
            < 0.2 => "very fast",
            < 0.4 => "fast",
            < 0.8 => "unhurried",
            < 1.5 => "slow",
            _ => "very slow"
        };

        var character = (kind, overshootPercent, crossings) switch
        {
            ("Spring", < 0.5, _) => "no overshoot at all - critically damped, so it eases in and stops dead",
            ("Spring", < 5, _) => "a barely visible overshoot - crisp, and calm enough for interface chrome",
            ("Spring", < 15, _) => "a light bounce - the usual choice for buttons, cards and entrances",
            ("Spring", < 30, _) => "a pronounced bounce - playful, and distracting on anything that moves often",
            ("Spring", _, _) => "a large overshoot - it reads as elastic, and is easy to overuse",
            ("Inertia", _, _) => "a decelerating glide, as a flung element comes to rest",
            (_, < 0.5, _) => "a straight run to the target with no overshoot",
            _ => "an easing that carries the element past its target before settling"
        };

        var wobble = crossings > 2
            ? $" It crosses the target {crossings} times before resting, which is visible as a wobble."
            : string.Empty;

        // The gap between "looks arrived" and "actually settled" is the number that surprises people
        // about springs: the tail is real time during which the element is still moving.
        var tail = settle - timeTo90 > 0.25
            ? $" It covers 90% of the distance in {timeTo90:0.00}s but only comes to rest at {settle:0.00}s - " +
              "the tail is the part that reads as sluggish."
            : string.Empty;

        return $"Settles in {settle:0.00}s ({pace}), with {character}.{wobble}{tail}";
    }

    private static string KindOf(BmTransition transition) => transition switch
    {
        BmSpring => "Spring",
        BmInertia => "Inertia",
        _ => "Tween"
    };

    /// <summary>Reads the engine's WAAPI timing bag, whatever concrete dictionary type it used.</summary>
    private static Dictionary<string, object?>? AsDictionary(object value)
    {
        if (value is Dictionary<string, object?> typed) return typed;

        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        }

        return null;
    }

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}

/// <summary>Where the engine plays an animation, as <see cref="BmotionMotionLab"/> measures it.</summary>
public enum BmotionPlaybackPath
{
    /// <summary>
    /// A Web Animation of two keyframes with the curve as its easing: one tween or zero-velocity spring
    /// for every key, over values the browser interpolates exactly as the engine does. Every runtime.
    /// </summary>
    Timeline,

    /// <summary>
    /// The frames the engine's own drivers draw, simplified and played linearly by the browser: what the
    /// frame loop can play but no single curve describes. Every runtime, at the cost of more keyframes.
    /// </summary>
    Recorded,

    /// <summary>The C# per-frame loop: Blazor WebAssembly only, and an instant state change on Blazor Server.</summary>
    FrameLoop,

    /// <summary>Set at once with nothing animated, on every runtime alike: a transition with no length.</summary>
    Instant
}
