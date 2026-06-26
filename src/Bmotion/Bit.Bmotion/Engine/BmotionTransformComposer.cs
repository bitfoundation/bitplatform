namespace Bit.Bmotion;
/// <summary>
/// Builds a CSS <c>transform</c> string from a dictionary of individual transform components.
/// Mirrors the JS <c>buildTransformString</c> function.
/// </summary>
internal static class BmotionTransformComposer
{
    private static readonly HashSet<string> _transformProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "x", "y", "z",
        "rotateX", "rotateY", "rotateZ", "rotate",
        "scaleX", "scaleY", "scale",
        "skewX", "skewY",
        "perspective",
    };

    public static bool IsTransformProp(string key) => _transformProps.Contains(key);

    /// <summary>
    /// Composes a CSS <c>transform</c> value string from a transform-components dictionary.
    /// Returns an empty string when all values are at their identity.
    /// </summary>
    public static string Build(Dictionary<string, double> t)
    {
        if (t.Count == 0) return string.Empty;

        var parts = new List<string>(8);

        if (t.TryGetValue("perspective", out double persp) && persp != 0)
            parts.Add($"perspective({BmotionCssFormat.Num(persp)}px)");

        double x = t.GetValueOrDefault("x");
        double y = t.GetValueOrDefault("y");
        double z = t.GetValueOrDefault("z");
        if (x != 0 || y != 0 || z != 0)
            parts.Add(z != 0
                ? $"translate3d({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px,{BmotionCssFormat.Num(z)}px)"
                : $"translate({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px)");

        if (t.TryGetValue("scale", out double scale) && scale != 1)
            parts.Add($"scale({BmotionCssFormat.Num(scale)})");
        else
        {
            if (t.TryGetValue("scaleX", out double sx) && sx != 1) parts.Add($"scaleX({BmotionCssFormat.Num(sx)})");
            if (t.TryGetValue("scaleY", out double sy) && sy != 1) parts.Add($"scaleY({BmotionCssFormat.Num(sy)})");
        }

        // rotateZ / rotate aliases: prefer a non-zero rotateZ, otherwise fall back to rotate so a
        // zero rotateZ doesn't mask a meaningful rotate value.
        double rz = t.TryGetValue("rotateZ", out double rz2) && rz2 != 0 ? rz2 : t.GetValueOrDefault("rotate");
        if (rz != 0) parts.Add($"rotate({BmotionCssFormat.Num(rz)}deg)");
        if (t.TryGetValue("rotateX", out double rx) && rx != 0) parts.Add($"rotateX({BmotionCssFormat.Num(rx)}deg)");
        if (t.TryGetValue("rotateY", out double ry) && ry != 0) parts.Add($"rotateY({BmotionCssFormat.Num(ry)}deg)");

        if (t.TryGetValue("skewX", out double skx) && skx != 0) parts.Add($"skewX({BmotionCssFormat.Num(skx)}deg)");
        if (t.TryGetValue("skewY", out double sky) && sky != 0) parts.Add($"skewY({BmotionCssFormat.Num(sky)}deg)");

        return string.Join(" ", parts);
    }
}
