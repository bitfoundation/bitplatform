using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Annotations;

/// <summary>A rectangle, optionally with rounded corners.</summary>
public sealed record RectangleAnnotation : Annotation
{
    public required RectangleF Rectangle { get; init; }
    public ShapeStyle Style { get; init; } = ShapeStyle.Default;
    /// <summary>Corner radius in pixels.</summary>
    public float CornerRadius { get; init; }

    public override string Kind => "Rectangle";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        var path = CornerRadius > 0
            ? new PathBuilder().AddRoundedRectangle(Rectangle, CornerRadius).Build()
            : new PathBuilder().AddRectangle(Rectangle).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    public override Annotation Translate(float dx, float dy) => this with { Rectangle = Offset(Rectangle, dx, dy) };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Rectangle = Scale(Rectangle, scale),
        CornerRadius = (float)(CornerRadius * scale),
        Style = Style.ForScale(scale),
    };

    internal static RectangleF Offset(RectangleF r, float dx, float dy) => new(r.X + dx, r.Y + dy, r.Width, r.Height);
    internal static RectangleF Scale(RectangleF r, double s) => new((float)(r.X * s), (float)(r.Y * s), (float)(r.Width * s), (float)(r.Height * s));
}

/// <summary>An ellipse inscribed in a rectangle.</summary>
public sealed record EllipseAnnotation : Annotation
{
    public required RectangleF Rectangle { get; init; }
    public ShapeStyle Style { get; init; } = ShapeStyle.Default;

    public override string Kind => "Ellipse";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        var path = new PathBuilder().AddEllipse(Rectangle).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    public override Annotation Translate(float dx, float dy) => this with { Rectangle = RectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override Annotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = RectangleAnnotation.Scale(Rectangle, scale), Style = Style.ForScale(scale) };

    public override bool HitTest(Vector2 point, float tolerance = 4f)
    {
        var local = point;
        if (Rotation != 0 && Matrix3x2.Invert(GetTransform(), out var inverse)) local = Vector2.Transform(point, inverse);
        var rx = Rectangle.Width / 2 + tolerance;
        var ry = Rectangle.Height / 2 + tolerance;
        if (rx <= 0 || ry <= 0) return false;
        var dx = (local.X - (Rectangle.X + Rectangle.Width / 2)) / rx;
        var dy = (local.Y - (Rectangle.Y + Rectangle.Height / 2)) / ry;
        return dx * dx + dy * dy <= 1f;
    }
}

/// <summary>A straight line between two points.</summary>
public sealed record LineAnnotation : Annotation
{
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public ShapeStyle Style { get; init; } = ShapeStyle.Default;

    public override string Kind => "Line";

    public override RectangleF GetBounds() => RectangleF.FromLTRB(
        MathF.Min(Start.X, End.X), MathF.Min(Start.Y, End.Y), MathF.Max(Start.X, End.X), MathF.Max(Start.Y, End.Y));

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (Style.StrokeColor is { } stroke) canvas.DrawLine(Start, End, stroke, Style.Stroke);
    }

    public override Annotation Translate(float dx, float dy) => this with { Start = Start + new Vector2(dx, dy), End = End + new Vector2(dx, dy) };
    public override Annotation ForScale(double scale) => scale == 1 ? this : this with { Start = Start * (float)scale, End = End * (float)scale, Style = Style.ForScale(scale) };

    public override bool HitTest(Vector2 point, float tolerance = 4f)
        => DistanceToSegment(ToLocal(point), Start, End) <= tolerance + Style.Stroke.Width / 2;

    internal static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        if (lengthSquared < 1e-6f) return Vector2.Distance(p, a);
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }
}

/// <summary>An arrow with a filled head at the end point. The most used annotation for screenshots and bug reports.</summary>
public sealed record ArrowAnnotation : Annotation
{
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public ShapeStyle Style { get; init; } = ShapeStyle.Default;
    /// <summary>Head length as a multiple of the stroke width.</summary>
    public float HeadLengthFactor { get; init; } = 4f;
    /// <summary>Head half-width as a multiple of the stroke width.</summary>
    public float HeadWidthFactor { get; init; } = 3f;

    public override string Kind => "Arrow";

    public override RectangleF GetBounds()
    {
        var head = Style.Stroke.Width * MathF.Max(HeadLengthFactor, HeadWidthFactor);
        var r = RectangleF.FromLTRB(MathF.Min(Start.X, End.X), MathF.Min(Start.Y, End.Y), MathF.Max(Start.X, End.X), MathF.Max(Start.Y, End.Y));
        r.Inflate(head, head);
        return r;
    }

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        var color = Style.StrokeColor ?? Style.FillColor;
        if (color is { } c) canvas.DrawArrow(Start, End, c, Style.Stroke, HeadLengthFactor, HeadWidthFactor);
    }

    public override Annotation Translate(float dx, float dy) => this with { Start = Start + new Vector2(dx, dy), End = End + new Vector2(dx, dy) };
    public override Annotation ForScale(double scale) => scale == 1 ? this : this with { Start = Start * (float)scale, End = End * (float)scale, Style = Style.ForScale(scale) };

    public override bool HitTest(Vector2 point, float tolerance = 4f)
        => LineAnnotation.DistanceToSegment(ToLocal(point), Start, End) <= tolerance + Style.Stroke.Width;
}

/// <summary>A freehand stroke captured from pointer input.</summary>
public sealed record FreehandAnnotation : Annotation
{
    private readonly Vector2[] _points = [];

    /// <summary>The stroke path in image coordinates.</summary>
    public required IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? [];
    }

    public ShapeStyle Style { get; init; } = ShapeStyle.Default with { Stroke = new StrokeStyle { Width = 4f, Cap = LineCap.Round, Join = LineJoin.Round } };

    /// <summary>Fill and close the stroke instead of drawing an open line (a lasso highlight).</summary>
    public bool Closed { get; init; }

    public override string Kind => "Freehand";

    public override RectangleF GetBounds()
    {
        if (_points.Length == 0) return RectangleF.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in _points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        var r = RectangleF.FromLTRB(minX, minY, maxX, maxY);
        var w = Style.Stroke.Width;
        r.Inflate(w, w);
        return r;
    }

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (_points.Length == 0) return;
        if (_points.Length == 1)
        {
            if (Style.StrokeColor is { } dot) canvas.FillCircle(_points[0], Style.Stroke.Width / 2, dot);
            return;
        }
        var builder = new PathBuilder();
        AppendSmoothed(builder, _points, Closed);
        var path = builder.Build();
        if (Closed && Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    /// <summary>Draws through the points with Catmull-Rom style smoothing so fast strokes do not look polygonal.</summary>
    internal static void AppendSmoothed(PathBuilder builder, IReadOnlyList<Vector2> points, bool closed)
    {
        if (points.Count < 3)
        {
            builder.AddPolyline(points);
            if (closed) builder.Close();
            return;
        }
        builder.MoveTo(points[0]);
        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Math.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(i + 2, points.Count - 1)];
            var c1 = p1 + (p2 - p0) / 6f;
            var c2 = p2 - (p3 - p1) / 6f;
            builder.CubicTo(c1, c2, p2);
        }
        if (closed) builder.Close();
    }

    public override Annotation Translate(float dx, float dy)
    {
        var offset = new Vector2(dx, dy);
        return this with { Points = _points.Select(p => p + offset).ToArray() };
    }

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Points = _points.Select(p => p * (float)scale).ToArray(),
        Style = Style.ForScale(scale),
    };

    public override bool HitTest(Vector2 point, float tolerance = 4f)
    {
        var limit = tolerance + Style.Stroke.Width / 2;
        point = ToLocal(point);
        for (var i = 0; i + 1 < _points.Length; i++)
            if (LineAnnotation.DistanceToSegment(point, _points[i], _points[i + 1]) <= limit) return true;
        return _points.Length == 1 && Vector2.Distance(point, _points[0]) <= limit;
    }
}

/// <summary>A polygon or polyline through arbitrary points.</summary>
public sealed record PolygonAnnotation : Annotation
{
    private readonly Vector2[] _points = [];

    public required IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? [];
    }

    public ShapeStyle Style { get; init; } = ShapeStyle.Default;
    public bool Closed { get; init; } = true;

    public override string Kind => "Polygon";

    public override RectangleF GetBounds()
    {
        if (_points.Length == 0) return RectangleF.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in _points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (_points.Length < 2) return;
        var path = new PathBuilder().AddPolygon(_points, Closed).Build();
        if (Closed && Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    public override Annotation Translate(float dx, float dy)
    {
        var offset = new Vector2(dx, dy);
        return this with { Points = _points.Select(p => p + offset).ToArray() };
    }

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Points = _points.Select(p => p * (float)scale).ToArray(),
        Style = Style.ForScale(scale),
    };
}

/// <summary>A text label, optionally with a background box.</summary>
public sealed record TextAnnotation : Annotation
{
    public required string Text { get; init; }
    public required Vector2 Position { get; init; }
    public TextStyle Style { get; init; } = new();

    /// <summary>Background colour drawn behind the text, or null for none.</summary>
    public Rgba32? BackgroundColor { get; init; }

    /// <summary>Padding around the text when a background is drawn.</summary>
    public float BackgroundPadding { get; init; } = 6f;

    /// <summary>Corner radius of the background box.</summary>
    public float BackgroundCornerRadius { get; init; } = 4f;

    /// <summary>Cached measured size, set by the editor after measuring. Used for bounds before the first render.</summary>
    public SizeF? MeasuredSize { get; init; }

    public override string Kind => "Text";

    public override RectangleF GetBounds()
    {
        var size = MeasuredSize ?? new SizeF(Math.Max(1, Text.Length) * Style.FontSize * 0.55f, Style.FontSize * Style.LineHeight * (1 + Text.Count(c => c == '\n')));
        var x = Style.Alignment switch
        {
            TextAlignment.Center => Position.X - size.Width / 2,
            TextAlignment.Right => Position.X - size.Width,
            _ => Position.X,
        };
        var y = Style.Baseline switch
        {
            TextBaseline.Top => Position.Y,
            TextBaseline.Middle => Position.Y - size.Height / 2,
            TextBaseline.Bottom => Position.Y - size.Height,
            _ => Position.Y - Style.FontSize * 0.8f,
        };
        var r = new RectangleF(x, y, size.Width, size.Height);
        if (BackgroundColor is not null) r.Inflate(BackgroundPadding, BackgroundPadding);
        return r;
    }

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (string.IsNullOrEmpty(Text) || context.TextRasterizer is not { } rasterizer) return;
        if (BackgroundColor is { } background)
        {
            var metrics = rasterizer.Measure(Text, Style);
            var box = MeasuredBounds(metrics);
            box.Inflate(BackgroundPadding, BackgroundPadding);
            canvas.FillRoundedRectangle(box, BackgroundCornerRadius, background);
        }
        canvas.DrawText(Text, Position, Style, rasterizer);
    }

    /// <summary>Bounds computed from actual measured metrics.</summary>
    public RectangleF MeasuredBounds(TextMetrics metrics)
    {
        var x = Style.Alignment switch
        {
            TextAlignment.Center => Position.X - metrics.Width / 2,
            TextAlignment.Right => Position.X - metrics.Width,
            _ => Position.X,
        };
        var y = Style.Baseline switch
        {
            TextBaseline.Top => Position.Y,
            TextBaseline.Middle => Position.Y - metrics.Height / 2,
            TextBaseline.Bottom => Position.Y - metrics.Height,
            _ => Position.Y - metrics.Ascent,
        };
        return new RectangleF(x, y, metrics.Width, metrics.Height);
    }

    public override Annotation Translate(float dx, float dy) => this with { Position = Position + new Vector2(dx, dy) };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Position = Position * (float)scale,
        Style = Style.ForScale(scale),
        BackgroundPadding = (float)(BackgroundPadding * scale),
        BackgroundCornerRadius = (float)(BackgroundCornerRadius * scale),
        MeasuredSize = MeasuredSize is { } s ? new SizeF((float)(s.Width * scale), (float)(s.Height * scale)) : null,
    };
}

/// <summary>A translucent highlight rectangle, like a marker pen.</summary>
public sealed record HighlightAnnotation : Annotation
{
    public required RectangleF Rectangle { get; init; }
    public Rgba32 Color { get; init; } = new(255, 235, 59, 110);

    public override string Kind => "Highlight";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
        => canvas.FillRectangle(Rectangle, Color);

    public override Annotation Translate(float dx, float dy) => this with { Rectangle = RectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override Annotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = RectangleAnnotation.Scale(Rectangle, scale) };
}

/// <summary>How a redaction hides the underlying content.</summary>
public enum RedactionMode
{
    /// <summary>Paints a solid block. Irreversible and unambiguous; the safest choice.</summary>
    Solid = 0,
    /// <summary>Averages blocks of pixels. Looks softer but is still irreversible at a sensible block size.</summary>
    Pixelate,
    /// <summary>Blurs the region. Use only for de-emphasis, never for secrets: strong blurs can sometimes be inverted.</summary>
    Blur,
}

/// <summary>
/// Hides part of the image. Unlike other annotations this one reads the pixels underneath, so it is applied by the
/// renderer before the overlay pass.
/// </summary>
public sealed record RedactionAnnotation : Annotation
{
    public required RectangleF Rectangle { get; init; }
    public RedactionMode Mode { get; init; } = RedactionMode.Solid;
    public Rgba32 Color { get; init; } = Rgba32.Black;
    /// <summary>Block size for <see cref="RedactionMode.Pixelate"/> or radius for <see cref="RedactionMode.Blur"/>.</summary>
    public float Strength { get; init; } = 12f;

    public override string Kind => "Redaction";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        var area = Rectangle;
        if (Mode is not (RedactionMode.Pixelate or RedactionMode.Blur))
        {
            canvas.FillRectangle(area, Color);
            return;
        }

        // The filters work on device pixels, so the region the user sees (after the canvas transform, which carries
        // this annotation's rotation) is mapped there first. Rounding outwards means a partly covered pixel is
        // redacted rather than left readable.
        var outline = new PathBuilder().AddRectangle(area).Build().Transform(canvas.Transform);
        var bounds = outline.Bounds;
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)) return;
        var device = System.Drawing.Rectangle.FromLTRB(
            (int)MathF.Floor(bounds.Left), (int)MathF.Floor(bounds.Top), (int)MathF.Ceiling(bounds.Right), (int)MathF.Ceiling(bounds.Bottom));
        device.Intersect(canvas.Clip);
        device.Intersect(canvas.Buffer.Bounds);
        if (device.IsEmptyArea()) return;

        var transform = canvas.Transform;
        var axisAligned = transform.M12 == 0 && transform.M21 == 0;
        if (axisAligned)
        {
            Redact(canvas.Buffer, device);
            return;
        }

        // Rotated: filter the bounding box on a copy, then blend it back only inside the rotated outline.
        using var filtered = canvas.Buffer.CopyRegion(device);
        Redact(filtered, new System.Drawing.Rectangle(System.Drawing.Point.Empty, filtered.Size));
        var target = canvas.Buffer;
        Rasterizer.Rasterize(outline, device, FillRule.NonZero, (y, x0, coverage) =>
        {
            var row = target.GetRow(y);
            var source = filtered.GetRow(y - device.Y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = Math.Min(1f, coverage[i]);
                if (a <= 0) continue;
                var x = x0 + i;
                row[x] = a >= 0.999f ? source[x - device.X] : Lerp(row[x], source[x - device.X], a);
            }
        });
    }

    private static Rgba32 Lerp(Rgba32 from, Rgba32 to, float t) => new(
        (byte)MathF.Round(from.R + (to.R - from.R) * t),
        (byte)MathF.Round(from.G + (to.G - from.G) * t),
        (byte)MathF.Round(from.B + (to.B - from.B) * t),
        (byte)MathF.Round(from.A + (to.A - from.A) * t));

    /// <summary>Pixelates or blurs a region of a buffer in place.</summary>
    private void Redact(ImageBuffer buffer, System.Drawing.Rectangle region)
    {
        Operations.IImageOperation filter = Mode == RedactionMode.Pixelate
            ? new PixelateFilter(Math.Max(2, (int)Strength), region)
            : new BlurFilter(Math.Max(1f, Strength), region);
        filter.Apply(buffer, new Operations.OperationContext(canMutateSource: true));
    }

    public override Annotation Translate(float dx, float dy) => this with { Rectangle = RectangleAnnotation.Offset(Rectangle, dx, dy) };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Rectangle = RectangleAnnotation.Scale(Rectangle, scale),
        Strength = (float)Math.Max(1, Strength * scale),
    };
}

/// <summary>A rectangle or ellipse with a leader line pointing at something, plus a text label.</summary>
public sealed record CalloutAnnotation : Annotation
{
    public required RectangleF Bubble { get; init; }
    /// <summary>Where the leader line points, in image coordinates.</summary>
    public required Vector2 Target { get; init; }
    public string Text { get; init; } = string.Empty;
    public TextStyle TextStyle { get; init; } = new() { Alignment = TextAlignment.Center, Baseline = TextBaseline.Middle };
    public ShapeStyle Style { get; init; } = ShapeStyle.Default with { FillColor = Rgba32.White };
    public float CornerRadius { get; init; } = 6f;

    public override string Kind => "Callout";

    public override RectangleF GetBounds()
    {
        var r = Bubble;
        return RectangleF.FromLTRB(MathF.Min(r.Left, Target.X), MathF.Min(r.Top, Target.Y), MathF.Max(r.Right, Target.X), MathF.Max(r.Bottom, Target.Y));
    }

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        // Leader: a triangle from the two bubble edge points nearest the target.
        var center = new Vector2(Bubble.X + Bubble.Width / 2, Bubble.Y + Bubble.Height / 2);
        var direction = Target - center;
        if (direction.LengthSquared() > 1e-3f)
        {
            direction = Vector2.Normalize(direction);
            var perpendicular = new Vector2(-direction.Y, direction.X) * MathF.Min(Bubble.Width, Bubble.Height) * 0.18f;
            var basePoint = EdgePoint(Bubble, center, direction);
            var tail = new PathBuilder()
                .MoveTo(basePoint + perpendicular)
                .LineTo(Target)
                .LineTo(basePoint - perpendicular)
                .Close()
                .Build();
            if (Style.FillColor is { } tailFill) canvas.FillPath(tail, tailFill);
            if (Style.StrokeColor is { } tailStroke) canvas.StrokePath(tail, tailStroke, Style.Stroke);
        }
        var bubble = new PathBuilder().AddRoundedRectangle(Bubble, CornerRadius).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(bubble, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(bubble, stroke, Style.Stroke);
        if (!string.IsNullOrEmpty(Text) && context.TextRasterizer is { } rasterizer)
            canvas.DrawText(Text, center, TextStyle with { MaxWidth = TextStyle.MaxWidth ?? Bubble.Width - 12 }, rasterizer);
    }

    private static Vector2 EdgePoint(RectangleF rect, Vector2 center, Vector2 direction)
    {
        // Intersect the ray from the centre with the rectangle border.
        var tx = direction.X == 0 ? float.MaxValue : (direction.X > 0 ? rect.Right - center.X : rect.Left - center.X) / direction.X;
        var ty = direction.Y == 0 ? float.MaxValue : (direction.Y > 0 ? rect.Bottom - center.Y : rect.Top - center.Y) / direction.Y;
        return center + direction * MathF.Min(tx, ty);
    }

    public override Annotation Translate(float dx, float dy) => this with
    {
        Bubble = RectangleAnnotation.Offset(Bubble, dx, dy),
        Target = Target + new Vector2(dx, dy),
    };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Bubble = RectangleAnnotation.Scale(Bubble, scale),
        Target = Target * (float)scale,
        TextStyle = TextStyle.ForScale(scale),
        Style = Style.ForScale(scale),
        CornerRadius = (float)(CornerRadius * scale),
    };
}

/// <summary>A numbered step marker, for tutorials and bug reports.</summary>
public sealed record StepAnnotation : Annotation
{
    public required Vector2 Center { get; init; }
    public required int Number { get; init; }
    public float Radius { get; init; } = 18f;
    public Rgba32 BackgroundColor { get; init; } = new(230, 40, 40);
    public Rgba32 ForegroundColor { get; init; } = Rgba32.White;
    public TextStyle TextStyle { get; init; } = new() { Alignment = TextAlignment.Center, Baseline = TextBaseline.Middle, Bold = true };

    public override string Kind => "Step";
    public override RectangleF GetBounds() => new(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        canvas.FillCircle(Center, Radius, BackgroundColor);
        if (context.TextRasterizer is { } rasterizer)
        {
            var style = TextStyle with { FontSize = Radius * 1.15f, FillColor = ForegroundColor };
            canvas.DrawText(Number.ToString(System.Globalization.CultureInfo.InvariantCulture), Center, style, rasterizer);
        }
    }

    public override Annotation Translate(float dx, float dy) => this with { Center = Center + new Vector2(dx, dy) };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Center = Center * (float)scale,
        Radius = (float)(Radius * scale),
        TextStyle = TextStyle.ForScale(scale),
    };

    public override bool HitTest(Vector2 point, float tolerance = 4f) => Vector2.Distance(point, Center) <= Radius + tolerance;
}

/// <summary>An image stamp (watermark, logo, sticker) composited at a position.</summary>
public sealed record ImageAnnotation : Annotation
{
    /// <summary>The image to draw. Not owned by the annotation; the caller keeps it alive.</summary>
    public required ImageBuffer Image { get; init; }
    public required RectangleF Rectangle { get; init; }

    public override string Kind => "Image";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (Image.IsDisposed) return;
        canvas.DrawImage(Image, Rectangle);
    }

    public override Annotation Translate(float dx, float dy) => this with { Rectangle = RectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override Annotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = RectangleAnnotation.Scale(Rectangle, scale) };
}
