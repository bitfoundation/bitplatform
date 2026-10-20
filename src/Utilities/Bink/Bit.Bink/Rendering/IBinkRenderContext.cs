using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// The drawing surface abstraction every element renders to. Implementations exist for the browser canvas
/// (command buffer), SVG output and a pure .NET rasterizer, which guarantees that what is drawn on screen
/// is what is exported.
/// </summary>
/// <remarks>
/// Coordinates are in document units; the host applies the viewport transform before rendering starts.
/// Implementations must support nested <see cref="Save"/>/<see cref="Restore"/> and <see cref="PushGroup"/>/<see cref="PopGroup"/>.
/// </remarks>
public interface IBinkRenderContext
{
    /// <summary>Pushes the current transform and clip.</summary>
    void Save();

    /// <summary>Pops the transform and clip saved by the matching <see cref="Save"/>.</summary>
    void Restore();

    /// <summary>Pre-multiplies the current transform with <paramref name="matrix"/> (drawing after this call is transformed by it).</summary>
    void Transform(Matrix3x2 matrix);

    /// <summary>Intersects the current clip with a rectangle in the current coordinate space.</summary>
    void ClipRect(BinkRect rect);

    /// <summary>Fills a path.</summary>
    void FillPath(BinkPathGeometry path, in BinkFillPaint paint);

    /// <summary>Strokes a path.</summary>
    void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint);

    /// <summary>Draws an image scaled into <paramref name="destination"/>.</summary>
    /// <param name="image">Image source.</param>
    /// <param name="destination">Destination rectangle in the current coordinate space.</param>
    /// <param name="sourceRect">Optional crop rectangle in image pixels; null draws the whole image.</param>
    /// <param name="opacity">Opacity 0..1.</param>
    void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity);

    /// <summary>Draws (possibly multi-line) text with its top-left corner (or top-center/top-right depending on alignment) at <paramref name="origin"/>.</summary>
    /// <param name="text">Text; line breaks split lines.</param>
    /// <param name="origin">Anchor point in the current coordinate space.</param>
    /// <param name="style">Font and color.</param>
    /// <param name="opacity">Opacity 0..1.</param>
    void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity);

    /// <summary>
    /// Starts an isolated compositing group. Everything drawn until <see cref="PopGroup"/> is composited as one unit
    /// with the given opacity and blend mode. Used for layer opacity and to confine <see cref="BinkBlendMode.DestinationOut"/>
    /// erasing to a layer.
    /// </summary>
    /// <remarks>
    /// Implementations must isolate even when <paramref name="opacity"/> is 1 and <paramref name="blend"/> is
    /// <see cref="BinkBlendMode.Normal"/>: callers open a group precisely when they need isolation, and content inside
    /// it may erase, which must not reach what was drawn before the group.
    /// </remarks>
    /// <param name="opacity">Opacity applied to the finished group.</param>
    /// <param name="blend">Blend mode used to composite the finished group.</param>
    void PushGroup(float opacity, BinkBlendMode blend);

    /// <summary>Ends the group started by <see cref="PushGroup"/>.</summary>
    void PopGroup();
}

/// <summary>
/// Optional capability of a render context: whether it can accept a cached representation for repeated paths. Hosts may
/// use it to skip regenerating geometry for unchanged elements.
/// </summary>
public interface IBinkRenderContextCapabilities
{
    /// <summary>The context renders text with real fonts. When false, text elements draw a placeholder outline only.</summary>
    bool SupportsText { get; }

    /// <summary>The context can decode and draw images. When false, image elements draw a placeholder outline only.</summary>
    bool SupportsImages { get; }
}
