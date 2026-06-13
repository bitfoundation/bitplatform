namespace Bit.Bmotion.Models;

/// <summary>
/// Describes a set of animatable CSS / transform properties - the "what" of an animation.
/// Assign to Initial, Animate, Exit, WhileHover, WhileTap, etc.
/// </summary>
public class AnimationProps
{
    // ── Transform properties ──────────────────────────────────────────────────
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }

    public double? Scale { get; set; }
    public double? ScaleX { get; set; }
    public double? ScaleY { get; set; }

    public double? Rotate { get; set; }
    public double? RotateX { get; set; }
    public double? RotateY { get; set; }
    public double? RotateZ { get; set; }

    public double? SkewX { get; set; }
    public double? SkewY { get; set; }

    public double? Perspective { get; set; }

    // ── Visual properties ─────────────────────────────────────────────────────
    public double? Opacity { get; set; }

    // Accept CSS color strings: #rgb, #rrggbbaa, rgb(), hsl(), named colors
    public string? BackgroundColor { get; set; }
    public string? Color { get; set; }
    public string? BorderColor { get; set; }
    public string? OutlineColor { get; set; }
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    // Box model (accept px values or CSS strings like "50%" or "2rem")
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? BorderRadius { get; set; }
    public string? BoxShadow { get; set; }

    // ── SVG path drawing ──────────────────────────────────────────────────────
    /// <summary>0 = invisible, 1 = fully drawn. Drives strokeDashoffset.</summary>
    public double? PathLength { get; set; }
    /// <summary>Offset along the path (0–1).</summary>
    public double? PathOffset { get; set; }
    /// <summary>Spacing between dash/gap pairs (0–1).</summary>
    public double? PathSpacing { get; set; }

    // ── CSS custom properties (e.g. "--my-var") ───────────────────────────────
    /// <summary>Animate arbitrary CSS custom properties. Keys must start with "--".</summary>
    public Dictionary<string, string>? CssVars { get; set; }

    // ── Keyframe arrays ───────────────────────────────────────────────────────
    /// <summary>
    /// Per-property keyframe arrays for multi-step animations.
    /// Keys are the same as the simple property names ("x", "y", "scale", "opacity",
    /// "backgroundColor", etc.). Values are <c>double[]</c> or <c>string[]</c>.
    /// When a key is present here it takes precedence over the single-value property.
    /// <example>
    /// <code>
    /// new AnimationProps
    /// {
    ///     Keyframes = new()
    ///     {
    ///         ["scale"] = new double[] { 1, 1.4, 0.8, 1 },
    ///         ["backgroundColor"] = new string[] { "#6c47ff", "#ff4785", "#6c47ff" }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public Dictionary<string, object>? Keyframes { get; set; }

    /// <summary>
    /// Serialise to a plain JS-friendly dictionary that the interop layer understands.
    /// </summary>
    internal Dictionary<string, object?> ToJsDictionary()
    {
        var d = new Dictionary<string, object?>();

        if (X.HasValue) d["x"] = X.Value;
        if (Y.HasValue) d["y"] = Y.Value;
        if (Z.HasValue) d["z"] = Z.Value;
        if (Scale.HasValue) d["scale"] = Scale.Value;
        if (ScaleX.HasValue) d["scaleX"] = ScaleX.Value;
        if (ScaleY.HasValue) d["scaleY"] = ScaleY.Value;
        if (Rotate.HasValue) d["rotate"] = Rotate.Value;
        if (RotateX.HasValue) d["rotateX"] = RotateX.Value;
        if (RotateY.HasValue) d["rotateY"] = RotateY.Value;
        if (RotateZ.HasValue) d["rotateZ"] = RotateZ.Value;
        if (SkewX.HasValue) d["skewX"] = SkewX.Value;
        if (SkewY.HasValue) d["skewY"] = SkewY.Value;
        if (Perspective.HasValue) d["perspective"] = Perspective.Value;
        if (Opacity.HasValue) d["opacity"] = Opacity.Value;
        if (BackgroundColor != null) d["backgroundColor"] = BackgroundColor;
        if (Color != null) d["color"] = Color;
        if (BorderColor != null) d["borderColor"] = BorderColor;
        if (OutlineColor != null) d["outlineColor"] = OutlineColor;
        if (Fill != null) d["fill"] = Fill;
        if (Stroke != null) d["stroke"] = Stroke;
        if (Width != null) d["width"] = Width;
        if (Height != null) d["height"] = Height;
        if (BorderRadius != null) d["borderRadius"] = BorderRadius;
        if (BoxShadow != null) d["boxShadow"] = BoxShadow;
        if (PathLength.HasValue) d["pathLength"] = PathLength.Value;
        if (PathOffset.HasValue) d["pathOffset"] = PathOffset.Value;
        if (PathSpacing.HasValue) d["pathSpacing"] = PathSpacing.Value;

        if (CssVars != null)
            foreach (var kv in CssVars)
                d[kv.Key] = kv.Value;

        // Keyframe arrays override single values
        if (Keyframes != null)
            foreach (var kv in Keyframes)
                d[kv.Key] = kv.Value;

        return d;
    }

    /// <summary>
    /// Render these props as an inline CSS style string - used server-side to avoid a
    /// flash of un-styled content before the JS interop layer initialises.
    /// </summary>
    internal string ToCssStyleString()
    {
        var sb = new System.Text.StringBuilder();

        var transforms = new List<string>();
        if (X.HasValue || Y.HasValue || Z.HasValue)
        {
            double x = X ?? 0, y = Y ?? 0, z = Z ?? 0;
            if (z != 0)
                transforms.Add($"translate3d({x}px,{y}px,{z}px)");
            else
                transforms.Add($"translate({x}px,{y}px)");
        }
        if (Scale.HasValue) transforms.Add($"scale({Scale.Value})");
        if (ScaleX.HasValue) transforms.Add($"scaleX({ScaleX.Value})");
        if (ScaleY.HasValue) transforms.Add($"scaleY({ScaleY.Value})");
        if (Rotate.HasValue || RotateZ.HasValue)
            transforms.Add($"rotate({RotateZ ?? Rotate}deg)");
        if (RotateX.HasValue) transforms.Add($"rotateX({RotateX.Value}deg)");
        if (RotateY.HasValue) transforms.Add($"rotateY({RotateY.Value}deg)");
        if (SkewX.HasValue) transforms.Add($"skewX({SkewX.Value}deg)");
        if (SkewY.HasValue) transforms.Add($"skewY({SkewY.Value}deg)");
        if (Perspective.HasValue) transforms.Insert(0, $"perspective({Perspective.Value}px)");

        if (transforms.Count > 0) sb.Append($"transform:{string.Join(" ", transforms)};");

        if (Opacity.HasValue) sb.Append($"opacity:{Opacity.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
        if (BackgroundColor != null) sb.Append($"background-color:{BackgroundColor};");
        if (Color != null) sb.Append($"color:{Color};");
        if (BorderColor != null) sb.Append($"border-color:{BorderColor};");
        if (Fill != null) sb.Append($"fill:{Fill};");
        if (Stroke != null) sb.Append($"stroke:{Stroke};");
        if (Width != null) sb.Append($"width:{Width};");
        if (Height != null) sb.Append($"height:{Height};");
        if (BorderRadius != null) sb.Append($"border-radius:{BorderRadius};");
        if (PathLength.HasValue)
        {
            double clamped = Math.Max(0, Math.Min(1, PathLength.Value));
            sb.Append($"stroke-dasharray:1 1;stroke-dashoffset:{(1 - clamped).ToString("G6", System.Globalization.CultureInfo.InvariantCulture)};");
        }

        if (CssVars != null)
            foreach (var kv in CssVars)
                sb.Append($"{kv.Key}:{kv.Value};");

        return sb.ToString();
    }
}
