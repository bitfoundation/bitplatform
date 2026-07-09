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

    // ── Layout / box-model (bare numbers default to px; CSS strings pass through) ──
    public BmStringKeyframes? Top { get; set; }
    public BmStringKeyframes? Left { get; set; }
    public BmStringKeyframes? Right { get; set; }
    public BmStringKeyframes? Bottom { get; set; }
    public BmStringKeyframes? Margin { get; set; }
    public BmStringKeyframes? Padding { get; set; }
    public BmStringKeyframes? Gap { get; set; }

    // ── Typography ─────────────────────────────────────────────────────────────
    public BmStringKeyframes? LetterSpacing { get; set; }
    public BmStringKeyframes? LineHeight { get; set; }
    public BmStringKeyframes? FontSize { get; set; }

    // ── Misc CSS ───────────────────────────────────────────────────────────────
    public BmStringKeyframes? ClipPath { get; set; }
    public BmStringKeyframes? BackgroundPosition { get; set; }
    public BmStringKeyframes? BackgroundSize { get; set; }

    // ── Motion path (CSS offset-*) ─────────────────────────────────────────────
    /// <summary>
    /// The path an element travels along, mapping to CSS <c>offset-path</c>, e.g.
    /// <c>OffsetPath = "path('M0,0 C50,100 150,-50 200,50')"</c>. Usually set once; animate
    /// <see cref="OffsetDistance"/> to move the element along it (both are compositor-friendly).
    /// </summary>
    public BmStringKeyframes? OffsetPath { get; set; }

    /// <summary>Position along <see cref="OffsetPath"/> (CSS <c>offset-distance</c>), e.g. <c>"0%"</c> → <c>"100%"</c>.</summary>
    public BmStringKeyframes? OffsetDistance { get; set; }

    // ── SVG shape morphing ─────────────────────────────────────────────────────
    /// <summary>
    /// The SVG path <c>d</c> attribute, for shape morphing on a <c>&lt;path&gt;</c> element. Two
    /// paths with the <b>same command structure</b> morph smoothly (control points interpolate);
    /// incompatible paths snap. Put the <c>&lt;path&gt;</c> directly inside the Bmotion so it is the
    /// animated element: <c>&lt;Bmotion Animate="Bm.To(d: "…")"&gt;&lt;path d="…" /&gt;&lt;/Bmotion&gt;</c>.
    /// </summary>
    public BmStringKeyframes? D { get; set; }

    // ── CSS custom properties (e.g. "--my-var") ───────────────────────────────
    /// <summary>Animate arbitrary CSS custom properties. Keys must start with "--".</summary>
    public Dictionary<string, string>? CssVars { get; set; }

    /// <summary>
    /// Animate any CSS property not covered by a typed member (motion.dev's <c>animate()</c> escape
    /// hatch), e.g. <c>Css = new() { ["gap"] = "2rem", ["letter-spacing"] = "0.1em" }</c>. Keys accept
    /// dash-case or camelCase. Layout-triggering properties won't be compositor-accelerated.
    /// </summary>
    public Dictionary<string, BmStringKeyframes>? Css { get; set; }

    // ── Embedded transition ───────────────────────────────────────────────────
    /// <summary>
    /// Transition used when animating to this target; overrides the component-level
    /// <c>Transition</c>. Lets variants and one-off targets carry their own timing/physics.
    /// </summary>
    public BmTransition? Transition { get; set; }

    // The extended string-valued props (layout/typography/misc), listed once so the runtime,
    // initial-style and instant-set paths all emit them consistently. EngineKey is the camelCase
    // key JS applies via style[key]; CssProp is the dash-case name for the initial inline style.
    private IEnumerable<(string EngineKey, string CssProp, BmStringKeyframes? Value)> ExtendedStringProps()
    {
        yield return ("top", "top", Top);
        yield return ("left", "left", Left);
        yield return ("right", "right", Right);
        yield return ("bottom", "bottom", Bottom);
        yield return ("margin", "margin", Margin);
        yield return ("padding", "padding", Padding);
        yield return ("gap", "gap", Gap);
        yield return ("letterSpacing", "letter-spacing", LetterSpacing);
        yield return ("lineHeight", "line-height", LineHeight);
        yield return ("fontSize", "font-size", FontSize);
        yield return ("clipPath", "clip-path", ClipPath);
        yield return ("backgroundPosition", "background-position", BackgroundPosition);
        yield return ("backgroundSize", "background-size", BackgroundSize);
        yield return ("offsetPath", "offset-path", OffsetPath);
        yield return ("offsetDistance", "offset-distance", OffsetDistance);
        yield return ("d", "d", D);
    }

    // "background-position" → "backgroundPosition"; leaves camelCase and "--custom" keys untouched.
    internal static string ToCamelCase(string prop)
    {
        if (prop.StartsWith("--", StringComparison.Ordinal) || !prop.Contains('-')) return prop;
        var sb = new System.Text.StringBuilder(prop.Length);
        bool upper = false;
        foreach (var c in prop)
        {
            if (c == '-') { upper = true; continue; }
            sb.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        return sb.ToString();
    }

    // "letterSpacing" → "letter-spacing"; leaves dash-case and "--custom" keys untouched.
    internal static string ToDashCase(string prop)
    {
        if (prop.StartsWith("--", StringComparison.Ordinal)) return prop;
        var sb = new System.Text.StringBuilder(prop.Length + 4);
        foreach (var c in prop)
        {
            if (char.IsUpper(c)) { sb.Append('-'); sb.Append(char.ToLowerInvariant(c)); }
            else sb.Append(c);
        }
        return sb.ToString();
    }

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

        foreach (var (engineKey, _, value) in ExtendedStringProps())
            if (value is not null) d[engineKey] = value.ToEngineValue();

        if (Css != null)
            foreach (var kv in Css)
                if (kv.Value is not null) d[ToCamelCase(kv.Key)] = kv.Value.ToEngineValue();

        if (CssVars != null)
            foreach (var kv in CssVars)
            {
                if (!kv.Key.StartsWith("--")) continue; // contract: CSS custom property keys start with "--"
                d[kv.Key] = kv.Value;
            }

        return d;
    }

    /// <summary>
    /// Enumerates every string-valued CSS declaration this target carries (all keyframes of each
    /// string prop, plus CSS-var values) as <c>(prop, value)</c> pairs. Used by the opt-in
    /// CSS-injection safe mode to validate values written verbatim into inline style.
    /// </summary>
    internal IEnumerable<(string Prop, string Value)> EnumerateCssStringValues()
    {
        foreach (var pair in Frames("backgroundColor", BackgroundColor)) yield return pair;
        foreach (var pair in Frames("color", Color)) yield return pair;
        foreach (var pair in Frames("borderColor", BorderColor)) yield return pair;
        foreach (var pair in Frames("outlineColor", OutlineColor)) yield return pair;
        foreach (var pair in Frames("fill", Fill)) yield return pair;
        foreach (var pair in Frames("stroke", Stroke)) yield return pair;
        foreach (var pair in Frames("width", Width)) yield return pair;
        foreach (var pair in Frames("height", Height)) yield return pair;
        foreach (var pair in Frames("borderRadius", BorderRadius)) yield return pair;
        foreach (var pair in Frames("boxShadow", BoxShadow)) yield return pair;
        foreach (var pair in Frames("filter", Filter)) yield return pair;

        foreach (var (engineKey, _, value) in ExtendedStringProps())
            foreach (var pair in Frames(engineKey, value)) yield return pair;

        if (Css != null)
            foreach (var kv in Css)
                foreach (var pair in Frames(kv.Key, kv.Value)) yield return pair;

        if (CssVars != null)
            foreach (var kv in CssVars)
                yield return (kv.Key, kv.Value);

        static IEnumerable<(string, string)> Frames(string prop, BmStringKeyframes? k)
        {
            if (k is null) yield break;
            foreach (var f in k.Frames) yield return (prop, f);
        }
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

    // Collects the present + finite transform components into the engine's transform-component
    // dictionary, then lets BmotionTransformComposer.Build own the ordering/aliasing/formatting.
    // This is the single source of truth for transform syntax: the runtime per-frame path already
    // calls Build directly, and both initial-style (ToCssStyleString) and instant-set
    // (ToCssStyleDictionary) now route through it too - no more hand-synced duplicate logic.
    // <paramref name="useLast"/> selects which keyframe to sample: the first frame for the initial
    // inline style (where the animation starts) or the last frame for an instant set (where it settles).
    private Dictionary<string, double> CollectTransformComponents(bool useLast)
    {
        var t = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, BmKeyframes? k)
        {
            if (k is null) return;
            if (useLast)
            {
                var v = k.Last;
                if (double.IsFinite(v)) t[key] = v;
            }
            else if (k.TryGetCssNumber(out var v))
            {
                t[key] = v;
            }
        }

        Add("perspective", Perspective);
        Add("x", X); Add("y", Y); Add("z", Z);
        Add("scale", Scale); Add("scaleX", ScaleX); Add("scaleY", ScaleY);
        Add("rotate", Rotate); Add("rotateZ", RotateZ); Add("rotateX", RotateX); Add("rotateY", RotateY);
        Add("skewX", SkewX); Add("skewY", SkewY);
        return t;
    }

    /// <summary>
    /// Render these props as an inline CSS style string - used server-side to avoid a
    /// flash of un-styled content before the JS interop layer initialises.
    /// </summary>
    internal string ToCssStyleString()
    {
        var sb = new System.Text.StringBuilder();

        // The initial inline style renders each transform component's FIRST keyframe.
        var transform = BmotionTransformComposer.Build(CollectTransformComponents(useLast: false));
        if (transform.Length > 0) sb.Append($"transform:{transform};");
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

        foreach (var (_, cssProp, value) in ExtendedStringProps())
        {
            // `d` is an SVG geometry attribute, not a CSS property: an inline `d:<path>` needs a
            // path() wrapper and isn't universally supported, so JS applies it via setAttribute
            // (see _svgGeomAttrs in bit-bmotion.js). The element's own `d` attribute already
            // renders it server-side, so omit it from the initial inline-style string.
            if (cssProp == "d") continue;
            if (CssStr(value) is { } s) sb.Append($"{cssProp}:{s};");
        }

        if (Css != null)
            foreach (var kv in Css)
                if (kv.Value?.First is { } s) sb.Append($"{ToDashCase(kv.Key)}:{s};");

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

        // For an instant Set, each transform component collapses to its LAST keyframe.
        var transform = BmotionTransformComposer.Build(CollectTransformComponents(useLast: true));
        if (transform.Length > 0) d["transform"] = transform;
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

        foreach (var (engineKey, _, value) in ExtendedStringProps())
            if (Str(value) is { } s) d[engineKey] = s;

        if (Css != null)
            foreach (var kv in Css)
                if (kv.Value?.Last is { } s) d[ToCamelCase(kv.Key)] = s;

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
            Equals(Top, o.Top) && Equals(Left, o.Left) && Equals(Right, o.Right) && Equals(Bottom, o.Bottom) &&
            Equals(Margin, o.Margin) && Equals(Padding, o.Padding) && Equals(Gap, o.Gap) &&
            Equals(LetterSpacing, o.LetterSpacing) && Equals(LineHeight, o.LineHeight) && Equals(FontSize, o.FontSize) &&
            Equals(ClipPath, o.ClipPath) && Equals(BackgroundPosition, o.BackgroundPosition) && Equals(BackgroundSize, o.BackgroundSize) &&
            Equals(OffsetPath, o.OffsetPath) && Equals(OffsetDistance, o.OffsetDistance) && Equals(D, o.D) &&
            Equals(PathLength, o.PathLength) && Equals(PathOffset, o.PathOffset) && Equals(PathSpacing, o.PathSpacing);

        return values && DictEquals(CssVars, o.CssVars) && StringKeyframesDictEquals(Css, o.Css)
            && BmTransition.AreEquivalent(Transition, o.Transition);
    }

    private static bool DictEquals(Dictionary<string, string>? a, Dictionary<string, string>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }

    private static bool StringKeyframesDictEquals(Dictionary<string, BmStringKeyframes>? a, Dictionary<string, BmStringKeyframes>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || !Equals(v, kv.Value)) return false;
        return true;
    }
}
