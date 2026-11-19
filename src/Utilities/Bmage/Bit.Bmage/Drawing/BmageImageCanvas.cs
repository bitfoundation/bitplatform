using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A mutable drawing surface with an immediate-mode API: draw images, shapes, paths and text, then read the pixels or
/// export them. Independent of any UI, so it works headlessly on a server as well as in the browser.
/// </summary>
/// <example>
/// <code>
/// using var canvas = BmageImageCanvas.Create(1200, 630, BmageRgba32.White);
/// canvas.DrawImage(logo, new Rectangle(40, 40, 200, 200));
/// canvas.FillRectangle(new RectangleF(0, 560, 1200, 70), new BmageRgba32(0, 0, 0, 160));
/// var png = await canvas.ExportAsync(BmageImageExportOptions.Png);
/// </code>
/// </example>
public sealed class BmageImageCanvas : IDisposable
{
    private BmageImageBuffer _buffer;
    private readonly bool _ownsBuffer;
    // What drawing allocates working buffers with, how many threads it spreads rows over, and what cancels it.
    private readonly BmageOperationContext _context;
    private readonly Stack<CanvasState> _states = new();
    private CanvasState _state;

    private readonly record struct CanvasState(Matrix3x2 Transform, Rectangle Clip, float Opacity);

    private BmageImageCanvas(BmageImageBuffer buffer, bool ownsBuffer, BmageOperationContext context)
    {
        _buffer = buffer;
        _ownsBuffer = ownsBuffer;
        _context = context.WithProgress(null);
        _state = new CanvasState(Matrix3x2.Identity, buffer.Bounds, 1f);
    }

    /// <summary>Creates a canvas of the given size filled with a colour (transparent by default).</summary>
    public static BmageImageCanvas Create(int width, int height, BmageRgba32 background = default, IBmagePixelAllocator? allocator = null, BmageImageLimits? limits = null)
    {
        var buffer = BmageImageBuffer.Create(width, height, allocator, clear: true, limits);
        if (background.A != 0) buffer.Fill(background);
        return new BmageImageCanvas(buffer, ownsBuffer: true, new BmageOperationContext(allocator, limits));
    }

    /// <summary>Creates a canvas that draws directly into an existing buffer. The buffer is not disposed by the canvas.</summary>
    public static BmageImageCanvas FromBuffer(BmageImageBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return new BmageImageCanvas(buffer, ownsBuffer: false, BmageOperationContext.Default);
    }

    /// <summary>
    /// A canvas drawing directly into an existing buffer with the allocator, limits, threads and cancellation of
    /// <paramref name="context"/>, as an operation or a layer draws.
    /// </summary>
    internal static BmageImageCanvas FromBuffer(BmageImageBuffer buffer, BmageOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(context);
        return new BmageImageCanvas(buffer, ownsBuffer: false, context);
    }

    /// <summary>Creates a canvas holding a copy of an existing image.</summary>
    public static BmageImageCanvas FromImage(BmageImageBuffer source, IBmagePixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new BmageImageCanvas(source.Clone(allocator), ownsBuffer: true, new BmageOperationContext(allocator));
    }

    /// <summary>The pixels being drawn into. Valid until the canvas is disposed.</summary>
    public BmageImageBuffer Buffer => _buffer;

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
    public BmageImageCanvas Save()
    {
        _states.Push(_state);
        return this;
    }

    /// <summary>Restores the state saved by the matching <see cref="Save"/>.</summary>
    public BmageImageCanvas Restore()
    {
        if (_states.Count > 0) _state = _states.Pop();
        return this;
    }

    /// <summary>Multiplies the current transform by a translation.</summary>
    public BmageImageCanvas Translate(float x, float y) { Transform = Matrix3x2.CreateTranslation(x, y) * Transform; return this; }

    /// <summary>Multiplies the current transform by a scale.</summary>
    public BmageImageCanvas Scale(float x, float y) { Transform = Matrix3x2.CreateScale(x, y) * Transform; return this; }

    /// <summary>Multiplies the current transform by a rotation in degrees around a point.</summary>
    public BmageImageCanvas Rotate(float degrees, Vector2 origin = default) { Transform = Matrix3x2.CreateRotation(degrees * MathF.PI / 180f, origin) * Transform; return this; }

    /// <summary>Intersects the clip with a rectangle in current-transform coordinates.</summary>
    public BmageImageCanvas ClipTo(RectangleF rect)
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
    public BmageImageCanvas Clear(BmageRgba32 color = default)
    {
        _buffer.Fill(_state.Clip == _buffer.Bounds ? _buffer.Bounds : _state.Clip, color);
        return this;
    }

    // ---- Shapes ----

    /// <summary>Fills a path.</summary>
    public BmageImageCanvas FillPath(BmageVectorPath path, BmageRgba32 color, BmageFillRule rule = BmageFillRule.NonZero)
    {
        BmageRasterizer.Fill(_buffer, TransformPath(path), color, rule, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Fills a path with a paint (gradient or custom).</summary>
    public BmageImageCanvas FillPath(BmageVectorPath path, IBmagePaint paint, BmageFillRule rule = BmageFillRule.NonZero)
    {
        BmageRasterizer.Fill(_buffer, TransformPath(path), paint, rule, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Strokes a path.</summary>
    public BmageImageCanvas StrokePath(BmageVectorPath path, BmageRgba32 color, BmageStrokeStyle? style = null)
    {
        style ??= BmageStrokeStyle.Default;
        var outline = BmageStroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        BmageRasterizer.Fill(_buffer, outline, color, BmageFillRule.NonZero, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Strokes a path with a paint.</summary>
    public BmageImageCanvas StrokePath(BmageVectorPath path, IBmagePaint paint, BmageStrokeStyle? style = null)
    {
        style ??= BmageStrokeStyle.Default;
        var outline = BmageStroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        BmageRasterizer.Fill(_buffer, outline, paint, BmageFillRule.NonZero, _state.Clip, _state.Opacity);
        return this;
    }

    /// <summary>Removes coverage from the canvas along a stroked path, making those pixels transparent.</summary>
    public BmageImageCanvas ErasePath(BmageVectorPath path, BmageStrokeStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var outline = BmageStroker.CreateStrokeOutline(TransformPath(path), ScaleStroke(style));
        var opacity = _state.Opacity;
        var clip = _state.Clip;
        BmageRasterizer.Rasterize(outline, clip, BmageFillRule.NonZero, (y, x0, coverage) =>
        {
            var row = _buffer.GetRow(y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = coverage[i];
                if (a <= 0.0005f) continue;
                var x = x0 + i;
                if (x < clip.Left || x >= clip.Right) continue;
                row[x] = BmageCompositor.Erase(row[x], MathF.Min(a, 1f) * opacity);
            }
        });
        return this;
    }

    /// <summary>Draws a straight line.</summary>
    public BmageImageCanvas DrawLine(Vector2 from, Vector2 to, BmageRgba32 color, BmageStrokeStyle? style = null)
        => StrokePath(new BmagePathBuilder().MoveTo(from).LineTo(to).Build(), color, style);

    /// <summary>Fills a rectangle.</summary>
    public BmageImageCanvas FillRectangle(RectangleF rect, BmageRgba32 color)
        => FillPath(new BmagePathBuilder().AddRectangle(rect).Build(), color);

    /// <summary>Outlines a rectangle.</summary>
    public BmageImageCanvas DrawRectangle(RectangleF rect, BmageRgba32 color, BmageStrokeStyle? style = null)
        => StrokePath(new BmagePathBuilder().AddRectangle(rect).Build(), color, style);

    /// <summary>Fills a rounded rectangle.</summary>
    public BmageImageCanvas FillRoundedRectangle(RectangleF rect, float radius, BmageRgba32 color)
        => FillPath(new BmagePathBuilder().AddRoundedRectangle(rect, radius).Build(), color);

    /// <summary>Outlines a rounded rectangle.</summary>
    public BmageImageCanvas DrawRoundedRectangle(RectangleF rect, float radius, BmageRgba32 color, BmageStrokeStyle? style = null)
        => StrokePath(new BmagePathBuilder().AddRoundedRectangle(rect, radius).Build(), color, style);

    /// <summary>Fills an ellipse inscribed in the rectangle.</summary>
    public BmageImageCanvas FillEllipse(RectangleF rect, BmageRgba32 color)
        => FillPath(new BmagePathBuilder().AddEllipse(rect).Build(), color);

    /// <summary>Outlines an ellipse inscribed in the rectangle.</summary>
    public BmageImageCanvas DrawEllipse(RectangleF rect, BmageRgba32 color, BmageStrokeStyle? style = null)
        => StrokePath(new BmagePathBuilder().AddEllipse(rect).Build(), color, style);

    /// <summary>Fills a circle.</summary>
    public BmageImageCanvas FillCircle(Vector2 center, float radius, BmageRgba32 color)
        => FillEllipse(new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2), color);

    /// <summary>Fills a polygon.</summary>
    public BmageImageCanvas FillPolygon(IReadOnlyList<Vector2> points, BmageRgba32 color, BmageFillRule rule = BmageFillRule.NonZero)
        => FillPath(new BmagePathBuilder().AddPolygon(points).Build(), color, rule);

    /// <summary>Draws a polyline.</summary>
    public BmageImageCanvas DrawPolyline(IReadOnlyList<Vector2> points, BmageRgba32 color, BmageStrokeStyle? style = null)
        => StrokePath(new BmagePathBuilder().AddPolyline(points).Build(), color, style);

    /// <summary>
    /// Draws an arrow from <paramref name="from"/> to <paramref name="to"/>: a shaft plus a filled triangular head
    /// sized relative to the stroke width.
    /// </summary>
    public BmageImageCanvas DrawArrow(Vector2 from, Vector2 to, BmageRgba32 color, BmageStrokeStyle? style = null, float headLengthFactor = 4f, float headWidthFactor = 3f)
    {
        style ??= BmageStrokeStyle.Default;
        var direction = to - from;
        var length = direction.Length();
        if (length < 1e-3f) return this;
        direction /= length;
        var headLength = MathF.Min(style.Width * headLengthFactor, length * 0.6f);
        var headWidth = style.Width * headWidthFactor;
        var shaftEnd = to - direction * headLength * 0.85f;
        var normal = new Vector2(-direction.Y, direction.X);
        StrokePath(new BmagePathBuilder().MoveTo(from).LineTo(shaftEnd).Build(), color, style with { Cap = BmageLineCap.Round });
        var head = new BmagePathBuilder()
            .MoveTo(to)
            .LineTo(to - direction * headLength + normal * headWidth)
            .LineTo(to - direction * headLength - normal * headWidth)
            .Close()
            .Build();
        return FillPath(head, color);
    }

    // ---- Images ----

    /// <summary>Draws an image at a position, one canvas pixel per source pixel.</summary>
    public BmageImageCanvas DrawImage(BmageImageBuffer image, Vector2 position, BmageBlendMode blend = BmageBlendMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(image);
        return DrawImage(image, new RectangleF(position.X, position.Y, image.Width, image.Height), blend);
    }

    /// <summary>Draws an image scaled into a destination rectangle.</summary>
    public BmageImageCanvas DrawImage(BmageImageBuffer image, RectangleF destination, BmageBlendMode blend = BmageBlendMode.SourceOver, BmageResamplingFilter filter = BmageResamplingFilter.Auto)
        => DrawImage(image, image.Bounds, destination, blend, filter);

    /// <summary>Draws part of an image into a destination rectangle.</summary>
    public BmageImageCanvas DrawImage(BmageImageBuffer image, Rectangle source, RectangleF destination, BmageBlendMode blend = BmageBlendMode.SourceOver, BmageResamplingFilter filter = BmageResamplingFilter.Auto)
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

    private void DrawImageAxisAligned(BmageImageBuffer image, Rectangle source, Rectangle device, BmageBlendMode blend, BmageResamplingFilter filter)
    {
        var clip = _state.Clip;
        var visible = device;
        visible.Intersect(clip);
        if (visible.IsEmptyArea()) return;

        var sameSize = device.Size == source.Size;
        BmageImageBuffer scaled;
        Rectangle scaledSource;
        if (sameSize)
        {
            scaled = image;
            scaledSource = new Rectangle(source.X + (visible.X - device.X), source.Y + (visible.Y - device.Y), visible.Width, visible.Height);
        }
        else
        {
            // Resample only the visible part, using the window trick so edges match a full-size resample.
            scaled = BmageImageBuffer.Create(visible.Width, visible.Height, _context.Allocator, clear: false, _context.Limits);
            var shifted = new Rectangle(device.X - visible.X, device.Y - visible.Y, device.Width, device.Height);
            BmageResampler.Resample(image, source, scaled, shifted, filter, _context);
            scaledSource = scaled.Bounds;
        }
        try
        {
            var opacity = _state.Opacity;
            // Rows blend independently, unless the canvas is drawn onto itself and a row may read one another writes.
            BmageParallelRows.For(RowContext(scaled), 0, visible.Height, visible.Width, (start, end, band) =>
            {
                for (var y = start; y < end; y++)
                {
                    if (((y - start) & 31) == 0) band.ThrowIfCancellationRequested();
                    var src = scaled.GetRow(scaledSource.Y + y).Slice(scaledSource.X, visible.Width);
                    var dst = _buffer.GetRow(visible.Y + y).Slice(visible.X, visible.Width);
                    Blend(src, dst, blend, opacity);
                }
            });
        }
        finally
        {
            if (!sameSize) scaled.Dispose();
        }
    }

    /// <summary>The context rows are drawn with: one thread when <paramref name="image"/> is the canvas's own pixels.</summary>
    private BmageOperationContext RowContext(BmageImageBuffer image) => ReferenceEquals(image, _buffer) ? _context.WithParallelism(1) : _context;

    private BmageImageCanvas DrawImageTransformed(BmageImageBuffer image, Rectangle source, RectangleF destination, BmageBlendMode blend, BmageResamplingFilter filter)
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
        // A copy at partial opacity is drawn over what is there, as a canvas always did: it never punches the image's
        // transparent pixels into the target. A layer that must is composited through BmageCompositor.BlendRow.
        var over = blend == BmageBlendMode.SourceOver || (blend == BmageBlendMode.Copy && opacity < 1f);
        var sw = source.Width;
        var sh = source.Height;
        // Every pixel is sampled from the image alone, so the rows split into bands; the work per pixel is a few samples.
        BmageParallelRows.For(RowContext(image), area.Top, area.Bottom, area.Width * 4L, (start, end, band) =>
        {
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) band.ThrowIfCancellationRequested();
                var dst = _buffer.GetRow(y);
                for (var x = area.Left; x < area.Right; x++)
                {
                    var sp = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), inverse);
                    if (sp.X < -0.5f || sp.Y < -0.5f || sp.X > sw + 0.5f || sp.Y > sh + 0.5f) continue;
                    var color = SampleBilinear(image, source, sp.X - 0.5f, sp.Y - 0.5f, filter);
                    if (color.A == 0 && over) continue;
                    dst[x] = over ? BmageCompositor.SourceOver(dst[x], color, opacity) : BmageCompositor.Blend(dst[x], color, blend, opacity);
                }
            }
        });
        return this;
    }

    private static BmageRgba32 SampleBilinear(BmageImageBuffer image, Rectangle source, float x, float y, BmageResamplingFilter filter)
    {
        if (filter == BmageResamplingFilter.NearestNeighbor)
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
        var tx = new Vector4(fx);
        var top = c00 + (c10 - c00) * tx;
        var bottom = c01 + (c11 - c01) * tx;
        var c = top + (bottom - top) * new Vector4(fy);
        if (c.W <= 1e-5f) return BmageRgba32.Transparent;
        var inv = 1f / c.W;
        return BmageRgba32.FromScaled(c.X * inv, c.Y * inv, c.Z * inv, c.W);
    }

    private static void Blend(ReadOnlySpan<BmageRgba32> src, Span<BmageRgba32> dst, BmageBlendMode mode, float opacity)
    {
        switch (mode)
        {
            case BmageBlendMode.Copy when opacity >= 1f:
                src.CopyTo(dst);
                break;
            case BmageBlendMode.Erase:
                for (var i = 0; i < src.Length; i++) dst[i] = BmageCompositor.Erase(dst[i], src[i].A / 255f * opacity);
                break;
            case BmageBlendMode.SourceOver:
            // A copy at partial opacity is drawn over what is there, as a canvas always did: it never punches the
            // image's transparent pixels into the target. A layer that must is composited through BmageCompositor.BlendRow.
            case BmageBlendMode.Copy:
                for (var i = 0; i < src.Length; i++)
                {
                    var s = src[i];
                    if (s.A == 0) continue;
                    dst[i] = BmageCompositor.SourceOver(dst[i], s, opacity);
                }
                break;
            default:
                BmageCompositor.BlendRow(src, dst, mode, opacity);
                break;
        }
    }

    // ---- Text ----

    /// <summary>
    /// Draws text using the supplied rasteriser. Core ships a TrueType/OpenType rasteriser; the Blazor package can
    /// substitute one backed by the browser so system fonts and complex scripts work.
    /// </summary>
    public BmageImageCanvas DrawText(string text, Vector2 position, BmageTextStyle style, IBmageTextRasterizer rasterizer)
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
    public BmageImageBuffer Snapshot(IBmagePixelAllocator? allocator = null) => _buffer.Clone(allocator);

    /// <summary>Encodes the canvas.</summary>
    public ValueTask<BmageEncodedImage> ExportAsync(BmageImageExportOptions? options = null, BmageImageCodecRegistry? codecs = null, CancellationToken cancellationToken = default)
    {
        var registry = codecs ?? BmageImageCodecRegistry.CreateDefault();
        var encoder = registry.GetEncoder(options?.Format ?? BmageImageFormat.Png)
            ?? throw new BmageImageCapabilityException($"No encoder is registered for {options?.Format ?? BmageImageFormat.Png}.");
        return encoder.EncodeAsync(_buffer, options ?? BmageImageExportOptions.Png, cancellationToken);
    }

    /// <summary>Runs a pipeline over the canvas contents, replacing them with the result.</summary>
    public BmageImageCanvas ApplyPipeline(BmageImagePipeline pipeline, BmagePipelineExecutionOptions? options = null)
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

    private BmageVectorPath TransformPath(BmageVectorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _state.Transform.IsIdentity ? path : path.Transform(_state.Transform);
    }

    private BmageStrokeStyle ScaleStroke(BmageStrokeStyle style)
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
