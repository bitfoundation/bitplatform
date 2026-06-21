namespace Bit.Bmotion;

/// <summary>
/// Describes a set of animatable CSS / transform properties - the "what" of an animation.
/// Assign to Initial, Animate, Exit, WhileHover, WhileTap, etc.
/// </summary>
/// <remarks>
/// <b>Security:</b> string-valued properties (<see cref="BackgroundColor"/>, <see cref="Width"/>,
/// <see cref="BoxShadow"/>, <see cref="CssVars"/>, …) are written verbatim into the element's
/// inline style. They are intended for developer-authored values; binding untrusted end-user input
/// to them risks CSS injection into the element's <c>style</c>.
/// </remarks>
public class BmotionAnimationProps
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
    /// <summary>
    /// 0 = invisible, 1 = fully drawn. Drives strokeDashoffset.
    /// <para>
    /// The generated <c>stroke-dasharray</c>/<c>stroke-dashoffset</c> values are normalised to a
    /// unit path length, so the target SVG element must declare <c>pathLength="1"</c> for the
    /// drawing to render correctly.
    /// </para>
    /// </summary>
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
    /// new BmotionAnimationProps
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
            {
                if (!kv.Key.StartsWith("--")) continue; // contract: CSS custom property keys start with "--"
                d[kv.Key] = kv.Value;
            }

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
                transforms.Add($"translate3d({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px,{BmotionCssFormat.Num(z)}px)");
            else
                transforms.Add($"translate({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px)");
        }
        if (Scale.HasValue && Scale.Value != 1)
        {
            transforms.Add($"scale({BmotionCssFormat.Num(Scale.Value)})");
        }
        else
        {
            if (ScaleX.HasValue && ScaleX.Value != 1) transforms.Add($"scaleX({BmotionCssFormat.Num(ScaleX.Value)})");
            if (ScaleY.HasValue && ScaleY.Value != 1) transforms.Add($"scaleY({BmotionCssFormat.Num(ScaleY.Value)})");
        }
        // Prefer a non-zero rotateZ, otherwise fall back to rotate, so an explicit RotateZ = 0
        // doesn't mask a meaningful Rotate value (matches BmotionTransformComposer).
        double rotateZ = RotateZ.HasValue && RotateZ.Value != 0 ? RotateZ.Value : (Rotate ?? 0);
        if (rotateZ != 0) transforms.Add($"rotate({BmotionCssFormat.Num(rotateZ)}deg)");
        if (RotateX.HasValue && RotateX.Value != 0) transforms.Add($"rotateX({BmotionCssFormat.Num(RotateX.Value)}deg)");
        if (RotateY.HasValue && RotateY.Value != 0) transforms.Add($"rotateY({BmotionCssFormat.Num(RotateY.Value)}deg)");
        if (SkewX.HasValue && SkewX.Value != 0) transforms.Add($"skewX({BmotionCssFormat.Num(SkewX.Value)}deg)");
        if (SkewY.HasValue && SkewY.Value != 0) transforms.Add($"skewY({BmotionCssFormat.Num(SkewY.Value)}deg)");
        if (Perspective.HasValue) transforms.Insert(0, $"perspective({BmotionCssFormat.Num(Perspective.Value)}px)");

        if (transforms.Count > 0) sb.Append($"transform:{string.Join(" ", transforms)};");

        if (Opacity.HasValue) sb.Append($"opacity:{BmotionCssFormat.Num(Opacity.Value)};");
        if (BackgroundColor != null) sb.Append($"background-color:{BackgroundColor};");
        if (Color != null) sb.Append($"color:{Color};");
        if (BorderColor != null) sb.Append($"border-color:{BorderColor};");
        if (OutlineColor != null) sb.Append($"outline-color:{OutlineColor};");
        if (Fill != null) sb.Append($"fill:{Fill};");
        if (Stroke != null) sb.Append($"stroke:{Stroke};");
        if (Width != null) sb.Append($"width:{Width};");
        if (Height != null) sb.Append($"height:{Height};");
        if (BorderRadius != null) sb.Append($"border-radius:{BorderRadius};");
        if (BoxShadow != null) sb.Append($"box-shadow:{BoxShadow};");
        if (PathLength.HasValue)
        {
            double clamped = Math.Max(0, Math.Min(1, PathLength.Value));
            double spacing = Math.Max(0, Math.Min(1, PathSpacing ?? 1.0));
            double offset = Math.Max(0, Math.Min(1, PathOffset ?? 0.0));
            sb.Append($"stroke-dasharray:{BmotionCssFormat.Num(clamped)} {BmotionCssFormat.Num(spacing)};");
            sb.Append($"stroke-dashoffset:{BmotionCssFormat.Num(1 - clamped - offset)};");
        }

        if (CssVars != null)
            foreach (var kv in CssVars)
            {
                if (!kv.Key.StartsWith("--")) continue; // contract: CSS custom property keys start with "--"
                sb.Append($"{kv.Key}:{kv.Value};");
            }

        return sb.ToString();
    }

    /// <summary>
    /// Render these props as a dictionary of individual CSS declarations
    /// (camelCase keys suitable for <c>element.style[prop] = value</c>).
    /// Used by instant <c>set()</c> calls so we update only the specified
    /// declarations instead of replacing the element's entire inline style
    /// (which assigning <c>cssText</c> would do).
    /// </summary>
    internal Dictionary<string, string> ToCssStyleDictionary()
    {
        var d = new Dictionary<string, string>();

        var transforms = new List<string>();
        if (Perspective.HasValue) transforms.Add($"perspective({BmotionCssFormat.Num(Perspective.Value)}px)");
        if (X.HasValue || Y.HasValue || Z.HasValue)
        {
            double x = X ?? 0, y = Y ?? 0, z = Z ?? 0;
            transforms.Add(z != 0
                ? $"translate3d({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px,{BmotionCssFormat.Num(z)}px)"
                : $"translate({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px)");
        }
        if (Scale.HasValue && Scale.Value != 1)
        {
            transforms.Add($"scale({BmotionCssFormat.Num(Scale.Value)})");
        }
        else
        {
            if (ScaleX.HasValue && ScaleX.Value != 1) transforms.Add($"scaleX({BmotionCssFormat.Num(ScaleX.Value)})");
            if (ScaleY.HasValue && ScaleY.Value != 1) transforms.Add($"scaleY({BmotionCssFormat.Num(ScaleY.Value)})");
        }
        // Prefer a non-zero rotateZ, otherwise fall back to rotate, so an explicit RotateZ = 0
        // doesn't mask a meaningful Rotate value (matches BmotionTransformComposer).
        double rotateZ = RotateZ.HasValue && RotateZ.Value != 0 ? RotateZ.Value : (Rotate ?? 0);
        if (rotateZ != 0) transforms.Add($"rotate({BmotionCssFormat.Num(rotateZ)}deg)");
        if (RotateX.HasValue && RotateX.Value != 0) transforms.Add($"rotateX({BmotionCssFormat.Num(RotateX.Value)}deg)");
        if (RotateY.HasValue && RotateY.Value != 0) transforms.Add($"rotateY({BmotionCssFormat.Num(RotateY.Value)}deg)");
        if (SkewX.HasValue && SkewX.Value != 0) transforms.Add($"skewX({BmotionCssFormat.Num(SkewX.Value)}deg)");
        if (SkewY.HasValue && SkewY.Value != 0) transforms.Add($"skewY({BmotionCssFormat.Num(SkewY.Value)}deg)");
        if (transforms.Count > 0) d["transform"] = string.Join(" ", transforms);

        if (Opacity.HasValue) d["opacity"] = BmotionCssFormat.Num(Opacity.Value);
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
        if (PathLength.HasValue)
        {
            double clamped = Math.Max(0, Math.Min(1, PathLength.Value));
            double spacing = Math.Max(0, Math.Min(1, PathSpacing ?? 1.0));
            double offset = Math.Max(0, Math.Min(1, PathOffset ?? 0.0));
            d["strokeDasharray"] = $"{BmotionCssFormat.Num(clamped)} {BmotionCssFormat.Num(spacing)}";
            d["strokeDashoffset"] = BmotionCssFormat.Num(1 - clamped - offset);
        }

        if (CssVars != null)
            foreach (var kv in CssVars)
            {
                if (!kv.Key.StartsWith("--")) continue; // contract: CSS custom property keys start with "--"
                d[kv.Key] = kv.Value;
            }

        return d;
    }

    /// <summary>
    /// Structural value comparison used by <see cref="Bmotion"/> to decide whether an
    /// <c>Animate</c> target actually changed between renders. This avoids re-triggering an
    /// animation (and <c>OnAnimationStart</c>) on every unrelated re-render when a consumer writes
    /// the idiomatic <c>Animate="@(new BmotionAnimationProps { ... })"</c> (a fresh reference each render).
    /// </summary>
    internal bool ValueEquals(BmotionAnimationProps? o)
    {
        if (o is null) return false;
        if (ReferenceEquals(this, o)) return true;

        bool scalars =
            X == o.X && Y == o.Y && Z == o.Z &&
            Scale == o.Scale && ScaleX == o.ScaleX && ScaleY == o.ScaleY &&
            Rotate == o.Rotate && RotateX == o.RotateX && RotateY == o.RotateY && RotateZ == o.RotateZ &&
            SkewX == o.SkewX && SkewY == o.SkewY && Perspective == o.Perspective &&
            Opacity == o.Opacity &&
            BackgroundColor == o.BackgroundColor && Color == o.Color && BorderColor == o.BorderColor &&
            OutlineColor == o.OutlineColor && Fill == o.Fill && Stroke == o.Stroke &&
            Width == o.Width && Height == o.Height && BorderRadius == o.BorderRadius && BoxShadow == o.BoxShadow &&
            PathLength == o.PathLength && PathOffset == o.PathOffset && PathSpacing == o.PathSpacing;

        return scalars && DictEquals(CssVars, o.CssVars) && KeyframeDictEquals(Keyframes, o.Keyframes);
    }

    private static bool DictEquals(Dictionary<string, string>? a, Dictionary<string, string>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }

    private static bool KeyframeDictEquals(Dictionary<string, object>? a, Dictionary<string, object>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var v)) return false;
            if (!SequenceEquals(kv.Value, v)) return false;
        }
        return true;
    }

    private static bool SequenceEquals(object? a, object? b)
    {
        if (Equals(a, b)) return true;
        if (a is System.Collections.IEnumerable ea && a is not string &&
            b is System.Collections.IEnumerable eb && b is not string)
        {
            var ia = ea.GetEnumerator();
            var ib = eb.GetEnumerator();
            try
            {
                while (true)
                {
                    bool na = ia.MoveNext(), nb = ib.MoveNext();
                    if (na != nb) return false;
                    if (!na) return true;
                    if (!Equals(ia.Current, ib.Current)) return false;
                }
            }
            finally
            {
                // Non-generic GetEnumerator() may return an IDisposable enumerator (e.g. List<T>);
                // dispose both so we don't leak iterator resources on early or normal exit.
                (ia as IDisposable)?.Dispose();
                (ib as IDisposable)?.Dispose();
            }
        }
        return false;
    }
}
