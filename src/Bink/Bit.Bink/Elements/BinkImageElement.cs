using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A raster image placed in the document. Position, size, rotation (via <see cref="BinkElement.Transform"/>) and an
/// optional crop are vector properties, so images can be annotated, moved and exported without re-encoding pixels.
/// </summary>
public sealed record BinkImageElement : BinkElement
{
    /// <summary>Creates an image element.</summary>
    /// <param name="source">Image source.</param>
    /// <param name="rect">Destination rectangle in local coordinates.</param>
    public BinkImageElement(BinkImageSource source, BinkRect rect)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Rect = rect.Normalize();
    }

    /// <summary>The image.</summary>
    public BinkImageSource Source { get; init; }

    /// <summary>Destination rectangle in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <summary>Optional crop rectangle in source pixels. Null draws the whole image.</summary>
    public BinkRect? Crop { get; init; }

    /// <summary>Opacity 0..1.</summary>
    public float Opacity { get; init; } = 1f;

    /// <inheritdoc />
    public override string TypeName => "Image";

    /// <inheritdoc />
    public override BinkRect LocalBounds => Rect;

    /// <summary>Creates an image element at its natural size (or a fallback size when unknown) positioned at <paramref name="position"/>.</summary>
    public static BinkImageElement AtNaturalSize(BinkImageSource source, Vector2 position, float maxWidth = float.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(source);
        float w = source.Width > 0 ? source.Width : 200;
        float h = source.Height > 0 ? source.Height : 200;
        if (w > maxWidth && maxWidth > 0 && float.IsFinite(maxWidth))
        {
            float s = maxWidth / w;
            w *= s;
            h *= s;
        }
        return new BinkImageElement(source, new BinkRect(position.X, position.Y, w, h));
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context) => context.DrawImage(Source, Rect, Crop, Opacity);

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => Rect.Inflate(tolerance).Contains(localPoint);

    /// <inheritdoc />
    public override string GetAccessibleDescription() => Description ?? (Source.IsOmitted ? "Image without its picture" : Source.IsEmbedded ? "Embedded image" : "Image");
}
