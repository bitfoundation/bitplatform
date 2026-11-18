using System.Text;

namespace Bit.Bmotion;

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
