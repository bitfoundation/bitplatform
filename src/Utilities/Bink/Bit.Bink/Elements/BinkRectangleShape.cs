using System.Numerics;

namespace Bit.Bink;

/// <summary>A rectangle annotation, optionally with rounded corners.</summary>
public sealed record BinkRectangleShape : BinkShapeElement
{
    private BinkCachedPath<(BinkRect, float)>? _path;

    /// <summary>Creates a rectangle.</summary>
    public BinkRectangleShape(BinkRect rect) { Rect = rect.Normalize(); }

    /// <summary>Geometry in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <summary>Corner radius in local units.</summary>
    public float CornerRadius { get; init; }

    /// <inheritdoc />
    public override string TypeName => "Rectangle";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => Rect;

    /// <inheritdoc />
    /// <remarks>
    /// Built once per geometry and reused by every redraw, hit test and export. The canvas keeps a drawn path in
    /// the browser and refers to it by identity, so a shape that rebuilt its path on every draw would resend it
    /// every frame; moving the shape changes only its transform and keeps the path.
    /// </remarks>
    public override BinkPathGeometry GetPath()
    {
        (BinkRect, float) key = (Rect, CornerRadius);
        if (_path is { } cached && cached.Matches(key)) return cached.Path;
        BinkPathGeometry path = CornerRadius > 0 ? new BinkPathBuilder(12).AddRoundedRect(Rect, CornerRadius).ToGeometry() : new BinkPathBuilder(6).AddRect(Rect).ToGeometry();
        _path = new(key, path);
        return path;
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}
