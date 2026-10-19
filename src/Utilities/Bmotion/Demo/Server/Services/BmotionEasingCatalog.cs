using System.Collections.Frozen;
using Bit.Bmotion.Demo.Server.Dtos;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Every <see cref="BmEase"/> preset, with the shape of its curve measured rather than described.
/// <para>
/// "BackOut" and "Anticipate" mean nothing to a model that has never seen them plotted, and the
/// difference between QuadOut and ExpoOut is the entire question when choosing one. So each preset
/// here is run through the real easing implementation - a one-second tween from 0 to 1, sampled at
/// eleven points - which turns the choice into numbers an agent can reason about, and reveals the
/// property that actually decides whether a preset is usable in a given place: whether it leaves
/// the 0-1 range, and therefore whether the element overshoots.
/// </para>
/// </summary>
public static class BmotionEasingCatalog
{
    private static readonly Lazy<Task<BmotionEasingDto[]>> _easings = new(BuildAsync);

    // What each family feels like. The numbers come from the library; these words are the part a
    // measurement cannot supply - when to reach for it.
    private static readonly FrozenDictionary<string, string> _feel = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Linear"] = "No acceleration at all. Right for continuous motion - a spinner, a marquee, a progress bar - and wrong for anything that starts or stops, where it reads as mechanical.",
        ["Sine"] = "The gentlest curve there is. Good for slow, ambient motion that should not draw attention.",
        ["Quad"] = "A mild acceleration. The safe default when Out is not quite enough.",
        ["Cubic"] = "The standard interface curve - what In, Out and InOut approximate.",
        ["Quart"] = "A firm acceleration: slow to leave, quick to arrive.",
        ["Quint"] = "Sharper still. The element barely moves, then rushes.",
        ["Expo"] = "The most extreme non-overshooting curve. Dramatic entrances; too much for anything that repeats.",
        ["Circ"] = "Flat then suddenly steep, like the edge of a circle. Mechanical and precise.",
        ["Back"] = "Pulls back before going, or overshoots on arrival. The cheapest way to make an element feel physical without a spring.",
        ["Elastic"] = "Oscillates around the target like a plucked string. Playful, and distracting on anything the user sees often.",
        ["Bounce"] = "Lands, bounces, lands again. Reads as a falling object; almost never right for interface chrome.",
        ["Anticipate"] = "A small wind-up before the move. Signals intent, which makes the motion feel deliberate.",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Every easing preset, with its measured curve.</summary>
    public static Task<BmotionEasingDto[]> GetAsync() => _easings.Value;

    private static async Task<BmotionEasingDto[]> BuildAsync()
    {
        var easings = new List<BmotionEasingDto>();

        foreach (var ease in Enum.GetValues<BmEase>())
        {
            double[] curve;

            try
            {
                curve = await BmotionMotionLab.SampleEaseAsync(ease);
            }
            catch (Exception)
            {
                // Same reasoning as BmotionPropertyCatalog: this list is built once for the life of
                // the process. A preset that would not sample is left out rather than described with
                // a curve nobody measured, and every other preset still answers.
                continue;
            }

            var name = ease.ToString();

            easings.Add(new BmotionEasingDto
            {
                Name = name,
                Direction = DirectionOf(name),
                Family = FamilyOf(name),
                Feel = _feel.GetValueOrDefault(FamilyOf(name), "A named easing curve.") + " " + DirectionNote(DirectionOf(name)),
                Curve = [.. curve.Select(value => Math.Round(value, 4))],
                Sparkline = Sparkline(curve),
                // A curve that leaves 0-1 moves the element past its target and back. That is the
                // single fact which decides whether a preset can be used on something that must not
                // appear to move beyond its bounds - a drawer, a modal, anything with a hard edge.
                Overshoots = curve.Any(value => value < -0.001 || value > 1.001)
            });
        }

        return [.. easings];
    }

    /// <summary>
    /// Draws the curve as text. Read left to right it is time; the height is progress toward the
    /// target, with the top of the range being the furthest the curve travels - so an overshooting
    /// preset visibly rises above where it settles.
    /// </summary>
    private static string Sparkline(double[] curve)
    {
        const string Blocks = "_.-=+*#%@";

        if (curve.Length == 0) return string.Empty;

        var min = Math.Min(0, curve.Min());
        var max = Math.Max(1, curve.Max());
        var range = max - min;

        return string.Concat(curve.Select(value =>
        {
            var level = range > 0 ? (value - min) / range : 0;

            return Blocks[Math.Clamp((int)Math.Round(level * (Blocks.Length - 1)), 0, Blocks.Length - 1)];
        }));
    }

    private static string DirectionOf(string name)
    {
        if (name == "Linear") return "Linear";
        if (name.EndsWith("InOut", StringComparison.Ordinal)) return "InOut";
        if (name.EndsWith("Out", StringComparison.Ordinal)) return "Out";
        if (name.EndsWith("In", StringComparison.Ordinal)) return "In";

        return "Custom";
    }

    private static string FamilyOf(string name)
    {
        if (name == "Linear" || name == "Anticipate") return name;

        var family = name;

        foreach (var suffix in new[] { "InOut", "Out", "In" })
        {
            if (family.EndsWith(suffix, StringComparison.Ordinal))
            {
                family = family[..^suffix.Length];
                break;
            }
        }

        // The bare In / Out / InOut members are the library's default cubic bezier curves.
        return family.Length == 0 ? "Cubic" : family;
    }

    private static string DirectionNote(string direction) => direction switch
    {
        "In" => "Accelerating: slow to start, fastest at the end - use it for something leaving the screen.",
        "Out" => "Decelerating: fastest at the start, easing into the target - the right default for something arriving.",
        "InOut" => "Both ends eased - the right choice when the element starts and stops on screen.",
        "Linear" => string.Empty,
        _ => "A shaped curve rather than a plain direction."
    };
}
