using System.Text;

namespace Bit.Bmotion;

/// <summary>
/// How a prerendered CSS animation honours <c>prefers-reduced-motion</c>. The component resolves it
/// from the same inputs as its runtime reduced-motion check, except that the OS preference itself
/// is left to a media query - nothing can ask the browser while the page is being prerendered.
/// </summary>
internal enum BmotionPrerenderMotionPolicy
{
    /// <summary>Always the full animation.</summary>
    Full,

    /// <summary>Always the reduced animation (transforms snap, opacity and colour still animate).</summary>
    Reduced,

    /// <summary>The full animation, or the reduced one when the OS asks for reduced motion.</summary>
    FollowOs,
}

/// <summary>One CSS animation's timing, in the units the <c>animation</c> shorthand takes.</summary>
internal sealed record BmotionPrerenderTiming(double DurationMs, string Easing, double DelayMs, int Iterations, bool Alternate)
{
    /// <summary>Whether the animation repeats forever (<see cref="Iterations"/> is negative).</summary>
    public bool IsInfinite => Iterations < 0;

    /// <summary>When a finite animation has finished, measured from the moment it was created.</summary>
    public double EndMs => DelayMs + (DurationMs * Iterations);
}

/// <summary>
/// One animated CSS property: its keyframe values, plus the timing it plays with normally and under
/// reduced motion (<c>null</c> = that variant doesn't animate it - the property just sits at rest).
/// </summary>
internal sealed record BmotionPrerenderTrack(string CssProperty, string[] Frames, BmotionPrerenderTiming? Full, BmotionPrerenderTiming? Reduced);

/// <summary>
/// An element's enter animation compiled for a pass with no browser attached: where it comes to
/// rest, and the CSS animation (if any) that gets it there.
/// </summary>
internal sealed class BmotionPrerenderPlan
{
    /// <summary>The engine values the element rests at once the enter animation has played.</summary>
    public required Dictionary<string, object?> Settled { get; init; }

    /// <summary><see cref="Settled"/> as an inline style.</summary>
    public required string SettledStyle { get; init; }

    /// <summary>Whether any of the animated properties repeats forever, so the animation never comes to rest.</summary>
    public required bool IsInfinite { get; init; }

    /// <summary>The properties CSS can animate; everything else just renders at rest.</summary>
    public required IReadOnlyList<BmotionPrerenderTrack> Tracks { get; init; }

    /// <summary>
    /// The custom-property declarations that hook the element up to the library stylesheet: the
    /// animation lists, the easings they share and every keyframe value they reference. Empty when
    /// nothing animates.
    /// <para>
    /// <paramref name="elapsedMs"/> continues the animation from that far in - used when the runtime
    /// takes over an element whose prerendered animation is still playing. Animations that would
    /// already have finished by then are left out.
    /// </para>
    /// </summary>
    public string BuildAnimationDeclarations(BmotionPrerenderMotionPolicy policy, double elapsedMs = 0)
    {
        var full = policy == BmotionPrerenderMotionPolicy.Reduced ? null : Playing(static t => t.Full, elapsedMs);
        var reduced = policy == BmotionPrerenderMotionPolicy.Full ? null : Playing(static t => t.Reduced, elapsedMs);
        if (full is null && reduced is null) return string.Empty;

        var easings = ShareEasings(full, reduced);
        var sb = new StringBuilder();
        // Each policy writes its own variables, and the stylesheet reads only the ones the element's
        // own style attribute declares - never a list inherited from an animated ancestor whose
        // policy differs.
        switch (policy)
        {
            case BmotionPrerenderMotionPolicy.Full:
                AppendDeclaration(sb, BmotionPrerenderCss.AnimationVariable, BuildList(full!, elapsedMs, easings));
                break;
            case BmotionPrerenderMotionPolicy.Reduced:
                AppendDeclaration(sb, BmotionPrerenderCss.AnimationVariable, BuildList(reduced!, elapsedMs, easings));
                break;
            default:
                AppendDeclaration(sb, BmotionPrerenderCss.FollowOsAnimationVariable, full is null ? "none" : BuildList(full, elapsedMs, easings));
                AppendDeclaration(sb, BmotionPrerenderCss.ReducedAnimationVariable, reduced is null ? "none" : BuildList(reduced, elapsedMs, easings));
                break;
        }

        foreach (var (easing, variable) in easings)
            AppendDeclaration(sb, variable, easing);

        foreach (var track in Tracks)
        {
            if (!Plays(full, track) && !Plays(reduced, track)) continue;
            for (int i = 0; i < track.Frames.Length; i++)
                AppendDeclaration(sb, BmotionPrerenderCss.FrameVariable(track.CssProperty, i), track.Frames[i]);
        }
        return sb.ToString();
    }

    /// <summary>The tracks still playing <paramref name="elapsedMs"/> in, with their timing; <c>null</c> when none is.</summary>
    private List<PlayingTrack>? Playing(Func<BmotionPrerenderTrack, BmotionPrerenderTiming?> select, double elapsedMs)
    {
        List<PlayingTrack>? playing = null;
        foreach (var track in Tracks)
        {
            if (select(track) is not { } timing) continue;
            if (!timing.IsInfinite && timing.EndMs <= elapsedMs) continue;
            (playing ??= []).Add(new PlayingTrack(track, timing));
        }
        return playing;
    }

    private static bool Plays(List<PlayingTrack>? playing, BmotionPrerenderTrack track)
        => playing?.Exists(p => ReferenceEquals(p.Track, track)) ?? false;

    /// <summary>
    /// A long easing (a sampled <c>linear()</c> curve) that several animations play with - the
    /// properties sharing a spring, or one property in both motion lists - is declared once in a
    /// custom property and referenced, instead of being repeated in every animation.
    /// </summary>
    private static Dictionary<string, string> ShareEasings(List<PlayingTrack>? full, List<PlayingTrack>? reduced)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var playing in (List<PlayingTrack>?[])[full, reduced])
        {
            if (playing is null) continue;
            foreach (var (_, timing) in playing)
                if (timing.Easing.Length > MinSharedEasingLength)
                    counts[timing.Easing] = counts.GetValueOrDefault(timing.Easing) + 1;
        }

        var shared = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (easing, count) in counts)
            if (count > 1) shared[easing] = BmotionPrerenderCss.EasingVariable(shared.Count);
        return shared;
    }

    // Below this, a reference (var(--bm-e0) plus the declaration it reads) saves nothing.
    private const int MinSharedEasingLength = 32;

    private static string BuildList(List<PlayingTrack> playing, double elapsedMs, Dictionary<string, string> easings)
    {
        var list = new StringBuilder();
        foreach (var (track, timing) in playing)
        {
            if (list.Length > 0) list.Append(',');
            list.Append(BmotionPrerenderCss.KeyframesName(track.CssProperty, track.Frames.Length))
                .Append(' ').Append(BmotionCssFormat.Num(timing.DurationMs)).Append("ms")
                .Append(' ').Append(easings.TryGetValue(timing.Easing, out var variable) ? $"var({variable})" : timing.Easing)
                .Append(' ').Append(BmotionCssFormat.Num(timing.DelayMs - elapsedMs)).Append("ms")
                .Append(' ').Append(timing.IsInfinite ? "infinite" : timing.Iterations.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(timing.Alternate ? " alternate" : " normal")
                // Backwards: the first keyframe holds through the delay (a stagger must not show the
                // resting state first). Not forwards: the resting state is the inline style itself.
                .Append(" backwards");
        }
        return list.ToString();
    }

    private readonly record struct PlayingTrack(BmotionPrerenderTrack Track, BmotionPrerenderTiming Timing);

    // The leading ';' is what the no-script selectors key on, so a variable can never be mistaken
    // for the tail of a longer one (--bm-nj-color vs --bm-nj-background-color).
    private static void AppendDeclaration(StringBuilder sb, string name, string value)
        => sb.Append(';').Append(name).Append(':').Append(value);
}

/// <summary>
/// Compiles an element's enter animation into CSS for the passes where no browser is attached
/// (prerendering and static SSR), and generates the library stylesheet that CSS runs on.
/// <para>
/// A Bmotion renders no element of its own and can't add one (a sibling <c>&lt;style&gt;</c> would
/// break the consumer's <c>:first-child</c>/<c>:nth-child</c> selectors), so everything the element
/// needs travels in its inline style as custom properties: the <c>animation</c> list and each
/// keyframe value. The shipped stylesheet (<c>wwwroot/bit-bmotion.css</c>) holds one generic
/// <c>@keyframes</c> per property and frame count that reads those values back with <c>var()</c>,
/// plus the selectors that switch the animation on. If the stylesheet isn't linked, nothing reads
/// the custom properties and the element simply renders at rest.
/// </para>
/// </summary>
internal static class BmotionPrerenderCss
{
    /// <summary>The most keyframes one prerendered property can have (the stylesheet's upper bound).</summary>
    internal const int MaxFrames = 6;

    /// <summary>Holds the element's <c>animation</c> list when its motion policy is fixed.</summary>
    internal const string AnimationVariable = "--bm-a";

    /// <summary>Holds the full <c>animation</c> list of an element that follows the OS motion preference.</summary>
    internal const string FollowOsAnimationVariable = "--bm-af";

    /// <summary>Holds the list that element plays under <c>prefers-reduced-motion: reduce</c>.</summary>
    internal const string ReducedAnimationVariable = "--bm-ar";

    /// <summary>
    /// The properties the stylesheet has keyframes for: everything <see cref="BmProps"/> can animate
    /// except <c>d</c> (an SVG attribute, not a style) and <c>offset-path</c> (it doesn't interpolate).
    /// Path drawing lands on <c>stroke-dasharray</c>/<c>stroke-dashoffset</c>, as in the engine.
    /// </summary>
    internal static readonly string[] AnimatableProperties =
    [
        "transform", "opacity",
        "background-color", "color", "border-color", "outline-color", "fill", "stroke",
        "width", "height", "border-radius", "box-shadow", "filter",
        "stroke-dasharray", "stroke-dashoffset",
        "top", "left", "right", "bottom", "margin", "padding", "gap",
        "letter-spacing", "line-height", "font-size",
        "clip-path", "background-position", "background-size", "offset-distance",
    ];

    private static readonly HashSet<string> _animatable = new(AnimatableProperties, StringComparer.Ordinal);

    internal static string KeyframesName(string cssProperty, int frameCount) => $"bm-{cssProperty}-{frameCount}";

    internal static string FrameVariable(string cssProperty, int frame) => $"--bm-{cssProperty}-{frame}";

    /// <summary>Carries an easing several of an element's animations share.</summary>
    internal static string EasingVariable(int index) => $"--bm-e{index}";

    /// <summary>Carries the value a property takes when scripting is off (see <see cref="BuildNoScriptDeclarations"/>).</summary>
    internal static string NoScriptVariable(string cssProperty) => $"--bm-nj-{cssProperty}";

    /// <summary>
    /// The full text of <c>wwwroot/bit-bmotion.css</c>. The file is generated from this - a test
    /// fails when they drift - so change the stylesheet here and regenerate it.
    /// </summary>
    internal static string BuildStylesheet()
    {
        var sb = new StringBuilder();
        sb.Append("/* Bit.Bmotion prerender stylesheet. Lets elements prerendered with BmPrerenderMode.Animate play their enter\n");
        sb.Append("   animation before the runtime loads. Generated by BmotionPrerenderCss.BuildStylesheet - edit that, not this file. */\n");

        // Keyed on the name and colon only, with no leading ';': once the engine writes a style
        // through the CSSOM, the browser reserializes the attribute as "name: value" - and the
        // animation has to keep playing. Neither name is the tail of another variable's.
        var hook = $"[style*=\"{AnimationVariable}:\"]";
        var followOsHook = $"[style*=\"{FollowOsAnimationVariable}:\"]";
        sb.Append(hook).Append("{animation:var(").Append(AnimationVariable).Append(")}\n");
        sb.Append(followOsHook).Append("{animation:var(").Append(FollowOsAnimationVariable).Append(")}\n");
        sb.Append("@media (prefers-reduced-motion:reduce){").Append(followOsHook)
          .Append("{animation:var(").Append(ReducedAnimationVariable).Append(")}}\n");

        // Without scripting nothing can ever reveal a WhileInView element, so show it as it would be in view.
        sb.Append("@media (scripting:none){");
        foreach (var property in AnimatableProperties)
        {
            var variable = NoScriptVariable(property);
            sb.Append("[style*=\";").Append(variable).Append(":\"]{")
              .Append(property).Append(":var(").Append(variable).Append(")!important}");
        }
        sb.Append("}\n");

        foreach (var property in AnimatableProperties)
        {
            for (int n = 2; n <= MaxFrames; n++)
            {
                sb.Append("@keyframes ").Append(KeyframesName(property, n)).Append('{');
                for (int i = 0; i < n; i++)
                {
                    sb.Append(BmotionCssFormat.Num(i * 100.0 / (n - 1), "0.####")).Append("%{")
                      .Append(property).Append(":var(").Append(FrameVariable(property, i)).Append(")}");
                }
                sb.Append("}\n");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Compiles the animation from <paramref name="initial"/> to <paramref name="target"/>. Returns
    /// <c>null</c> when the element can't be prerendered at rest at all (the target morphs an SVG
    /// path's <c>d</c>, which only the browser bridge can write); otherwise a plan whose tracks hold
    /// whatever CSS can express - a property it can't is simply left at rest.
    /// </summary>
    /// <param name="initial">The engine values the element starts from (its <c>Initial</c> target), if any.</param>
    /// <param name="target">The engine values of the enter animation's target.</param>
    /// <param name="full">The transition the enter animation plays with (stagger delay included).</param>
    /// <param name="reduced">The reduced-motion variant of it, or <c>null</c> when it never applies.</param>
    /// <param name="template">The element's transform template, applied to every composed transform.</param>
    internal static BmotionPrerenderPlan? Compile(
        Dictionary<string, object?>? initial,
        Dictionary<string, object?> target,
        BmotionTransitionConfig? full,
        BmotionTransitionConfig? reduced,
        BmTransformTemplate? template)
    {
        foreach (var key in target.Keys)
            if (string.Equals(key, "d", StringComparison.OrdinalIgnoreCase)) return null;

        var startValues = initial is null ? new Dictionary<string, object?>() : FirstFrames(initial);
        var start = new BmotionElementAnimationState { TransformTemplate = template };
        start.SetInstant(startValues);

        var settled = new Dictionary<string, object?>(startValues);
        var numericFrames = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        var stringFrames = new Dictionary<string, string[]>(StringComparer.Ordinal);
        bool infinite = false;
        var fullConfig = full ?? new BmotionTransitionConfig();

        foreach (var (key, raw) in target)
        {
            if (raw is null) continue;
            var config = ConfigFor(fullConfig, key);
            infinite |= config.IsInfiniteRepeat;
            if (TryNumericFrames(start, key, raw, out var numbers))
            {
                numericFrames[key] = numbers;
                settled[key] = numbers[TerminalFrame(config, numbers.Length)];
            }
            else if (TryStringFrames(start, key, raw, out var strings))
            {
                if (strings.Length >= 2) stringFrames[key] = strings;
                settled[key] = strings[TerminalFrame(config, strings.Length)];
            }
            // Anything else is a value the engine skips as well.
        }

        var tracks = new List<BmotionPrerenderTrack>();
        AddTransformTrack(tracks, start, target, numericFrames, fullConfig, reduced);
        AddPathTracks(tracks, start, numericFrames, fullConfig, reduced);

        foreach (var (key, frames) in numericFrames)
        {
            if (BmotionTransformComposer.IsTransformProp(key) || IsPathKey(key)) continue;
            AddTrack(tracks, BmProps.ToDashCase(key), [key], frames.Select(BmotionCssFormat.Num).ToArray(), fullConfig, reduced);
        }
        foreach (var (key, frames) in stringFrames)
            AddTrack(tracks, BmProps.ToDashCase(key), [key], frames, fullConfig, reduced);

        return new BmotionPrerenderPlan
        {
            Settled = settled,
            SettledStyle = StyleOf(settled, template),
            IsInfinite = infinite,
            Tracks = tracks,
        };
    }

    /// <summary>
    /// The custom properties that show an element as <paramref name="layer"/> (its WhileInView
    /// target) would leave it, read by the stylesheet only when scripting is off - the one case where
    /// nothing will ever reveal it. Covers the properties the layer touches.
    /// </summary>
    internal static string BuildNoScriptDeclarations(
        Dictionary<string, object?> rest, Dictionary<string, object?> layer, BmTransformTemplate? template)
    {
        var layerValues = new Dictionary<string, object?>();
        foreach (var (key, raw) in layer)
        {
            switch (raw)
            {
                case double[] { Length: > 0 } numbers when double.IsFinite(numbers[^1]): layerValues[key] = numbers[^1]; break;
                case string[] { Length: > 0 } strings: layerValues[key] = strings[^1]; break;
                case null or double[] or string[]: break;
                default: layerValues[key] = raw; break;
            }
        }
        if (layerValues.Count == 0) return string.Empty;

        var state = new BmotionElementAnimationState { TransformTemplate = template };
        state.SetInstant(rest);
        state.SetInstant(layerValues);
        var styles = state.BuildSnapshotStyles();
        if (styles is null) return string.Empty;

        var touched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in layerValues.Keys)
        {
            if (BmotionTransformComposer.IsTransformProp(key)) touched.Add("transform");
            else if (IsPathKey(key)) { touched.Add("strokeDasharray"); touched.Add("strokeDashoffset"); }
            else touched.Add(key);
        }

        var sb = new StringBuilder();
        foreach (var (key, value) in styles)
        {
            if (!touched.Contains(key)) continue;
            var property = BmProps.ToDashCase(key);
            if (!_animatable.Contains(property)) continue;
            sb.Append(';').Append(NoScriptVariable(property)).Append(':').Append(value);
        }
        return sb.ToString();
    }

    /// <summary>The engine values a target leaves an element at when applied instantly (each sequence's first frame).</summary>
    internal static Dictionary<string, object?> FirstFrames(Dictionary<string, object?> values)
    {
        var result = new Dictionary<string, object?>();
        foreach (var (key, raw) in values)
        {
            switch (raw)
            {
                // A leading Bm.Current wildcard has no value until the element exists.
                case double[] { Length: > 0 } numbers:
                    if (double.IsFinite(numbers[0])) result[key] = numbers[0];
                    break;
                case string[] { Length: > 0 } strings:
                    result[key] = strings[0];
                    break;
                case null or double[] or string[]:
                    break;
                default:
                    result[key] = raw;
                    break;
            }
        }
        return result;
    }

    /// <summary>Renders engine values as an inline style, the way the engine would write them.</summary>
    internal static string StyleOf(Dictionary<string, object?> values, BmTransformTemplate? template)
    {
        var state = new BmotionElementAnimationState { TransformTemplate = template };
        state.SetInstant(values);
        var styles = state.BuildSnapshotStyles();
        if (styles is null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var (key, value) in styles)
        {
            // `d` is an SVG attribute the browser bridge sets; the element's own `d` renders it here.
            if (key == "d") continue;
            sb.Append(BmProps.ToDashCase(key)).Append(':').Append(value).Append(';');
        }
        return sb.ToString();
    }

    private static BmotionTransitionConfig ConfigFor(BmotionTransitionConfig config, string key)
        => config.Properties?.GetValueOrDefault(key) ?? config;

    private static bool IsPathKey(string key) => key is "pathLength" or "pathOffset" or "pathSpacing";

    /// <summary>
    /// The frame an animation comes to rest on - mirrors the drivers' <c>Complete</c>: a mirrored
    /// animation with an even number of passes ends where it started, and a reversed one with any
    /// repeat ends on its first frame.
    /// </summary>
    private static int TerminalFrame(BmotionTransitionConfig config, int frameCount)
    {
        if (frameCount < 2 || config.IsInfiniteRepeat) return frameCount - 1;
        return config.RepeatType switch
        {
            BmRepeatType.Mirror => (config.Repeat + 1) % 2 == 0 ? 0 : frameCount - 1,
            BmRepeatType.Reverse => config.Repeat == 0 ? frameCount - 1 : 0,
            _ => frameCount - 1,
        };
    }

    private static bool TryNumericFrames(BmotionElementAnimationState start, string key, object raw, out double[] frames)
    {
        bool isTransform = BmotionTransformComposer.IsTransformProp(key);
        double from = isTransform
            ? start.Transforms.GetValueOrDefault(key, BmotionElementAnimationState.DefaultTransformValue(key))
            : start.NumericValues.GetValueOrDefault(key, BmotionElementAnimationState.DefaultNumericValue(key));

        switch (raw)
        {
            case double d: frames = [from, d]; break;
            case int i: frames = [from, i]; break;
            case float f: frames = [from, f]; break;
            case long l: frames = [from, l]; break;
            case double[] { Length: 1 } one: frames = [from, one[0]]; break;
            case double[] { Length: >= 2 } many:
                frames = (double[])many.Clone();
                // Bm.Current wildcards resolve to the value the element starts from, as the engine does.
                for (int i = 0; i < frames.Length; i++)
                    if (double.IsNaN(frames[i])) frames[i] = from;
                break;
            default:
                frames = [];
                return false;
        }
        foreach (var value in frames)
            if (!double.IsFinite(value)) return false;
        return true;
    }

    private static bool TryStringFrames(BmotionElementAnimationState start, string key, object raw, out string[] frames)
    {
        switch (raw)
        {
            case string s:
                // Without a known starting value CSS has nothing to animate from: the property just lands.
                frames = start.StringValues.TryGetValue(key, out var from) ? [from, s] : [s];
                return true;
            case string[] { Length: > 0 } many:
                frames = (string[])many.Clone();
                return true;
            default:
                frames = [];
                return false;
        }
    }

    private static void AddTransformTrack(
        List<BmotionPrerenderTrack> tracks,
        BmotionElementAnimationState start,
        Dictionary<string, object?> target,
        Dictionary<string, double[]> numericFrames,
        BmotionTransitionConfig full,
        BmotionTransitionConfig? reduced)
    {
        var keys = numericFrames.Keys.Where(BmotionTransformComposer.IsTransformProp).ToArray();
        if (keys.Length == 0) return;
        // An arc bends x and y along a curve; keyframes between the endpoints would draw a straight line.
        if (full.Path is not null && BmotionArcTargets.Applies(target)) return;

        int frameCount = numericFrames[keys[0]].Length;
        var frames = new string[frameCount];
        for (int j = 0; j < frameCount; j++)
        {
            var components = new Dictionary<string, double>(start.Transforms, StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                var keyFrames = numericFrames[key];
                // transform is one CSS property: every component has to move on the same frames.
                if (keyFrames.Length != frameCount) return;
                components[key] = keyFrames[j];
            }
            var composed = start.ComposeTransform(components);
            frames[j] = string.IsNullOrEmpty(composed) ? "none" : composed;
        }
        AddTrack(tracks, "transform", keys, frames, full, reduced);
    }

    private static void AddPathTracks(
        List<BmotionPrerenderTrack> tracks,
        BmotionElementAnimationState start,
        Dictionary<string, double[]> numericFrames,
        BmotionTransitionConfig full,
        BmotionTransitionConfig? reduced)
    {
        var keys = numericFrames.Keys.Where(IsPathKey).ToArray();
        if (keys.Length == 0) return;

        int frameCount = numericFrames[keys[0]].Length;
        if (keys.Any(k => numericFrames[k].Length != frameCount)) return;

        double At(string key, int frame, double fallback)
            => numericFrames.TryGetValue(key, out var f) ? f[frame] : start.NumericValues.GetValueOrDefault(key, fallback);

        var dashArray = new string[frameCount];
        var dashOffset = new string[frameCount];
        for (int j = 0; j < frameCount; j++)
        {
            // Same arithmetic as BmotionElementAnimationState.BuildSnapshotStyles.
            double length = Math.Clamp(At("pathLength", j, 1.0), 0, 1);
            double spacing = At("pathSpacing", j, 1.0);
            double offset = At("pathOffset", j, 0.0);
            dashArray[j] = BmotionCssFormat.Num(length) + " " + BmotionCssFormat.Num(spacing);
            dashOffset[j] = BmotionCssFormat.Num(1 - length - offset);
        }
        AddTrack(tracks, "stroke-dasharray", keys, dashArray, full, reduced);
        AddTrack(tracks, "stroke-dashoffset", keys, dashOffset, full, reduced);
    }

    /// <summary>
    /// Adds a track for <paramref name="cssProperty"/>, driven by <paramref name="keys"/>. One CSS
    /// animation has one timing, so every key behind it must share its transition.
    /// </summary>
    private static void AddTrack(
        List<BmotionPrerenderTrack> tracks,
        string cssProperty,
        string[] keys,
        string[] frames,
        BmotionTransitionConfig full,
        BmotionTransitionConfig? reduced)
    {
        if (!_animatable.Contains(cssProperty)) return;
        if (frames.Length < 2 || frames.Length > MaxFrames) return;
        if (frames.All(f => f == frames[0])) return;
        if (SharedConfig(full, keys) is not { } fullConfig) return;

        var fullTiming = BuildTiming(fullConfig, frames.Length);
        var reducedTiming = reduced is not null && SharedConfig(reduced, keys) is { } reducedConfig
            ? BuildTiming(reducedConfig, frames.Length)
            : null;
        if (fullTiming is null && reducedTiming is null) return;

        tracks.Add(new BmotionPrerenderTrack(cssProperty, frames, fullTiming, reducedTiming));
    }

    private static BmotionTransitionConfig? SharedConfig(BmotionTransitionConfig config, string[] keys)
    {
        var shared = ConfigFor(config, keys[0]);
        for (int i = 1; i < keys.Length; i++)
            if (!ReferenceEquals(ConfigFor(config, keys[i]), shared)) return null;
        return shared;
    }

    /// <summary>The CSS timing for one transition, or <c>null</c> when CSS can't play it faithfully.</summary>
    internal static BmotionPrerenderTiming? BuildTiming(BmotionTransitionConfig config, int frameCount)
    {
        if (!BmotionBrowserTiming.CanPlay(config)) return null;

        Func<double, double>? ease = null;
        if (config.Type == BmotionTransitionType.Spring)
        {
            // A spring's curve covers the whole move; a keyframe sequence isn't a spring's to shape.
            if (frameCount != 2) return null;
        }
        else
        {
            // CSS eases every keyframe segment alike, which is also what a single ease means to the
            // keyframes driver; differing per-segment eases can't be expressed.
            if (config.Eases is { Length: > 1 } && frameCount > 2) return null;
            ease = BmEaseFunctions.GetSegmentEases(config, frameCount - 1)[0];
        }

        if (BmotionBrowserTiming.SampleProgress(config, ease) is not { } sampled) return null;
        // Nothing can ask the browser about linear() support while prerendering, so it is assumed
        // (springs and curves CSS has no keyword for need it), and an exact keyword is used
        // wherever one exists.
        var easing = BmotionBrowserTiming.ChooseEasing(config, sampled.Samples, linearSupported: true, preferKeyword: true);
        if (easing is null) return null;

        return new BmotionPrerenderTiming(
            sampled.DurationMs,
            easing,
            config.Delay * 1000,
            config.IsInfiniteRepeat ? -1 : config.Repeat + 1,
            config.RepeatType == BmRepeatType.Mirror);
    }
}
