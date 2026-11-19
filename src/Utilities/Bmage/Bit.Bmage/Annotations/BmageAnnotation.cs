using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// An editable overlay object living in image coordinates. Annotations are never painted into the pixels until export,
/// so they can be selected, moved, restyled, reordered and deleted at any time.
/// </summary>
/// <remarks>
/// Annotations are immutable records: editing produces a new instance, which makes undo/redo and change tracking trivial.
/// Custom annotation types are supported by deriving from this class and implementing <see cref="Render"/> and
/// <see cref="GetBounds"/>; the editor will move, select and persist them like the built-in kinds.
/// </remarks>
public abstract record BmageAnnotation
{
    /// <summary>Stable identifier, generated when the annotation is created.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Draw order. Higher values are drawn on top; ties fall back to insertion order.</summary>
    public int ZIndex { get; init; }

    /// <summary>Rotation in degrees applied around the annotation's centre.</summary>
    public float Rotation { get; init; }

    /// <summary>Overall opacity in [0,1].</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Whether the annotation is drawn. Hidden annotations are still selectable in the editor's layer list.</summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>Whether the editor should prevent the user from moving or editing this annotation.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Optional caller data. Not interpreted by the library.</summary>
    public string? Tag { get; init; }

    /// <summary>A short type name for UI and serialisation, e.g. "Arrow".</summary>
    public abstract string Kind { get; }

    /// <summary>The axis-aligned bounds in image coordinates, ignoring <see cref="Rotation"/>.</summary>
    public abstract RectangleF GetBounds();

    /// <summary>The bounds after rotation, which is what hit-testing and selection handles should use.</summary>
    public RectangleF GetRotatedBounds()
    {
        var b = GetBounds();
        if (Rotation == 0) return b;
        var m = GetTransform();
        Span<Vector2> corners =
        [
            Vector2.Transform(new Vector2(b.Left, b.Top), m),
            Vector2.Transform(new Vector2(b.Right, b.Top), m),
            Vector2.Transform(new Vector2(b.Right, b.Bottom), m),
            Vector2.Transform(new Vector2(b.Left, b.Bottom), m),
        ];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var c in corners)
        {
            minX = MathF.Min(minX, c.X); minY = MathF.Min(minY, c.Y);
            maxX = MathF.Max(maxX, c.X); maxY = MathF.Max(maxY, c.Y);
        }
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    /// <summary>The rotation transform around the annotation's centre.</summary>
    public Matrix3x2 GetTransform()
    {
        if (Rotation == 0) return Matrix3x2.Identity;
        var b = GetBounds();
        var center = new Vector2(b.X + b.Width / 2, b.Y + b.Height / 2);
        return Matrix3x2.CreateRotation(Rotation * MathF.PI / 180f, center);
    }

    /// <summary>Draws the annotation onto a canvas whose coordinate space is image pixels.</summary>
    public abstract void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context);

    /// <summary>Returns a copy moved by the given offset.</summary>
    public abstract BmageAnnotation Translate(float dx, float dy);

    /// <summary>Returns a copy scaled for a proxy image of a different resolution.</summary>
    public abstract BmageAnnotation ForScale(double scale);

    /// <summary>
    /// Returns the annotation as it belongs after the image beneath it is transformed by <paramref name="matrix"/>, which
    /// maps the image's coordinates before the edit to those after it: a crop, a quarter turn, a flip, a resize, a
    /// straighten. The annotation stays on the content it marks.
    /// </summary>
    /// <remarks>
    /// By default the annotation's centre follows the image, its size scales by the transform's average scale and its
    /// <see cref="Rotation"/> turns with the image. A mirror moves the annotation to the mirrored place without mirroring
    /// how it is drawn, so text stays readable. The built-in annotations made of points map every point exactly, mirrors
    /// included, and those made of a rectangle map it exactly whenever the transform keeps rectangles upright. A custom
    /// annotation gets the default from its <see cref="ForScale"/> and <see cref="Translate"/>.
    /// </remarks>
    public virtual BmageAnnotation Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return this;
        var parts = BmageAffineParts.Of(matrix);
        var target = Vector2.Transform(Centre(GetBounds()), matrix);
        var scaled = MathF.Abs(parts.Scale - 1) < 1e-6f ? this : ForScale(parts.Scale);
        var centre = Centre(scaled.GetBounds());
        return scaled.Translate(target.X - centre.X, target.Y - centre.Y) with { Rotation = parts.RotationOf(Rotation) };
    }

    /// <summary>The annotation's own rotation followed by <paramref name="matrix"/>: where its unrotated geometry ends up.</summary>
    protected Matrix3x2 Through(Matrix3x2 matrix) => GetTransform() * matrix;

    /// <summary>The average scale of a transform, by which widths and sizes scale.</summary>
    protected static float ScaleOf(Matrix3x2 matrix) => BmageAffineParts.Of(matrix).Scale;

    /// <summary>
    /// True when the annotation is upright and the transform keeps rectangles upright, so its rectangle can be mapped
    /// exactly, quarter turns and mirrors included.
    /// </summary>
    protected bool MapsUpright(Matrix3x2 matrix) => Rotation == 0 && BmageAffineParts.PreservesAxes(matrix);

    /// <summary>The rectangle an upright transform maps <paramref name="rectangle"/> to.</summary>
    protected static RectangleF MapUpright(RectangleF rectangle, Matrix3x2 matrix) => BmageAffineParts.MapRectangle(rectangle, matrix);

    private static Vector2 Centre(RectangleF r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    /// <summary>True when the point (in image coordinates) is on the annotation.</summary>
    public virtual bool HitTest(Vector2 point, float tolerance = 4f)
    {
        var local = ToLocal(point);
        var b = GetBounds();
        b.Inflate(tolerance, tolerance);
        return b.Contains(local.X, local.Y);
    }

    /// <summary>
    /// Maps a point in image coordinates into the annotation's unrotated geometry, which is where hit tests must be
    /// evaluated because rendering applies <see cref="Rotation"/> on top of that geometry.
    /// </summary>
    protected Vector2 ToLocal(Vector2 point)
        => Rotation != 0 && Matrix3x2.Invert(GetTransform(), out var inverse) ? Vector2.Transform(point, inverse) : point;

    /// <summary>Renders the annotation with its rotation and opacity applied.</summary>
    internal void RenderWithTransform(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (!IsVisible || Opacity <= 0) return;
        canvas.Save();
        try
        {
            if (Rotation != 0) canvas.Transform = GetTransform() * canvas.Transform;
            canvas.Opacity *= Opacity;
            Render(canvas, context);
        }
        finally
        {
            canvas.Restore();
        }
    }
}
