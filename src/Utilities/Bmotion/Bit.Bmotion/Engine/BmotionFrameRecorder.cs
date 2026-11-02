namespace Bit.Bmotion;

/// <summary>
/// Records an animation frame by frame by running the frame loop's own drivers against a fork of the element's
/// state, so the browser can play exactly what the frame loop would have drawn. This is the "sampled" browser
/// animation: per-property transitions, keyframes with per-segment easing, springs carrying velocity, inertia,
/// arcs, gradients - anything the frame loop can play, on runtimes that have no frame loop at all.
/// </summary>
internal static class BmotionFrameRecorder
{
    /// <summary>The interval between recorded frames (60 Hz); the browser interpolates linearly between them.</summary>
    internal const double StepMs = 1000.0 / 60;

    /// <summary>The longest stretch recorded. A longer animation stays on the frame loop.</summary>
    internal const double MaxMs = 10_000;

    /// <summary>One recorded animation.</summary>
    internal sealed class Recording
    {
        /// <summary>Per numeric key (transform components, opacity, path drawing), its value on every frame.</summary>
        public required Dictionary<string, double[]> Numeric { get; init; }

        /// <summary>Per string key (colours, lengths, filters, ...), its value on every frame.</summary>
        public required Dictionary<string, string[]> Strings { get; init; }

        /// <summary>How many frames it has.</summary>
        public required int FrameCount { get; init; }

        /// <summary>The interval between its frames.</summary>
        public required double Step { get; init; }

        /// <summary>How long the recording plays.</summary>
        public double DurationMs => (FrameCount - 1) * Step;

        /// <summary>
        /// Frame <paramref name="index"/> as the CSS the frame loop would write - the browser's keyframe. Composed on
        /// demand, so only the frames actually sent pay for the number formatting.
        /// </summary>
        public Dictionary<string, string> CssAt(int index) => Compose(index);

        /// <summary>Composes a frame's CSS (see <see cref="CssAt"/>).</summary>
        public required Func<int, Dictionary<string, string>> Compose { get; init; }
    }

    /// <summary>
    /// The frames of <paramref name="recording"/> a browser needs to reproduce it by interpolating linearly between
    /// them: the first and last, and every frame the straight line from the previous kept frame can't reach within a
    /// fraction of a pixel (or degree, or opacity step). A string value is only skipped over while it holds still -
    /// its interpolation isn't the engine's to predict. Indices, ascending.
    /// </summary>
    internal static List<int> Simplify(Recording recording)
    {
        int n = recording.FrameCount;
        var kept = new List<int> { 0 };
        if (n <= 2)
        {
            if (n == 2) kept.Add(1);
            return kept;
        }

        var numeric = recording.Numeric.Select(kv => (Frames: kv.Value, Tolerance: ToleranceOf(kv.Key))).ToArray();
        var strings = recording.Strings.Values.ToArray();
        int from = 0;
        while (from < n - 1)
        {
            int to = from + 1;
            while (to + 1 < n && Reproducible(from, to + 1)) to++;
            kept.Add(to);
            from = to;
        }
        return kept;

        bool Reproducible(int a, int b)
        {
            foreach (var frames in strings)
                for (int i = a + 1; i <= b; i++)
                    if (!string.Equals(frames[i], frames[a], StringComparison.Ordinal)) return false;
            foreach (var (frames, tolerance) in numeric)
            {
                double fa = frames[a], fb = frames[b];
                for (int i = a + 1; i < b; i++)
                {
                    double line = fa + ((fb - fa) * (i - a) / (b - a));
                    if (Math.Abs(frames[i] - line) > tolerance) return false;
                }
            }
            return true;
        }
    }

    // How far a frame may stray from the straight line between kept frames without anyone seeing it.
    private static double ToleranceOf(string key) => key switch
    {
        "x" or "y" or "z" or "perspective" => 0.1,                         // px
        "rotate" or "rotateX" or "rotateY" or "rotateZ" or "skewX" or "skewY" => 0.1, // deg
        "opacity" => 0.002,
        _ => 0.001,                                                       // scale, path drawing
    };

    /// <summary>
    /// Records <paramref name="values"/> animated with <paramref name="transition"/> from the element's current state,
    /// followed by <paramref name="holdMs"/> of standing still (a repeat delay). <c>null</c> when nothing animates
    /// or the animation outlasts <see cref="MaxMs"/>.
    /// </summary>
    internal static Recording? Record(
        BmotionElementAnimationState source, Dictionary<string, object?> values,
        BmotionTransitionConfig? transition, double holdMs = 0)
        => Record(source, values, transition, holdMs, StepFor(values, transition));

    /// <summary>
    /// The recording interval: 60 Hz, or - for a tween every key shares - the nearest interval at or under it that
    /// divides the tween's length exactly, so the last frame lands on its end rather than up to a frame past it
    /// (which a loop would repeat every cycle).
    /// </summary>
    internal static double StepFor(Dictionary<string, object?> values, BmotionTransitionConfig? transition)
    {
        if (transition is not { Type: BmotionTransitionType.Tween, Duration: > 0 } tween) return StepMs;
        if (tween.Properties is { Count: > 0 } perKey && values.Keys.Any(perKey.ContainsKey)) return StepMs;
        double active = (tween.Duration * (tween.Repeat + 1) + (tween.RepeatDelay * tween.Repeat)) * 1000;
        if (!double.IsFinite(active) || active <= 0) return StepMs;
        return active / Math.Ceiling(active / StepMs - 1e-9);
    }

    internal static Recording? Record(
        BmotionElementAnimationState source, Dictionary<string, object?> values,
        BmotionTransitionConfig? transition, double holdMs, double stepMs)
    {
        var fork = source.ForkForRecording();
        fork.AnimateTo(values, transition);
        // Advance the drivers only: the CSS is composed afterwards, for the frames that are kept.
        fork.Silent = true;

        // Every key the animation moves: the ones it names, and the ones a coupled driver claims (an arc's rotate).
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values) if (value is not null) keys.Add(key);
        foreach (var key in fork.ActiveDriverKeys) keys.Add(key);

        var numeric = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        var strings = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        int count = 0;
        int maxFrames = (int)(MaxMs / stepMs) + 1;

        // Each tick's time from its index, so no rounding accumulates over a long recording - nudged a hair late, so
        // the tick meant to land on the end isn't rounded to just short of it.
        for (int i = 0; ; i++)
        {
            if (count >= maxFrames) return null;
            fork.Tick((i * stepMs) + 1e-6);
            Capture();
            if (!fork.HasActiveAnimations) break;
        }

        // A repeat delay is the last frame held; an animation that settled on its first tick (an instant change)
        // still gets the two frames a Web Animation needs.
        for (int held = 1; held * stepMs <= holdMs + 1e-6 || count < 2; held++)
        {
            if (count >= maxFrames) return null;
            Capture();
        }

        // A track lines up with the frames only if the key had a value on every one of them.
        var numericTracks = numeric.Where(kv => kv.Value.Count == count)
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        var stringTracks = strings.Where(kv => kv.Value.Count == count)
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.Ordinal);
        // The components no track moves are what they are at the end; the tracks overlay the rest frame by frame.
        var restingTransforms = new Dictionary<string, double>(fork.Transforms, StringComparer.OrdinalIgnoreCase);
        var restingNumbers = new Dictionary<string, double>(fork.NumericValues, StringComparer.Ordinal);

        return new Recording
        {
            Numeric = numericTracks,
            Strings = stringTracks,
            FrameCount = count,
            Step = stepMs,
            Compose = Compose,
        };

        // What Tick would have written for frame i: the full transform (template applied), opacity and other numbers,
        // the stroke-dash pair path drawing composes, and the string values.
        Dictionary<string, string> Compose(int i)
        {
            var css = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, double>? transforms = null;
            bool path = false;
            foreach (var (key, frames) in numericTracks)
            {
                if (BmotionTransformComposer.IsTransformProp(key))
                    (transforms ??= new Dictionary<string, double>(restingTransforms, StringComparer.OrdinalIgnoreCase))[key] = frames[i];
                else if (key is "pathLength" or "pathOffset" or "pathSpacing") path = true;
                else css[key] = BmotionCssFormat.Num(frames[i]);
            }
            if (transforms is not null) css["transform"] = fork.ComposeTransform(transforms);
            if (path)
            {
                double Path(string key, double fallback)
                    => numericTracks.TryGetValue(key, out var f) ? f[i] : restingNumbers.GetValueOrDefault(key, fallback);
                double len = Math.Clamp(Path("pathLength", 1), 0, 1);
                css["strokeDasharray"] = BmotionCssFormat.Num(len) + " " + BmotionCssFormat.Num(Path("pathSpacing", 1));
                css["strokeDashoffset"] = BmotionCssFormat.Num(1 - len - Path("pathOffset", 0));
            }
            foreach (var (key, frames) in stringTracks) css[key] = frames[i];
            return css;
        }

        void Capture()
        {
            count++;
            foreach (var key in keys)
            {
                switch (fork.CurrentValue(key))
                {
                    case double number:
                        (numeric.TryGetValue(key, out var n) ? n : numeric[key] = new List<double>()).Add(number);
                        break;
                    case string text:
                        (strings.TryGetValue(key, out var s) ? s : strings[key] = new List<string>()).Add(text);
                        break;
                }
            }
        }
    }
}
