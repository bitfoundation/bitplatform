using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A text-highlight annotation: one or more rectangles (typically one per text line) drawn with multiply blending
/// so the underlying document content stays legible. Used for document/PDF review interfaces.
/// </summary>
public sealed record BinkHighlightElement : BinkElement
{
    private readonly BinkRect[] _rects;

    /// <summary>Creates a highlight over the given rectangles.</summary>
    public BinkHighlightElement(IEnumerable<BinkRect> rects)
    {
        ArgumentNullException.ThrowIfNull(rects);
        _rects = rects.Select(static r => r.Normalize()).Where(static r => !r.IsEmpty).ToArray();
        LocalBounds = _rects.Length == 0 ? BinkRect.Empty : _rects.Aggregate(BinkRect.Empty, static (a, b) => a.Union(b));
    }

    /// <summary>Creates a highlight over one rectangle.</summary>
    public BinkHighlightElement(BinkRect rect) : this([rect]) { }

    /// <summary>Highlighted rectangles in local coordinates.</summary>
    public IReadOnlyList<BinkRect> Rects => _rects;

    /// <summary>Highlight color (yellow by default).</summary>
    public BinkColor Color { get; init; } = new(255, 235, 59);

    /// <summary>Opacity 0..1.</summary>
    public float Opacity { get; init; } = 0.5f;

    /// <inheritdoc />
    public override string TypeName => "Highlight";

    /// <inheritdoc />
    public override BinkRect LocalBounds { get; }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        if (_rects.Length == 0) return;
        var b = new BinkPathBuilder(_rects.Length * 5);
        foreach (BinkRect r in _rects) b.AddRect(r);
        context.FillPath(b.ToGeometry(), new BinkFillPaint(Color, Opacity, BinkBlendMode.Multiply));
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        foreach (BinkRect r in _rects) if (r.Inflate(tolerance).Contains(localPoint)) return true;
        return false;
    }
}

/// <summary>
/// A callout: a text box with a pointer line to an anchor. Used for review comments and explanatory labels.
/// </summary>
public sealed record BinkCalloutElement : BinkElement
{
    /// <summary>Creates a callout.</summary>
    /// <param name="box">Text box rectangle in local coordinates.</param>
    /// <param name="anchor">Point the callout points to.</param>
    /// <param name="text">Text content.</param>
    public BinkCalloutElement(BinkRect box, Vector2 anchor, string text)
    {
        Box = box.Normalize();
        Anchor = anchor;
        Text = text;
    }

    private readonly string _text = string.Empty;

    /// <summary>Text box rectangle.</summary>
    public BinkRect Box { get; init; }

    /// <summary>Anchor point the pointer leads to.</summary>
    public Vector2 Anchor { get; init; }

    /// <summary>Text content.</summary>
    /// <remarks>Validated on every assignment, including <c>with</c> expressions, as <see cref="BinkTextElement.Text"/> is.</remarks>
    public string Text
    {
        get => _text;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > BinkTextElement.MaxLength) throw new ArgumentException("Text is too long.", nameof(value));
            _text = value;
        }
    }

    /// <summary>Text style.</summary>
    public BinkTextStyle TextStyle { get; init; } = BinkTextStyle.Default with { FontSize = 14f };

    /// <summary>Box and pointer style. The fill defaults to a light yellow.</summary>
    public BinkShapeStyle Style { get; init; } = new() { StrokeColor = new BinkColor(120, 90, 0), StrokeWidth = 1.5f, FillColor = new BinkColor(255, 249, 196) };

    /// <summary>Padding between box and text.</summary>
    public float Padding { get; init; } = 6f;

    /// <inheritdoc />
    public override string TypeName => "Callout";

    /// <inheritdoc />
    public override BinkRect LocalBounds => Box.Union(Anchor).Inflate(Style.StrokeWidth + 1f);

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        // Pointer from the nearest box edge point to the anchor.
        Vector2 from = ClosestEdgePoint(Box, Anchor);
        var pointer = new BinkPathBuilder(4).MoveTo(from).LineTo(Anchor).ToGeometry();
        context.StrokePath(pointer, Style.ToStrokePaint());

        var box = new BinkPathBuilder(12).AddRoundedRect(Box, 4f).ToGeometry();
        if (Style.FillColor is { } fill) context.FillPath(box, new BinkFillPaint(fill, Style.Opacity, Style.BlendMode));
        if (Style.StrokeWidth > 0) context.StrokePath(box, Style.ToStrokePaint());

        if (Text.Length > 0)
        {
            context.Save();
            context.ClipRect(Box);
            context.DrawText(Text, new Vector2(Box.Left + Padding, Box.Top + Padding), TextStyle, Style.Opacity);
            context.Restore();
        }
    }

    private static Vector2 ClosestEdgePoint(BinkRect r, Vector2 p)
    {
        Vector2 c = r.Center;
        Vector2 d = p - c;
        if (d == Vector2.Zero) return c;
        float sx = MathF.Abs(d.X) > BinkGeometryMath.Epsilon ? (r.Width / 2f) / MathF.Abs(d.X) : float.MaxValue;
        float sy = MathF.Abs(d.Y) > BinkGeometryMath.Epsilon ? (r.Height / 2f) / MathF.Abs(d.Y) : float.MaxValue;
        float s = MathF.Min(sx, sy);
        return s >= 1f ? p : c + d * s;
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        if (Box.Inflate(tolerance).Contains(localPoint)) return true;
        float radius = MathF.Max(Style.StrokeWidth / 2f, 2f) + tolerance;
        return BinkGeometryMath.DistanceToSegmentSquared(localPoint, ClosestEdgePoint(Box, Anchor), Anchor) <= radius * radius;
    }

    /// <inheritdoc />
    public override string GetAccessibleDescription() => Description ?? "Callout: " + Text;
}

/// <summary>
/// An element of a type this version of the library does not know. It is preserved verbatim through load/save so
/// documents created by newer versions or with custom element types are never silently damaged. It draws nothing.
/// </summary>
public sealed record BinkUnknownElement : BinkElement
{
    /// <summary>Creates an unknown element.</summary>
    /// <param name="typeName">The serialized type discriminator.</param>
    /// <param name="rawJson">The element's original JSON object.</param>
    /// <param name="bounds">Bounds recorded in the document, if any.</param>
    public BinkUnknownElement(string typeName, string rawJson, BinkRect bounds)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(rawJson);
        SerializedTypeName = typeName;
        RawJson = rawJson;
        LocalBounds = bounds;
    }

    /// <summary>The type discriminator found in the document.</summary>
    public string SerializedTypeName { get; }

    /// <summary>The original JSON.</summary>
    public string RawJson { get; }

    /// <inheritdoc />
    public override string TypeName => "Unknown (" + SerializedTypeName + ")";

    /// <inheritdoc />
    public override bool IsSelectable => false;

    /// <inheritdoc />
    public override BinkRect LocalBounds { get; }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context) { }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => false;
}
