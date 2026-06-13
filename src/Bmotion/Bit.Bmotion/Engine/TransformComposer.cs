namespace Bit.Bmotion.Engine;

/// <summary>
/// Builds a CSS <c>transform</c> string from a dictionary of individual transform components.
/// Mirrors the JS <c>buildTransformString</c> function.
/// </summary>
internal static class TransformComposer
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
            parts.Add($"perspective({persp}px)");

        double x = t.GetValueOrDefault("x");
        double y = t.GetValueOrDefault("y");
        double z = t.GetValueOrDefault("z");
        if (x != 0 || y != 0 || z != 0)
            parts.Add(z != 0
                ? $"translate3d({x}px,{y}px,{z}px)"
                : $"translate({x}px,{y}px)");

        if (t.TryGetValue("scale", out double scale))
            parts.Add($"scale({scale})");
        else
        {
            if (t.TryGetValue("scaleX", out double sx) && sx != 1) parts.Add($"scaleX({sx})");
            if (t.TryGetValue("scaleY", out double sy) && sy != 1) parts.Add($"scaleY({sy})");
        }

        // rotateZ / rotate aliases
        double rz = t.TryGetValue("rotateZ", out double rz2) ? rz2 : t.GetValueOrDefault("rotate");
        if (rz != 0) parts.Add($"rotate({rz}deg)");
        if (t.TryGetValue("rotateX", out double rx) && rx != 0) parts.Add($"rotateX({rx}deg)");
        if (t.TryGetValue("rotateY", out double ry) && ry != 0) parts.Add($"rotateY({ry}deg)");

        if (t.TryGetValue("skewX", out double skx) && skx != 0) parts.Add($"skewX({skx}deg)");
        if (t.TryGetValue("skewY", out double sky) && sky != 0) parts.Add($"skewY({sky}deg)");

        return string.Join(" ", parts);
    }
}
