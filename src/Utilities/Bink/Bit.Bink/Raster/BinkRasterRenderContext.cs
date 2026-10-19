using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Renders text for the pure .NET rasterizer. The library has no font engine, so applications that need text in
/// server-side PNG export supply one (backed by SkiaSharp, ImageSharp or a bitmap font).
/// </summary>
public interface IBinkTextRasterizer
{
    /// <summary>
    /// Draws text into the surface. Coordinates are in device pixels and the transform has already been applied
    /// to <paramref name="origin"/>; <paramref name="scale"/> is the current transform's scale factor.
    /// </summary>
    void DrawText(BinkRasterSurface surface, string text, Vector2 origin, in BinkTextStyle style, float scale, float opacity, BinkRect clip);

    /// <summary>Measures a single line of text in style units, or null when the implementation cannot measure.</summary>
    Vector2? Measure(string text, in BinkTextStyle style);
}

/// <summary>
/// An <see cref="IBinkRenderContext"/> that draws into a <see cref="BinkRasterSurface"/> using the built-in rasterizer.
/// Runs anywhere .NET runs, including on a server and in WebAssembly, with no native dependencies.
/// </summary>
/// <remarks>
/// Text is only drawn when an <see cref="IBinkTextRasterizer"/> is supplied, because the library deliberately ships no
/// font engine. Images are drawn when they are embedded PNG data URIs; other formats need the browser exporter.
/// Anything skipped is reported through <see cref="SkippedText"/> and <see cref="SkippedImages"/> so callers can
/// tell the user instead of silently producing a wrong image.
/// </remarks>
public sealed class BinkRasterRenderContext : IBinkRenderContext, IBinkRenderContextCapabilities
{
    private readonly BinkRasterSurface _root;
    private readonly BinkRasterizer _rasterizer = new();
    private readonly List<Vector2[]> _strokePolygons = new(64);
    private readonly Stack<State> _states = new();
    private readonly Stack<GroupFrame> _groups = new();
    private readonly Dictionary<string, BinkPngCodec.DecodedImage?> _imageCache = new(StringComparer.Ordinal);
    private State _state;
    private BinkRasterSurface _target;

    private readonly record struct State(Matrix3x2 Transform, BinkRect Clip);

    /// <summary>
    /// A group also remembers how deep the transform and clip stack was when it opened. Restoring to that depth
    /// on close stops an unbalanced Save inside the group from leaking its clip into what follows.
    /// </summary>
    private readonly record struct GroupFrame(BinkRasterSurface Surface, float Opacity, BinkBlendMode Blend, BinkRect Clip, State State, int StateDepth);

    /// <summary>Creates a context drawing into a surface.</summary>
    /// <param name="surface">Target surface.</param>
    /// <param name="transform">Document-to-device transform (usually scale by device pixel ratio and translate by the export origin).</param>
    public BinkRasterRenderContext(BinkRasterSurface surface, Matrix3x2 transform)
    {
        ArgumentNullException.ThrowIfNull(surface);
        _root = surface;
        _target = surface;
        _state = new State(transform, surface.Bounds);
    }

    /// <summary>Optional text renderer. When null, text elements are skipped and counted in <see cref="SkippedText"/>.</summary>
    public IBinkTextRasterizer? TextRasterizer { get; init; }

    /// <summary>Flattening tolerance in device pixels. Smaller values give smoother curves at more cost.</summary>
    public float Tolerance { get; init; } = 0.2f;

    /// <summary>Number of text elements that could not be drawn.</summary>
    public int SkippedText { get; private set; }

    /// <summary>Number of images that could not be decoded or drawn.</summary>
    public int SkippedImages { get; private set; }

    /// <inheritdoc />
    public bool SupportsText => TextRasterizer is not null;

    /// <inheritdoc />
    public bool SupportsImages => true;

    /// <inheritdoc />
    public void Save() => _states.Push(_state);

    /// <inheritdoc />
    public void Restore()
    {
        // A Restore must not unwind past the group it is inside; that state belongs to the caller of PushGroup.
        int floor = _groups.Count > 0 ? _groups.Peek().StateDepth : 0;
        if (_states.Count > floor) _state = _states.Pop();
    }

    /// <inheritdoc />
    public void Transform(Matrix3x2 matrix) => _state = _state with { Transform = matrix * _state.Transform };

    /// <inheritdoc />
    public void ClipRect(BinkRect rect) => _state = _state with { Clip = _state.Clip.Intersect(rect.Transform(_state.Transform)) };

    /// <inheritdoc />
    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || _state.Clip.IsEmpty) return;
        if (paint.EffectiveAlpha <= 0 && paint.Blend != BinkBlendMode.DestinationOut) return;

        _rasterizer.Reset();
        _rasterizer.AddPath(path, _state.Transform, Tolerance);
        BinkRasterSurface target = _target;
        BinkColor color = paint.Color;
        float opacity = paint.Opacity;
        BinkBlendMode blend = paint.Blend;
        _rasterizer.Rasterize(_state.Clip, paint.Rule, (y, x, coverage) => target.Blend(y, x, coverage, color, opacity, blend));
    }

    /// <inheritdoc />
    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || paint.Width <= 0 || _state.Clip.IsEmpty) return;
        if (paint.EffectiveAlpha <= 0 && paint.Blend != BinkBlendMode.DestinationOut) return;

        // Stroking happens in device space so the line width scales with the transform.
        BinkPathGeometry device = path.Transform(_state.Transform);
        float scale = MathF.Sqrt(MathF.Abs(_state.Transform.GetDeterminant()));
        if (!float.IsFinite(scale) || scale <= 0) scale = 1f;
        float width = MathF.Max(paint.Width * scale, 0.35f);
        float[]? dash = null;
        if (paint.Dash is { Length: > 0 } source)
        {
            dash = new float[source.Length];
            for (int i = 0; i < source.Length; i++) dash[i] = MathF.Max(source[i] * scale, 0.05f);
        }

        // Only the clip can be seen, so a dash pattern is only cut there: a huge shape with a tiny dash would
        // otherwise be cut into an unbounded number of pieces.
        BinkPathStroker.Stroke(device, width, paint.Cap, paint.Join, Tolerance, dash, _strokePolygons, _state.Clip);
        if (_strokePolygons.Count == 0) return;

        _rasterizer.Reset();
        _rasterizer.AddPolygons(_strokePolygons);
        BinkRasterSurface target = _target;
        BinkColor color = paint.Color;
        float opacity = paint.Opacity;
        BinkBlendMode blend = paint.Blend;
        _rasterizer.Rasterize(_state.Clip, BinkFillRule.NonZero, (y, x, coverage) => target.Blend(y, x, coverage, color, opacity, blend));
    }

    /// <inheritdoc />
    public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (destination.IsEmpty || opacity <= 0 || _state.Clip.IsEmpty || image.IsOmitted) return;

        BinkPngCodec.DecodedImage? decoded = GetImage(image);
        if (decoded is null)
        {
            SkippedImages++;
            return;
        }

        BinkPngCodec.DecodedImage source = decoded.Value;
        BinkRect crop = sourceRect ?? new BinkRect(0, 0, source.Width, source.Height);
        if (crop.Width <= 0 || crop.Height <= 0) return;

        // Map each destination device pixel back to a source pixel (bilinear), which handles rotation and scale.
        BinkRect deviceRect = destination.Transform(_state.Transform).Intersect(_state.Clip);
        if (deviceRect.IsEmpty) return;
        if (!Matrix3x2.Invert(_state.Transform, out Matrix3x2 inverse)) return;

        int x0 = Math.Max((int)MathF.Floor(deviceRect.Left), 0);
        int y0 = Math.Max((int)MathF.Floor(deviceRect.Top), 0);
        int x1 = Math.Min((int)MathF.Ceiling(deviceRect.Right), _target.Width);
        int y1 = Math.Min((int)MathF.Ceiling(deviceRect.Bottom), _target.Height);

        float scaleX = crop.Width / destination.Width;
        float scaleY = crop.Height / destination.Height;

        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                Vector2 document = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), inverse);
                if (!destination.Contains(document)) continue;
                float u = crop.X + (document.X - destination.X) * scaleX;
                float v = crop.Y + (document.Y - destination.Y) * scaleY;
                (float r, float g, float b, float a) = SampleBilinear(source, u - 0.5f, v - 0.5f);
                if (a <= 0.002f) continue;
                // Already premultiplied by the sampler; opacity scales colour and alpha together.
                _target.BlendPixel(x, y, r * opacity, g * opacity, b * opacity, a * opacity, BinkBlendMode.Normal);
            }
        }
    }

    /// <summary>
    /// Bilinear sample returning premultiplied colour. Interpolating straight colour would let a fully
    /// transparent pixel contribute its stored colour, which shows up as a dark or coloured fringe on the edges
    /// of an image with transparency.
    /// </summary>
    private static (float R, float G, float B, float A) SampleBilinear(BinkPngCodec.DecodedImage image, float x, float y)
    {
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        float fx = x - x0;
        float fy = y - y0;
        (float r, float g, float b, float a) c00 = Sample(image, x0, y0);
        (float r, float g, float b, float a) c10 = Sample(image, x0 + 1, y0);
        (float r, float g, float b, float a) c01 = Sample(image, x0, y0 + 1);
        (float r, float g, float b, float a) c11 = Sample(image, x0 + 1, y0 + 1);
        float w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
        return (
            c00.r * w00 + c10.r * w10 + c01.r * w01 + c11.r * w11,
            c00.g * w00 + c10.g * w10 + c01.g * w01 + c11.g * w11,
            c00.b * w00 + c10.b * w10 + c01.b * w01 + c11.b * w11,
            c00.a * w00 + c10.a * w10 + c01.a * w01 + c11.a * w11);
    }

    private static (float R, float G, float B, float A) Sample(BinkPngCodec.DecodedImage image, int x, int y)
    {
        x = Math.Clamp(x, 0, image.Width - 1);
        y = Math.Clamp(y, 0, image.Height - 1);
        int offset = (y * image.Width + x) * 4;
        float a = image.Rgba[offset + 3] / 255f;
        return (image.Rgba[offset] / 255f * a, image.Rgba[offset + 1] / 255f * a, image.Rgba[offset + 2] / 255f * a, a);
    }

    private BinkPngCodec.DecodedImage? GetImage(BinkImageSource image)
    {
        if (_imageCache.TryGetValue(image.Uri, out BinkPngCodec.DecodedImage? cached)) return cached;
        BinkPngCodec.DecodedImage? result = null;
        if (image.TryGetBytes(out byte[] bytes) && BinkPngCodec.IsPng(bytes))
        {
            try
            {
                result = BinkPngCodec.Decode(bytes);
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException or ArgumentException or OutOfMemoryException)
            {
                // A document may embed any bytes at all. An undecodable image is reported through SkippedImages,
                // never by aborting the export.
                result = null;
            }
        }
        _imageCache[image.Uri] = result;
        return result;
    }

    /// <inheritdoc />
    public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (TextRasterizer is null)
        {
            SkippedText++;
            return;
        }
        float scale = MathF.Sqrt(MathF.Abs(_state.Transform.GetDeterminant()));
        if (!float.IsFinite(scale) || scale <= 0) scale = 1f;
        TextRasterizer.DrawText(_target, text, Vector2.Transform(origin, _state.Transform), style, scale, opacity, _state.Clip);
    }

    /// <inheritdoc />
    public void PushGroup(float opacity, BinkBlendMode blend)
    {
        // Always isolate. Callers only open a group when they need it, and skipping the buffer here would let an
        // eraser stroke inside the group erase the background and the layers below it.
        var surface = new BinkRasterSurface(_root.Width, _root.Height);
        _groups.Push(new GroupFrame(_target, opacity, blend, _state.Clip, _state, _states.Count));
        _target = surface;
    }

    /// <inheritdoc />
    public void PopGroup()
    {
        if (_groups.Count == 0) return;
        GroupFrame frame = _groups.Pop();

        // Discard anything the group left on the state stack and restore the transform and clip it opened with,
        // so an element that forgot a Restore cannot affect what comes after the group.
        while (_states.Count > frame.StateDepth) _states.Pop();
        _state = frame.State;

        BinkRasterSurface group = _target;
        _target = frame.Surface;
        _target.Composite(group, frame.Opacity, frame.Blend, frame.Clip);
    }

    /// <summary>The surface everything is ultimately composited onto.</summary>
    public BinkRasterSurface Surface => _root;
}
