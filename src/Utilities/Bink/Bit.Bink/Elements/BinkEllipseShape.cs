using System.Numerics;

namespace Bit.Bink;

/// <summary>An ellipse annotation inscribed in a rectangle.</summary>
public sealed record BinkEllipseShape : BinkShapeElement
{
    private BinkCachedPath<BinkRect>? _path;

    /// <summary>Creates an ellipse.</summary>
    public BinkEllipseShape(BinkRect rect) { Rect = rect.Normalize(); }

    /// <summary>Bounding rectangle in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <inheritdoc />
    public override string TypeName => "Ellipse";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => Rect;

    /// <inheritdoc />
    /// <remarks>Built once per geometry and reused; see <see cref="BinkRectangleShape.GetPath"/>.</remarks>
    public override BinkPathGeometry GetPath()
    {
        if (_path is { } cached && cached.Matches(Rect)) return cached.Path;
        BinkPathGeometry path = new BinkPathBuilder(8).AddEllipse(Rect).ToGeometry();
        _path = new(Rect, path);
        return path;
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}
