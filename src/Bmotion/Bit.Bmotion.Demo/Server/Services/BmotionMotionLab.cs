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
///     <b>Will it play on Blazor Server?</b> Only animations the browser compositor can own do; the
///     rest need the per-frame loop and collapse to an instant change. The rule set behind that
///     decision is long and lives in the engine. <see cref="AnalyzePlaybackAsync"/> asks the engine
///     instead of restating it: it starts the animation and watches which path the engine takes.
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
    /// Starts a real animation on the engine and reports which playback path it took - which is what
    /// decides whether it survives on Blazor Server.
    /// </summary>
    /// <param name="properties">The animated properties, by their <c>Bm.To(...)</c> names.</param>
    /// <param name="spec">The transition spec, e.g. "tween(0.4, InOut)".</param>
    public static async Task<BmotionPlaybackDto> AnalyzePlaybackAsync(IReadOnlyCollection<string> properties, string? spec)
    {
        var parsed = BmotionTransitionSpec.Parse(spec);

        if (parsed.Transition is null)
        {
            return new BmotionPlaybackDto
            {
                Properties = [.. properties],
                Transition = spec ?? string.Empty,
                Path = "Not analysed",
                WorksOnBlazorServer = false,
                Reason = parsed.Error ?? "The transition could not be read.",
                Error = parsed.Error
            };
        }

        var props = BmotionPropertyCatalog.BuildTarget(properties, out var unknown);

        var interop = new HeadlessBmotionInterop();
        await using var engine = new BmotionAnimationEngine(interop);
        var service = new BmotionAnimateService(engine, interop);

        var controls = await service.AnimateAsync(".bmotion-target", props, parsed.Transition);

        // The compositor decision is taken inside the animation's first few asynchronous steps, and
        // the frame-loop path needs frames to finish at all. Pump a bounded number of both so the
        // engine reaches its decision either way; the verdict is read from the interop afterwards.
        var clockMs = 0.0;
        var completion = controls.WhenCompleteAsync();

        for (int frame = 0; frame < 240 && completion.IsCompleted is false; frame++)
        {
            engine.ComputeFrame(clockMs);
            clockMs += FrameMs;

            // Lets the engine's own awaits run: without this the compositor hand-off, which is
            // asynchronous, could still be in flight when the verdict is read.
            if (frame % 8 == 0) await Task.Yield();
        }

        controls.Stop();

        var offloaded = interop.WaapiCalls.Count > 0;
        var timing = offloaded ? AsDictionary(interop.WaapiCalls[0].Timing) : null;

        var reasons = BmotionPropertyCatalog.ExplainPlayback(properties, parsed.Transition, offloaded);

        return new BmotionPlaybackDto
        {
            Properties = [.. properties],
            Transition = parsed.Canonical,
            Path = offloaded ? "Compositor (Web Animations API)" : "C# frame loop (requestAnimationFrame)",
            WorksOnBlazorServer = offloaded,
            Reason = reasons.Reason + (unknown.Length > 0
                ? $" Ignored, because Bm.To has no such argument: {string.Join(", ", unknown)}."
                : string.Empty),
            CompositorDurationMs = timing is not null && timing.TryGetValue("duration", out var duration)
                ? Convert.ToDouble(duration, CultureInfo.InvariantCulture)
                : null,
            CompositorEasing = timing is not null && timing.TryGetValue("easing", out var easing)
                ? easing?.ToString()
                : null,
            HowToOffload = offloaded ? null : reasons.Remedies
        };
    }

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
        catch (TimeoutException)
        {
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
