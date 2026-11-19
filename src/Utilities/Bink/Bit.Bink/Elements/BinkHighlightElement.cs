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
        context.FillPath(Path, new BinkFillPaint(Color, Opacity, BinkBlendMode.Multiply));
    }

    // The rectangles are fixed at construction and a with expression copies them, so one path serves every
    // version of the highlight: changing its colour or opacity, or moving it, keeps the path the canvas holds.
    private BinkPathGeometry Path
    {
        get
        {
            if (_path is { } path) return path;
            var b = new BinkPathBuilder(_rects.Length * 5);
            foreach (BinkRect r in _rects) b.AddRect(r);
            return _path = b.ToGeometry();
        }
    }

    private BinkPathGeometry? _path;

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        foreach (BinkRect r in _rects) if (r.Inflate(tolerance).Contains(localPoint)) return true;
        return false;
    }
}
