using System.Numerics;

namespace Bit.Bink.Demo.Client.Extending;

/// <summary>
/// A render target of the demo's own, and the smallest one there is: it draws nothing and counts what the renderer
/// asks for.
/// </summary>
/// <remarks>
/// <see cref="IBinkRenderContext"/> is the only surface the engine draws on; the browser canvas, SVG export and the
/// built-in rasterizer are three implementations of it. A fourth - a PDF writer, a printer, another graphics API - walks
/// the same calls. Two rules matter for a real one: <see cref="PushGroup"/> must genuinely isolate, because what is drawn
/// inside may erase, and fills must honour <see cref="BinkFillRule.NonZero"/>, which keeps a self-overlapping stroke
/// outline solid.
/// </remarks>
public sealed class DrawCallCounter : IBinkRenderContext
{
    /// <summary>Paths filled: stroke outlines, shape interiors.</summary>
    public int Fills { get; private set; }

    /// <summary>Paths stroked: centerline ink, shape outlines, lines.</summary>
    public int Strokes { get; private set; }

    /// <summary>Lines, curves and moves across every path filled or stroked.</summary>
    public long Segments { get; private set; }

    /// <summary>Strings drawn.</summary>
    public int Texts { get; private set; }

    /// <summary>Pictures drawn.</summary>
    public int Images { get; private set; }

    /// <summary>Isolated groups opened, for a layer with opacity or an eraser, or a blend mode.</summary>
    public int Groups { get; private set; }

    /// <summary>Transforms applied, one for each element moved, resized or rotated away from where it was drawn.</summary>
    public int Transforms { get; private set; }

    /// <inheritdoc />
    public void Save() { }

    /// <inheritdoc />
    public void Restore() { }

    /// <inheritdoc />
    public void Transform(Matrix3x2 matrix) => Transforms++;

    /// <inheritdoc />
    public void ClipRect(BinkRect rect) { }

    /// <inheritdoc />
    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        Fills++;
        Segments += path.VerbCount;
    }

    /// <inheritdoc />
    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        Strokes++;
        Segments += path.VerbCount;
    }

    /// <inheritdoc />
    public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity) => Images++;

    /// <inheritdoc />
    public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity) => Texts++;

    /// <inheritdoc />
    public void PushGroup(float opacity, BinkBlendMode blend) => Groups++;

    /// <inheritdoc />
    public void PopGroup() { }
}
