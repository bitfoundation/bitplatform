namespace Bit.Bmotion;

/// <summary>
/// Describes a set of animatable CSS / transform properties - the "what" of an animation.
/// Assign to Initial, Animate, Exit, WhileHover, WhileTap, etc. Create tersely via
/// <see cref="Bm.To"/>.
/// <para>
/// Every property accepts a single target value or a keyframe sequence:
/// <c>Scale = 1.2</c> or <c>Scale = [1, 1.4, 0.8, 1]</c>.
/// </para>
/// </summary>
/// <remarks>
/// <b>Security:</b> string-valued properties (<see cref="BackgroundColor"/>, <see cref="Width"/>,
/// <see cref="BoxShadow"/>, <see cref="CssVars"/>, …) are written verbatim into the element's
/// inline style. They are intended for developer-authored values; binding untrusted end-user input
/// to them risks CSS injection into the element's <c>style</c>.
/// </remarks>
public class BmProps
{
    // ── Transform properties ──────────────────────────────────────────────────
    public BmKeyframes? X { get; set; }
    public BmKeyframes? Y { get; set; }
    public BmKeyframes? Z { get; set; }

    public BmKeyframes? Scale { get; set; }
    public BmKeyframes? ScaleX { get; set; }
    public BmKeyframes? ScaleY { get; set; }

    public BmKeyframes? Rotate { get; set; }
    public BmKeyframes? RotateX { get; set; }
    public BmKeyframes? RotateY { get; set; }
    public BmKeyframes? RotateZ { get; set; }

    public BmKeyframes? SkewX { get; set; }
    public BmKeyframes? SkewY { get; set; }

    public BmKeyframes? Perspective { get; set; }

    /// <summary>Transform origin X (0-1; 0.5 = center). Applied instantly, not interpolated.</summary>
    public double? OriginX { get; set; }

    /// <summary>Transform origin Y (0-1; 0.5 = center). Applied instantly, not interpolated.</summary>
    public double? OriginY { get; set; }

    // ── Visual properties ─────────────────────────────────────────────────────
    public BmKeyframes? Opacity { get; set; }

    // Accept CSS color strings: #rgb, #rrggbbaa, rgb(), hsl(), named colors
    public BmStringKeyframes? BackgroundColor { get; set; }
    public BmStringKeyframes? Color { get; set; }
    public BmStringKeyframes? BorderColor { get; set; }
    public BmStringKeyframes? OutlineColor { get; set; }
    public BmStringKeyframes? Fill { get; set; }
    public BmStringKeyframes? Stroke { get; set; }

    // Box model (bare numbers default to px; CSS strings like "50%" or "2rem" pass through)
    public BmStringKeyframes? Width { get; set; }
    public BmStringKeyframes? Height { get; set; }
    public BmStringKeyframes? BorderRadius { get; set; }
    public BmStringKeyframes? BoxShadow { get; set; }

    /// <summary>
    /// CSS filter, e.g. <c>"blur(8px) brightness(1.2)"</c>. Between values with the same filter
    /// list shape, each number (and embedded color) interpolates smoothly.
    /// </summary>
    public BmStringKeyframes? Filter { get; set; }

    // ── SVG path drawing ──────────────────────────────────────────────────────
    /// <summary>
    /// 0 = invisible, 1 = fully drawn. Drives strokeDashoffset.
    /// <para>
    /// The generated <c>stroke-dasharray</c>/<c>stroke-dashoffset</c> values are normalised to a
    /// unit path length, so the target SVG element must declare <c>pathLength="1"</c> for the
    /// drawing to render correctly.
    /// </para>
    /// </summary>
    public BmKeyframes? PathLength { get; set; }
    /// <summary>Offset along the path (0-1).</summary>
    public BmKeyframes? PathOffset { get; set; }
    /// <summary>Spacing between dash/gap pairs (0-1).</summary>
    public BmKeyframes? PathSpacing { get; set; }

    // ── CSS custom properties (e.g. "--my-var") ───────────────────────────────
    /// <summary>Animate arbitrary CSS custom properties. Keys must start with "--".</summary>
    public Dictionary<string, string>? CssVars { get; set; }

    // ── Embedded transition ───────────────────────────────────────────────────
    /// <summary>
    /// Transition used when animating to this target; overrides the component-level
    /// <c>Transition</c>. Lets variants and one-off targets carry their own timing/physics.
    /// </summary>
    public BmTransition? Transition { get; set; }

    /// <summary>
    /// Serialise to a plain dictionary the animation engine understands: single values as
    /// <c>double</c>/<c>string</c>, keyframe sequences as <c>double[]</c>/<c>string[]</c>.
    /// </summary>
    internal Dictionary<string, object?> ToJsDictionary()
    {
        var d = new Dictionary<string, object?>();

        if (X is not null) d["x"] = X.ToEngineValue();
        if (Y is not null) d["y"] = Y.ToEngineValue();
        if (Z is not null) d["z"] = Z.ToEngineValue();
        if (Scale is not null) d["scale"] = Scale.ToEngineValue();
        if (ScaleX is not null) d["scaleX"] = ScaleX.ToEngineValue();
        if (ScaleY is not null) d["scaleY"] = ScaleY.ToEngineValue();
        if (Rotate is not null) d["rotate"] = Rotate.ToEngineValue();
        if (RotateX is not null) d["rotateX"] = RotateX.ToEngineValue();
        if (RotateY is not null) d["rotateY"] = RotateY.ToEngineValue();
        if (RotateZ is not null) d["rotateZ"] = RotateZ.ToEngineValue();
        if (SkewX is not null) d["skewX"] = SkewX.ToEngineValue();
        if (SkewY is not null) d["skewY"] = SkewY.ToEngineValue();
        if (Perspective is not null) d["perspective"] = Perspective.ToEngineValue();
        if (OriginX.HasValue || OriginY.HasValue) d["transformOrigin"] = TransformOriginCss();
        if (Opacity is not null) d["opacity"] = Opacity.ToEngineValue();
        if (BackgroundColor is not null) d["backgroundColor"] = BackgroundColor.ToEngineValue();
        if (Color is not null) d["color"] = Color.ToEngineValue();
        if (BorderColor is not null) d["borderColor"] = BorderColor.ToEngineValue();
        if (OutlineColor is not null) d["outlineColor"] = OutlineColor.ToEngineValue();
        if (Fill is not null) d["fill"] = Fill.ToEngineValue();
        if (Stroke is not null) d["stroke"] = Stroke.ToEngineValue();
        if (Width is not null) d["width"] = Width.ToEngineValue();
        if (Height is not null) d["height"] = Height.ToEngineValue();
        if (BorderRadius is not null) d["borderRadius"] = BorderRadius.ToEngineValue();
        if (BoxShadow is not null) d["boxShadow"] = BoxShadow.ToEngineValue();
        if (Filter is not null) d["filter"] = Filter.ToEngineValue();
        if (PathLength is not null) d["pathLength"] = PathLength.ToEngineValue();
        if (PathOffset is not null) d["pathOffset"] = PathOffset.ToEngineValue();
        if (PathSpacing is not null) d["pathSpacing"] = PathSpacing.ToEngineValue();

        if (CssVars != null)
            foreach (var kv in CssVars)
            {
                if (!kv.Key.StartsWith("--")) continue; // contract: CSS custom property keys start with "--"
                d[kv.Key] = kv.Value;
            }

        return d;
    }

    // ── First-frame accessors for the initial inline style ────────────────────
    // The initial CSS renders each property's FIRST keyframe (where the animation starts).
    // A Bm.Current wildcard in the first frame is skipped: its value is only known at runtime.

    private static bool CssNum(BmKeyframes? k, out double value)
    {
        value = 0;
        return k is not null && k.TryGetCssNumber(out value);
    }

    private static string? CssStr(BmStringKeyframes? k) => k?.First;

    private string TransformOriginCss()
        => $"{BmotionCssFormat.Num((OriginX ?? 0.5) * 100)}% {BmotionCssFormat.Num((OriginY ?? 0.5) * 100)}%";

    /// <summary>
    /// Render these props as an inline CSS style string - used server-side to avoid a
    /// flash of un-styled content before the JS interop layer initialises.
    /// </summary>
    internal string ToCssStyleString()
    {
        var sb = new System.Text.StringBuilder();

        var transforms = new List<string>();
        bool hasX = CssNum(X, out double x), hasY = CssNum(Y, out double y), hasZ = CssNum(Z, out double z);
        if (hasX || hasY || hasZ)
        {
            if (z != 0)
                transforms.Add($"translate3d({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px,{BmotionCssFormat.Num(z)}px)");
            else
                transforms.Add($"translate({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px)");
        }
        if (CssNum(Scale, out double scale) && scale != 1)
        {
            transforms.Add($"scale({BmotionCssFormat.Num(scale)})");
        }
        else
        {
            if (CssNum(ScaleX, out double sx) && sx != 1) transforms.Add($"scaleX({BmotionCssFormat.Num(sx)})");
            if (CssNum(ScaleY, out double sy) && sy != 1) transforms.Add($"scaleY({BmotionCssFormat.Num(sy)})");
        }
        // Prefer a non-zero rotateZ, otherwise fall back to rotate, so an explicit RotateZ = 0
        // doesn't mask a meaningful Rotate value (matches BmotionTransformComposer).
        CssNum(Rotate, out double rot);
        double rotateZ = CssNum(RotateZ, out double rz) && rz != 0 ? rz : rot;
        if (rotateZ != 0) transforms.Add($"rotate({BmotionCssFormat.Num(rotateZ)}deg)");
        if (CssNum(RotateX, out double rx) && rx != 0) transforms.Add($"rotateX({BmotionCssFormat.Num(rx)}deg)");
        if (CssNum(RotateY, out double ry) && ry != 0) transforms.Add($"rotateY({BmotionCssFormat.Num(ry)}deg)");
        if (CssNum(SkewX, out double kx) && kx != 0) transforms.Add($"skewX({BmotionCssFormat.Num(kx)}deg)");
        if (CssNum(SkewY, out double ky) && ky != 0) transforms.Add($"skewY({BmotionCssFormat.Num(ky)}deg)");
        if (CssNum(Perspective, out double persp)) transforms.Insert(0, $"perspective({BmotionCssFormat.Num(persp)}px)");

        if (transforms.Count > 0) sb.Append($"transform:{string.Join(" ", transforms)};");
        if (OriginX.HasValue || OriginY.HasValue) sb.Append($"transform-origin:{TransformOriginCss()};");

        if (CssNum(Opacity, out double opacity)) sb.Append($"opacity:{BmotionCssFormat.Num(opacity)};");
        if (CssStr(BackgroundColor) is { } bg) sb.Append($"background-color:{bg};");
        if (CssStr(Color) is { } col) sb.Append($"color:{col};");
        if (CssStr(BorderColor) is { } bc) sb.Append($"border-color:{bc};");
        if (CssStr(OutlineColor) is { } oc) sb.Append($"outline-color:{oc};");
        if (CssStr(Fill) is { } fill) sb.Append($"fill:{fill};");
        if (CssStr(Stroke) is { } stroke) sb.Append($"stroke:{stroke};");
        if (CssStr(Width) is { } w) sb.Append($"width:{w};");
        if (CssStr(Height) is { } h) sb.Append($"height:{h};");
        if (CssStr(BorderRadius) is { } br) sb.Append($"border-radius:{br};");
        if (CssStr(BoxShadow) is { } bs) sb.Append($"box-shadow:{bs};");
        if (CssStr(Filter) is { } flt) sb.Append($"filter:{flt};");
        if (CssNum(PathLength, out double pl))
        {
            double clamped = Math.Max(0, Math.Min(1, pl));
            double spacing = Math.Max(0, Math.Min(1, CssNum(PathSpacing, out double ps) ? ps : 1.0));
            double offset = Math.Max(0, Math.Min(1, CssNum(PathOffset, out double po) ? po : 0.0));
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
    /// (which assigning <c>cssText</c> would do). Keyframe sequences apply their
    /// LAST frame - the value the sequence settles on.
    /// </summary>
    internal Dictionary<string, string> ToCssStyleDictionary()
    {
        // For an instant Set, a keyframe sequence collapses to its final value.
        static bool Num(BmKeyframes? k, out double value)
        {
            value = 0;
            if (k is null) return false;
            value = k.Last;
            return double.IsFinite(value);
        }
        static string? Str(BmStringKeyframes? k) => k?.Last;

        var d = new Dictionary<string, string>();

        var transforms = new List<string>();
        if (Num(Perspective, out double persp)) transforms.Add($"perspective({BmotionCssFormat.Num(persp)}px)");
        bool hasX = Num(X, out double x), hasY = Num(Y, out double y), hasZ = Num(Z, out double z);
        if (hasX || hasY || hasZ)
        {
            transforms.Add(z != 0
                ? $"translate3d({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px,{BmotionCssFormat.Num(z)}px)"
                : $"translate({BmotionCssFormat.Num(x)}px,{BmotionCssFormat.Num(y)}px)");
        }
        if (Num(Scale, out double scale) && scale != 1)
        {
            transforms.Add($"scale({BmotionCssFormat.Num(scale)})");
        }
        else
        {
            if (Num(ScaleX, out double sx) && sx != 1) transforms.Add($"scaleX({BmotionCssFormat.Num(sx)})");
            if (Num(ScaleY, out double sy) && sy != 1) transforms.Add($"scaleY({BmotionCssFormat.Num(sy)})");
        }
        // Prefer a non-zero rotateZ, otherwise fall back to rotate, so an explicit RotateZ = 0
        // doesn't mask a meaningful Rotate value (matches BmotionTransformComposer).
        Num(Rotate, out double rot);
        double rotateZ = Num(RotateZ, out double rz) && rz != 0 ? rz : rot;
        if (rotateZ != 0) transforms.Add($"rotate({BmotionCssFormat.Num(rotateZ)}deg)");
        if (Num(RotateX, out double rx) && rx != 0) transforms.Add($"rotateX({BmotionCssFormat.Num(rx)}deg)");
        if (Num(RotateY, out double ry) && ry != 0) transforms.Add($"rotateY({BmotionCssFormat.Num(ry)}deg)");
        if (Num(SkewX, out double kx) && kx != 0) transforms.Add($"skewX({BmotionCssFormat.Num(kx)}deg)");
        if (Num(SkewY, out double ky) && ky != 0) transforms.Add($"skewY({BmotionCssFormat.Num(ky)}deg)");
        if (transforms.Count > 0) d["transform"] = string.Join(" ", transforms);
        if (OriginX.HasValue || OriginY.HasValue) d["transformOrigin"] = TransformOriginCss();

        if (Num(Opacity, out double opacity)) d["opacity"] = BmotionCssFormat.Num(opacity);
        if (Str(BackgroundColor) is { } bg) d["backgroundColor"] = bg;
        if (Str(Color) is { } col) d["color"] = col;
        if (Str(BorderColor) is { } bc) d["borderColor"] = bc;
        if (Str(OutlineColor) is { } oc) d["outlineColor"] = oc;
        if (Str(Fill) is { } fill) d["fill"] = fill;
        if (Str(Stroke) is { } stroke) d["stroke"] = stroke;
        if (Str(Width) is { } w) d["width"] = w;
        if (Str(Height) is { } h) d["height"] = h;
        if (Str(BorderRadius) is { } br) d["borderRadius"] = br;
        if (Str(BoxShadow) is { } bs) d["boxShadow"] = bs;
        if (Str(Filter) is { } flt) d["filter"] = flt;
        if (Num(PathLength, out double pl))
        {
            double clamped = Math.Max(0, Math.Min(1, pl));
            double spacing = Math.Max(0, Math.Min(1, Num(PathSpacing, out double ps) ? ps : 1.0));
            double offset = Math.Max(0, Math.Min(1, Num(PathOffset, out double po) ? po : 0.0));
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
    /// the idiomatic <c>Animate="Bm.To(...)"</c> (a fresh reference each render).
    /// </summary>
    internal bool ValueEquals(BmProps? o)
    {
        if (o is null) return false;
        if (ReferenceEquals(this, o)) return true;

        bool values =
            Equals(X, o.X) && Equals(Y, o.Y) && Equals(Z, o.Z) &&
            Equals(Scale, o.Scale) && Equals(ScaleX, o.ScaleX) && Equals(ScaleY, o.ScaleY) &&
            Equals(Rotate, o.Rotate) && Equals(RotateX, o.RotateX) && Equals(RotateY, o.RotateY) && Equals(RotateZ, o.RotateZ) &&
            Equals(SkewX, o.SkewX) && Equals(SkewY, o.SkewY) && Equals(Perspective, o.Perspective) &&
            OriginX == o.OriginX && OriginY == o.OriginY &&
            Equals(Opacity, o.Opacity) &&
            Equals(BackgroundColor, o.BackgroundColor) && Equals(Color, o.Color) && Equals(BorderColor, o.BorderColor) &&
            Equals(OutlineColor, o.OutlineColor) && Equals(Fill, o.Fill) && Equals(Stroke, o.Stroke) &&
            Equals(Width, o.Width) && Equals(Height, o.Height) && Equals(BorderRadius, o.BorderRadius) && Equals(BoxShadow, o.BoxShadow) && Equals(Filter, o.Filter) &&
            Equals(PathLength, o.PathLength) && Equals(PathOffset, o.PathOffset) && Equals(PathSpacing, o.PathSpacing);

        return values && DictEquals(CssVars, o.CssVars) && BmTransition.AreEquivalent(Transition, o.Transition);
    }

    private static bool DictEquals(Dictionary<string, string>? a, Dictionary<string, string>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }
}
