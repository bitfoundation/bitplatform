using System.Numerics;

namespace Bit.Bink;

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

    // Built once per geometry and reused by every redraw, so the canvas can keep them in the browser. A with
    // expression copies the cache, which is why it is keyed by what it was built from.
    private BinkCachedPath<(BinkRect, Vector2)>? _pointerPath;
    private BinkCachedPath<BinkRect>? _boxPath;

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        // Pointer from the nearest box edge point to the anchor.
        BinkPathGeometry pointer;
        if (_pointerPath is { } cachedPointer && cachedPointer.Matches((Box, Anchor))) pointer = cachedPointer.Path;
        else
        {
            pointer = new BinkPathBuilder(4).MoveTo(ClosestEdgePoint(Box, Anchor)).LineTo(Anchor).ToGeometry();
            _pointerPath = new((Box, Anchor), pointer);
        }
        context.StrokePath(pointer, Style.ToStrokePaint());

        BinkPathGeometry box;
        if (_boxPath is { } cachedBox && cachedBox.Matches(Box)) box = cachedBox.Path;
        else
        {
            box = new BinkPathBuilder(12).AddRoundedRect(Box, 4f).ToGeometry();
            _boxPath = new(Box, box);
        }
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
