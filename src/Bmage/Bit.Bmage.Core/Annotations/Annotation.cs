using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;

namespace Bit.Bmage.Annotations;

/// <summary>
/// An editable overlay object living in image coordinates. Annotations are never painted into the pixels until export,
/// so they can be selected, moved, restyled, reordered and deleted at any time.
/// </summary>
/// <remarks>
/// Annotations are immutable records: editing produces a new instance, which makes undo/redo and change tracking trivial.
/// Custom annotation types are supported by deriving from this class and implementing <see cref="Render"/> and
/// <see cref="GetBounds"/>; the editor will move, select and persist them like the built-in kinds.
/// </remarks>
public abstract record Annotation
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
    public abstract void Render(ImageCanvas canvas, AnnotationRenderContext context);

    /// <summary>Returns a copy moved by the given offset.</summary>
    public abstract Annotation Translate(float dx, float dy);

    /// <summary>Returns a copy scaled for a proxy image of a different resolution.</summary>
    public abstract Annotation ForScale(double scale);

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
    internal void RenderWithTransform(ImageCanvas canvas, AnnotationRenderContext context)
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

/// <summary>Services available while rendering annotations.</summary>
public sealed class AnnotationRenderContext
{
    public AnnotationRenderContext(ITextRasterizer? textRasterizer = null, double scale = 1.0)
    {
        TextRasterizer = textRasterizer;
        Scale = scale;
    }

    /// <summary>Rasteriser used for text annotations. Text is skipped when this is null.</summary>
    public ITextRasterizer? TextRasterizer { get; }

    /// <summary>
    /// The scale of the surface relative to the original image. Annotations are pre-scaled by the renderer, so this is
    /// informational (useful for choosing detail levels).
    /// </summary>
    public double Scale { get; }
}

/// <summary>Shared styling for shape annotations.</summary>
public sealed record ShapeStyle
{
    public static ShapeStyle Default { get; } = new();

    /// <summary>Outline colour, or null for no outline.</summary>
    public Rgba32? StrokeColor { get; init; } = new Rgba32(230, 40, 40);

    /// <summary>Interior colour, or null for no fill.</summary>
    public Rgba32? FillColor { get; init; }

    /// <summary>Outline width and dashes.</summary>
    public StrokeStyle Stroke { get; init; } = new() { Width = 3f };

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public ShapeStyle ForScale(double scale) => scale == 1 ? this : this with { Stroke = Stroke.ForScale(scale) };
}
