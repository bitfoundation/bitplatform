using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// Reads the keyframes the engine handed the (fake) browser the way a browser plays them: a recorded animation sends
/// only the frames linear interpolation can't reproduce, each with its <c>offset</c>, so a test asks what the
/// animation shows at an offset rather than counting frames.
/// </summary>
internal static partial class WebAnimationKeyframes
{
    /// <summary>The offset of every keyframe: its own, or evenly spaced between its neighbours' when it has none.</summary>
    public static double[] Offsets(IReadOnlyList<Dictionary<string, object>> keyframes)
    {
        var offsets = new double[keyframes.Count];
        for (int i = 0; i < keyframes.Count; i++)
            offsets[i] = keyframes[i].TryGetValue("offset", out var o)
                ? Convert.ToDouble(o, CultureInfo.InvariantCulture)
                : double.NaN;
        if (double.IsNaN(offsets[0])) offsets[0] = keyframes.Count == 1 ? 1 : 0;
        if (double.IsNaN(offsets[^1])) offsets[^1] = 1;
        for (int i = 1; i < offsets.Length - 1; i++)
        {
            if (!double.IsNaN(offsets[i])) continue;
            int next = i + 1;
            while (double.IsNaN(offsets[next])) next++;
            double step = (offsets[next] - offsets[i - 1]) / (next - (i - 1));
            for (int j = i; j < next; j++) offsets[j] = offsets[i - 1] + (step * (j - (i - 1)));
        }
        return offsets;
    }

    /// <summary>The value <paramref name="read"/> takes at <paramref name="offset"/>, interpolated linearly between keyframes.</summary>
    public static double At(IReadOnlyList<Dictionary<string, object>> keyframes, double offset, Func<Dictionary<string, object>, double> read)
    {
        var offsets = Offsets(keyframes);
        if (offset <= offsets[0]) return read(keyframes[0]);
        for (int i = 1; i < keyframes.Count; i++)
        {
            if (offset > offsets[i]) continue;
            double span = offsets[i] - offsets[i - 1];
            double t = span > 0 ? (offset - offsets[i - 1]) / span : 1;
            double a = read(keyframes[i - 1]), b = read(keyframes[i]);
            return a + ((b - a) * t);
        }
        return read(keyframes[^1]);
    }

    public static double TranslateX(Dictionary<string, object> keyframe) => Translate(keyframe, 1);

    public static double TranslateY(Dictionary<string, object> keyframe) => Translate(keyframe, 2);

    public static double Scale(Dictionary<string, object> keyframe)
        => ScaleRegex().Match((string)keyframe["transform"]) is { Success: true } m
            ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
            : 1;

    public static double Number(Dictionary<string, object> keyframe, string property)
        => double.Parse((string)keyframe[property], CultureInfo.InvariantCulture);

    private static double Translate(Dictionary<string, object> keyframe, int group)
        => TranslateRegex().Match((string)keyframe["transform"]) is { Success: true } m
            ? double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture)
            : 0;

    [GeneratedRegex(@"translate\(([-\d.eE]+)px,([-\d.eE]+)px\)")]
    private static partial Regex TranslateRegex();

    [GeneratedRegex(@"scale\(([^)]+)\)")]
    private static partial Regex ScaleRegex();
}
