using System.Drawing;
using System.Numerics;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Drawing;

/// <summary>How a drawing operation combines with what is already on the canvas.</summary>
public enum BlendMode
{
    /// <summary>Standard alpha compositing.</summary>
    SourceOver = 0,
    /// <summary>Replaces the destination pixels, including alpha.</summary>
    Copy,
    /// <summary>Removes coverage from the destination, making it transparent (an eraser).</summary>
    Erase,
}

/// <summary>
/// A mutable drawing surface with an immediate-mode API: draw images, shapes, paths and text, then read the pixels or
/// export them. Independent of any UI, so it works headlessly on a server as well as in the browser.
/// </summary>
/// <example>
/// <code>
/// using var canvas = ImageCanvas.Create(1200, 630, Rgba32.White);
/// canvas.DrawImage(logo, new Rectangle(40, 40, 200, 200));
/// canvas.FillRectangle(new RectangleF(0, 560, 1200, 70), new Rgba32(0, 0, 0, 160));
/// var png = await canvas.ExportAsync(ImageExportOptions.Png);
/// </code>
/// </example>
public sealed class ImageCanvas : IDisposable
{
    private ImageBuffer _buffer;
    private readonly bool _ownsBuffer;
    private readonly Stack<CanvasState> _states = new();
    private CanvasState _state;

    private readonly record struct CanvasState(Matrix3x2 Transform, Rectangle Clip, float Opacity);

    private ImageCanvas(ImageBuffer buffer, bool ownsBuffer)
    {
        _buffer = buffer;
        _ownsBuffer = ownsBuffer;
        _state = new CanvasState(Matrix3x2.Identity, buffer.Bounds, 1f);
    }

    /// <summary>Creates a canvas of the given size filled with a colour (transparent by default).</summary>
    public static ImageCanvas Create(int width, int height, Rgba32 background = default, IPixelAllocator? allocator = null, ImageLimits? limits = null)
    {
        var buffer = ImageBuffer.Create(width, height, allocator, clear: true, limits);
        if (background.A != 0) buffer.Fill(background);
        return new ImageCanvas(buffer, ownsBuffer: true);
    }

    /// <summary>Creates a canvas that draws directly into an existing buffer. The buffer is not disposed by the canvas.</summary>
    public static ImageCanvas FromBuffer(ImageBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return new ImageCanvas(buffer, ownsBuffer: false);
    }

    /// <summary>Creates a canvas holding a copy of an existing image.</summary>
    public static ImageCanvas FromImage(ImageBuffer source, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new ImageCanvas(source.Clone(allocator), ownsBuffer: true);
    }

    /// <summary>The pixels being drawn into. Valid until the canvas is disposed.</summary>
    public ImageBuffer Buffer => _buffer;

    public int Width => _buffer.Width;
    public int Height => _buffer.Height;
    public Size Size => _buffer.Size;

    /// <summary>The transform applied to coordinates passed to drawing calls.</summary>
    public Matrix3x2 Transform
    {
        get => _state.Transform;
        set => _state = _state with { Transform = value };
    }

    /// <summary>The clip rectangle in device (pixel) coordinates. Drawing outside it is discarded.</summary>
    public Rectangle Clip
    {
        get => _state.Clip;
        set
        {
            var c = value;
            c.Intersect(_buffer.Bounds);
            _state = _state with { Clip = c };
        }
    }

    /// <summary>Global opacity multiplier in [0,1] applied to every drawing call.</summary>
    public float Opacity
    {
        get => _state.Opacity;
        set => _state = _state with { Opacity = Math.Clamp(value, 0f, 1f) };
    }

    /// <summary>Saves the transform, clip and opacity so they can be restored later.</summary>
    public ImageCanvas Save()
    {
        _states.Push(_state);
        return this;
    }

    /// <summary>Restores the state saved by the matching <see cref="Save"/>.</summary>
    public ImageCanvas Restore()
    {
        if (_states.Count > 0) _state = _states.Pop();
        return this;
    }

    /// <summary>Multiplies the current transform by a translation.</summary>
    public ImageCanvas Translate(float x, float y) { Transform = Matrix3x2.CreateTranslation(x, y) * Transform; return this; }

    /// <summary>Multiplies the current transform by a scale.</summary>
    public ImageCanvas Scale(float x, float y) { Transform = Matrix3x2.CreateScale(x, y) * Transform; return this; }

    /// <summary>Multiplies the current transform by a rotation in degrees around a point.</summary>
    public ImageCanvas Rotate(float degrees, Vector2 origin = default) { Transform = Matrix3x2.CreateRotation(degrees * MathF.PI / 180f, origin) * Transform; return this; }

    /// <summary>Intersects the clip with a rectangle in current-transform coordinates.</summary>
    public ImageCanvas ClipTo(RectangleF rect)
    {
        var corners = new[]
        {
            Vector2.Transform(new Vector2(rect.Left, rect.Top), _state.Transform),
            Vector2.Transform(new Vector2(rect.Right, rect.Top), _state.Transform),
            Vector2.Transform(new Vector2(rect.Right, rect.Bottom), _state.Transform),
            Vector2.Transform(new Vector2(rect.Left, rect.Bottom), _state.Transform),
        };
        var minX = corners.Min(c => c.X);
        var minY = corners.Min(c => c.Y);
        var maxX = corners.Max(c => c.X);
        var maxY = corners.Max(c => c.Y);
        var device = Rectangle.FromLTRB((int)Math.Floor(minX), (int)Math.Floor(minY), (int)Math.Ceiling(maxX), (int)Math.Ceiling(maxY));
        device.Intersect(_state.Clip);
        _state = _state with { Clip = device };
        return this;
    }

    /// <summary>Clears the whole canvas to a colour, replacing existing pixels.</summary>
    public ImageCanvas Clear(Rgba32 color = default)
    {
        _buffer.Fill(_state.Clip == _buffer.Bounds ? _buffer.Bounds : _state.Clip, color);
        return this;
    }

    // ---- Shapes ----

    /// <summary>Fills a path.</summary>
    public ImageCanvas FillPath(VectorPath path, Rgba32 color, FillRule rule = FillRule.NonZero)
    {
        Rasterizer.Fill(_buffer, TransformPath(path), color, rule, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Fills a path with a paint (gradient or custom).</summary>
    public ImageCanvas FillPath(VectorPath path, IPaint paint, FillRule rule = FillRule.NonZero)
    {
        Rasterizer.Fill(_buffer, TransformPath(path), paint, rule, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Strokes a path.</summary>
    public ImageCanvas StrokePath(VectorPath path, Rgba32 color, StrokeStyle? style = null)
    {
        style ??= StrokeStyle.Default;
        var outline = Stroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        Rasterizer.Fill(_buffer, outline, color, FillRule.NonZero, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Strokes a path with a paint.</summary>
    public ImageCanvas StrokePath(VectorPath path, IPaint paint, StrokeStyle? style = null)
    {
        style ??= StrokeStyle.Default;
        var outline = Stroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        Rasterizer.Fill(_buffer, outline, paint, FillRule.NonZero, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Removes coverage from the canvas along a stroked path, making those pixels transparent.</summary>
    public ImageCanvas ErasePath(VectorPath path, StrokeStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var outline = Stroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        var opacity = _state.Opacity;
        var clip = _state.Clip;
        Rasterizer.Rasterize(outline, clip, FillRule.NonZero, (y, x0, coverage) =>
        {
            var row = _buffer.GetRow(y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = coverage[i];
                if (a <= 0.0005f) continue;
                var x = x0 + i;
                if (x < clip.Left || x >= clip.Right) continue;
                row[x] = Compositor.Erase(row[x], MathF.Min(a, 1f) * opacity);
            }
        });
        return this;
    }

    /// <summary>Draws a straight line.</summary>
    public ImageCanvas DrawLine(Vector2 from, Vector2 to, Rgba32 color, StrokeStyle? style = null)
        => StrokePath(new PathBuilder().MoveTo(from).LineTo(to).Build(), color, style);

    /// <summary>Fills a rectangle.</summary>
    public ImageCanvas FillRectangle(RectangleF rect, Rgba32 color)
        => FillPath(new PathBuilder().AddRectangle(rect).Build(), color);

    /// <summary>Outlines a rectangle.</summary>
    public ImageCanvas DrawRectangle(RectangleF rect, Rgba32 color, StrokeStyle? style = null)
        => StrokePath(new PathBuilder().AddRectangle(rect).Build(), color, style);

    /// <summary>Fills a rounded rectangle.</summary>
    public ImageCanvas FillRoundedRectangle(RectangleF rect, float radius, Rgba32 color)
        => FillPath(new PathBuilder().AddRoundedRectangle(rect, radius).Build(), color);

    /// <summary>Outlines a rounded rectangle.</summary>
    public ImageCanvas DrawRoundedRectangle(RectangleF rect, float radius, Rgba32 color, StrokeStyle? style = null)
        => StrokePath(new PathBuilder().AddRoundedRectangle(rect, radius).Build(), color, style);

    /// <summary>Fills an ellipse inscribed in the rectangle.</summary>
    public ImageCanvas FillEllipse(RectangleF rect, Rgba32 color)
        => FillPath(new PathBuilder().AddEllipse(rect).Build(), color);

    /// <summary>Outlines an ellipse inscribed in the rectangle.</summary>
    public ImageCanvas DrawEllipse(RectangleF rect, Rgba32 color, StrokeStyle? style = null)
        => StrokePath(new PathBuilder().AddEllipse(rect).Build(), color, style);

    /// <summary>Fills a circle.</summary>
    public ImageCanvas FillCircle(Vector2 center, float radius, Rgba32 color)
        => FillEllipse(new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2), color);

    /// <summary>Fills a polygon.</summary>
    public ImageCanvas FillPolygon(IReadOnlyList<Vector2> points, Rgba32 color, FillRule rule = FillRule.NonZero)
        => FillPath(new PathBuilder().AddPolygon(points).Build(), color, rule);

    /// <summary>Draws a polyline.</summary>
    public ImageCanvas DrawPolyline(IReadOnlyList<Vector2> points, Rgba32 color, StrokeStyle? style = null)
        => StrokePath(new PathBuilder().AddPolyline(points).Build(), color, style);

    /// <summary>
    /// Draws an arrow from <paramref name="from"/> to <paramref name="to"/>: a shaft plus a filled triangular head
    /// sized relative to the stroke width.
    /// </summary>
    public ImageCanvas DrawArrow(Vector2 from, Vector2 to, Rgba32 color, StrokeStyle? style = null, float headLengthFactor = 4f, float headWidthFactor = 3f)
    {
        style ??= StrokeStyle.Default;
        var direction = to - from;
        var length = direction.Length();
        if (length < 1e-3f) return this;
        direction /= length;
        var headLength = MathF.Min(style.Width * headLengthFactor, length * 0.6f);
        var headWidth = style.Width * headWidthFactor;
        var shaftEnd = to - direction * headLength * 0.85f;
        var normal = new Vector2(-direction.Y, direction.X);
        StrokePath(new PathBuilder().MoveTo(from).LineTo(shaftEnd).Build(), color, style with { Cap = LineCap.Round });
        var head = new PathBuilder()
            .MoveTo(to)
            .LineTo(to - direction * headLength + normal * headWidth)
            .LineTo(to - direction * headLength - normal * headWidth)
            .Close()
            .Build();
        return FillPath(head, color);
    }

    // ---- Images ----

    /// <summary>Draws an image at a position, one canvas pixel per source pixel.</summary>
    public ImageCanvas DrawImage(ImageBuffer image, Vector2 position, BlendMode blend = BlendMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(image);
        return DrawImage(image, new RectangleF(position.X, position.Y, image.Width, image.Height), blend);
    }

    /// <summary>Draws an image scaled into a destination rectangle.</summary>
    public ImageCanvas DrawImage(ImageBuffer image, RectangleF destination, BlendMode blend = BlendMode.SourceOver, ResamplingFilter filter = ResamplingFilter.Auto)
        => DrawImage(image, image.Bounds, destination, blend, filter);

    /// <summary>Draws part of an image into a destination rectangle.</summary>
    public ImageCanvas DrawImage(ImageBuffer image, Rectangle source, RectangleF destination, BlendMode blend = BlendMode.SourceOver, ResamplingFilter filter = ResamplingFilter.Auto)
    {
        ArgumentNullException.ThrowIfNull(image);
        source.Intersect(image.Bounds);
        if (source.IsEmptyArea() || destination.Width <= 0 || destination.Height <= 0) return this;

        // Map the destination through the current transform. Axis-aligned transforms use the fast resampling path.
        var m = _state.Transform;
        var isAxisAligned = MathF.Abs(m.M12) < 1e-6f && MathF.Abs(m.M21) < 1e-6f && m.M11 > 0 && m.M22 > 0;
        if (isAxisAligned)
        {
            var topLeft = Vector2.Transform(new Vector2(destination.Left, destination.Top), m);
            var bottomRight = Vector2.Transform(new Vector2(destination.Right, destination.Bottom), m);
            var device = Rectangle.FromLTRB(
                (int)MathF.Round(topLeft.X), (int)MathF.Round(topLeft.Y),
                (int)MathF.Round(bottomRight.X), (int)MathF.Round(bottomRight.Y));
            if (device.Width <= 0 || device.Height <= 0) return this;
            DrawImageAxisAligned(image, source, device, blend, filter);
            return this;
        }
        return DrawImageTransformed(image, source, destination, blend, filter);
    }

    private void DrawImageAxisAligned(ImageBuffer image, Rectangle source, Rectangle device, BlendMode blend, ResamplingFilter filter)
    {
        var clip = _state.Clip;
        var visible = device;
        visible.Intersect(clip);
        if (visible.IsEmptyArea()) return;

        var sameSize = device.Size == source.Size;
        ImageBuffer scaled;
        Rectangle scaledSource;
        if (sameSize)
        {
            scaled = image;
            scaledSource = new Rectangle(source.X + (visible.X - device.X), source.Y + (visible.Y - device.Y), visible.Width, visible.Height);
        }
        else
        {
            // Resample only the visible part, using the window trick so edges match a full-size resample.
            scaled = ImageBuffer.Create(visible.Width, visible.Height, clear: false);
            var shifted = new Rectangle(device.X - visible.X, device.Y - visible.Y, device.Width, device.Height);
            Resampler.Resample(image, source, scaled, shifted, filter);
            scaledSource = scaled.Bounds;
        }
        try
        {
            var opacity = _state.Opacity;
            for (var y = 0; y < visible.Height; y++)
            {
                var src = scaled.GetRow(scaledSource.Y + y).Slice(scaledSource.X, visible.Width);
                var dst = _buffer.GetRow(visible.Y + y).Slice(visible.X, visible.Width);
                Blend(src, dst, blend, opacity);
            }
        }
        finally
        {
            if (!sameSize) scaled.Dispose();
        }
    }

    private ImageCanvas DrawImageTransformed(ImageBuffer image, Rectangle source, RectangleF destination, BlendMode blend, ResamplingFilter filter)
    {
        // General case: build the source→device matrix and inverse-map each device pixel.
        var toDest = Matrix3x2.CreateScale(destination.Width / source.Width, destination.Height / source.Height)
            * Matrix3x2.CreateTranslation(destination.X, destination.Y)
            * _state.Transform;
        var full = Matrix3x2.CreateTranslation(-source.X, -source.Y) * toDest;
        if (!Matrix3x2.Invert(full, out var inverse)) return this;

        var corners = new[]
        {
            Vector2.Transform(new Vector2(source.Left, source.Top), Matrix3x2.CreateTranslation(-source.X, -source.Y) * toDest),
            Vector2.Transform(new Vector2(source.Right, source.Top), Matrix3x2.CreateTranslation(-source.X, -source.Y) * toDest),
            Vector2.Transform(new Vector2(source.Right, source.Bottom), Matrix3x2.CreateTranslation(-source.X, -source.Y) * toDest),
            Vector2.Transform(new Vector2(source.Left, source.Bottom), Matrix3x2.CreateTranslation(-source.X, -source.Y) * toDest),
        };
        var area = Rectangle.FromLTRB(
            (int)MathF.Floor(corners.Min(c => c.X)), (int)MathF.Floor(corners.Min(c => c.Y)),
            (int)MathF.Ceiling(corners.Max(c => c.X)), (int)MathF.Ceiling(corners.Max(c => c.Y)));
        area.Intersect(_state.Clip);
        if (area.IsEmptyArea()) return this;

        var opacity = _state.Opacity;
        var sw = source.Width;
        var sh = source.Height;
        for (var y = area.Top; y < area.Bottom; y++)
        {
            var dst = _buffer.GetRow(y);
            for (var x = area.Left; x < area.Right; x++)
            {
                var sp = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), inverse);
                if (sp.X < -0.5f || sp.Y < -0.5f || sp.X > sw + 0.5f || sp.Y > sh + 0.5f) continue;
                var color = SampleBilinear(image, source, sp.X - 0.5f, sp.Y - 0.5f, filter);
                if (color.A == 0 && blend == BlendMode.SourceOver) continue;
                dst[x] = blend switch
                {
                    BlendMode.Copy => opacity >= 1f ? color : Compositor.SourceOver(dst[x], color, opacity),
                    BlendMode.Erase => Compositor.Erase(dst[x], color.A / 255f * opacity),
                    _ => Compositor.SourceOver(dst[x], color, opacity),
                };
            }
        }
        return this;
    }

    private static Rgba32 SampleBilinear(ImageBuffer image, Rectangle source, float x, float y, ResamplingFilter filter)
    {
        if (filter == ResamplingFilter.NearestNeighbor)
        {
            var nx = Math.Clamp(source.X + (int)MathF.Round(x), source.Left, source.Right - 1);
            var ny = Math.Clamp(source.Y + (int)MathF.Round(y), source.Top, source.Bottom - 1);
            return image[nx, ny];
        }
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var fx = x - x0;
        var fy = y - y0;
        Vector4 Fetch(int px, int py)
        {
            px = Math.Clamp(source.X + px, source.Left, source.Right - 1);
            py = Math.Clamp(source.Y + py, source.Top, source.Bottom - 1);
            var p = image[px, py];
            var a = p.A / 255f;
            return new Vector4(p.R / 255f * a, p.G / 255f * a, p.B / 255f * a, a);
        }
        var c00 = Fetch(x0, y0);
        var c10 = Fetch(x0 + 1, y0);
        var c01 = Fetch(x0, y0 + 1);
        var c11 = Fetch(x0 + 1, y0 + 1);
        var top = c00 + (c10 - c00) * fx;
        var bottom = c01 + (c11 - c01) * fx;
        var c = top + (bottom - top) * fy;
        if (c.W <= 1e-5f) return Rgba32.Transparent;
        var inv = 1f / c.W;
        return Rgba32.FromScaled(c.X * inv, c.Y * inv, c.Z * inv, c.W);
    }

    private static void Blend(ReadOnlySpan<Rgba32> src, Span<Rgba32> dst, BlendMode mode, float opacity)
    {
        switch (mode)
        {
            case BlendMode.Copy when opacity >= 1f:
                src.CopyTo(dst);
                break;
            case BlendMode.Erase:
                for (var i = 0; i < src.Length; i++) dst[i] = Compositor.Erase(dst[i], src[i].A / 255f * opacity);
                break;
            default:
                for (var i = 0; i < src.Length; i++)
                {
                    var s = src[i];
                    if (s.A == 0) continue;
                    dst[i] = Compositor.SourceOver(dst[i], s, opacity);
                }
                break;
        }
    }

    // ---- Text ----

    /// <summary>
    /// Draws text using the supplied rasteriser. Core ships a TrueType/OpenType rasteriser; the Blazor package can
    /// substitute one backed by the browser so system fonts and complex scripts work.
    /// </summary>
    public ImageCanvas DrawText(string text, Vector2 position, TextStyle style, ITextRasterizer rasterizer)
    {
        ArgumentNullException.ThrowIfNull(rasterizer);
        ArgumentNullException.ThrowIfNull(style);
        if (string.IsNullOrEmpty(text)) return this;
        var path = rasterizer.GetTextPath(text, position, style);
        if (path is null || path.IsEmpty) return this;
        if (style.FillColor is { } fill) FillPath(path, fill);
        if (style.StrokeColor is { } stroke) StrokePath(path, stroke, style.Stroke);
        return this;
    }

    // ---- Output ----

    /// <summary>Returns an independent copy of the canvas pixels.</summary>
    public ImageBuffer Snapshot(IPixelAllocator? allocator = null) => _buffer.Clone(allocator);

    /// <summary>Encodes the canvas.</summary>
    public ValueTask<EncodedImage> ExportAsync(ImageExportOptions? options = null, ImageCodecRegistry? codecs = null, CancellationToken cancellationToken = default)
    {
        var registry = codecs ?? ImageCodecRegistry.CreateDefault();
        var encoder = registry.GetEncoder(options?.Format ?? ImageFormat.Png)
            ?? throw new ImageCapabilityException($"No encoder is registered for {options?.Format ?? ImageFormat.Png}.");
        return encoder.EncodeAsync(_buffer, options ?? ImageExportOptions.Png, cancellationToken);
    }

    /// <summary>Runs a pipeline over the canvas contents, replacing them with the result.</summary>
    public ImageCanvas ApplyPipeline(Pipeline.ImagePipeline pipeline, Pipeline.PipelineExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        var result = pipeline.Execute(_buffer, options);
        if (ReferenceEquals(result, _buffer)) return this;

        if (!_ownsBuffer)
        {
            // Someone else owns these pixels, so the result has to be written back in place.
            if (result.Size != _buffer.Size)
            {
                result.Dispose();
                throw new InvalidOperationException("A pipeline that changes the image size cannot be applied to a canvas created with FromBuffer, because the caller owns those pixels. Use FromImage or Create instead.");
            }
            result.CopyTo(_buffer);
            result.Dispose();
            return this;
        }

        _buffer.Dispose();
        _buffer = result;
        _state = _state with { Clip = ClampClip(_state.Clip, _buffer.Bounds) };
        _states.Clear();
        return this;
    }

    private static Rectangle ClampClip(Rectangle clip, Rectangle bounds)
    {
        clip.Intersect(bounds);
        return clip.HasArea() ? clip : bounds;
    }

    private VectorPath TransformPath(VectorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _state.Transform.IsIdentity ? path : path.Transform(_state.Transform);
    }

    private StrokeStyle ScaleStroke(StrokeStyle style)
    {
        // Stroke width follows the transform's average scale so strokes look right under zoom.
        var m = _state.Transform;
        if (m.IsIdentity) return style;
        var sx = new Vector2(m.M11, m.M12).Length();
        var sy = new Vector2(m.M21, m.M22).Length();
        var scale = (sx + sy) / 2;
        return MathF.Abs(scale - 1f) < 1e-4f ? style : style.ForScale(scale);
    }

    public void Dispose()
    {
        if (_ownsBuffer) _buffer.Dispose();
    }
}

/// <summary>Text appearance: font, size, colours and alignment.</summary>
public sealed record TextStyle
{
    /// <summary>Font family name. Resolution depends on the rasteriser in use.</summary>
    public string FontFamily { get; init; } = "sans-serif";

    /// <summary>Font size in pixels (em size).</summary>
    public float FontSize { get; init; } = 16f;

    public bool Bold { get; init; }
    public bool Italic { get; init; }

    /// <summary>Fill colour, or null to skip filling.</summary>
    public Rgba32? FillColor { get; init; } = Rgba32.Black;

    /// <summary>Outline colour, or null to skip outlining.</summary>
    public Rgba32? StrokeColor { get; init; }

    /// <summary>Outline style used when <see cref="StrokeColor"/> is set.</summary>
    public StrokeStyle Stroke { get; init; } = StrokeStyle.Default;

    /// <summary>Horizontal alignment of each line relative to the drawing position.</summary>
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    /// <summary>Vertical meaning of the drawing position.</summary>
    public TextBaseline Baseline { get; init; } = TextBaseline.Alphabetic;

    /// <summary>Line spacing as a multiple of the font size.</summary>
    public float LineHeight { get; init; } = 1.2f;

    /// <summary>Extra spacing added between characters, in pixels.</summary>
    public float LetterSpacing { get; init; }

    /// <summary>Maximum line width in pixels before wrapping, or null for no wrapping.</summary>
    public float? MaxWidth { get; init; }

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public TextStyle ForScale(double scale) => scale == 1 ? this : this with
    {
        FontSize = (float)(FontSize * scale),
        LetterSpacing = (float)(LetterSpacing * scale),
        MaxWidth = MaxWidth is { } w ? (float)(w * scale) : null,
        Stroke = Stroke.ForScale(scale),
    };
}

/// <summary>Horizontal text alignment.</summary>
public enum TextAlignment { Left = 0, Center, Right }

/// <summary>Which part of the text the drawing position refers to vertically.</summary>
public enum TextBaseline { Alphabetic = 0, Top, Middle, Bottom }

/// <summary>Measured text extents in pixels.</summary>
public readonly record struct TextMetrics(float Width, float Height, float Ascent, float Descent, int LineCount);

/// <summary>Turns text into vector outlines. Implementations differ in how they resolve fonts.</summary>
public interface ITextRasterizer
{
    /// <summary>Measures the text without drawing it.</summary>
    TextMetrics Measure(string text, TextStyle style);

    /// <summary>Builds the outline of the text positioned at <paramref name="origin"/>, or null when nothing can be drawn.</summary>
    VectorPath? GetTextPath(string text, Vector2 origin, TextStyle style);
}
