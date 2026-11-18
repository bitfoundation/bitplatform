using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A layer painted with a brush, stroke by stroke: freehand colour, retouching by hand, or an eraser taking paint away.
/// The strokes are kept, not their pixels, so painting costs kilobytes of history, previews on a proxy at proxy
/// resolution, and replays at full resolution on export.
/// </summary>
/// <remarks>
/// Strokes are painted in order onto a transparent layer: a painting stroke lays its colour over what is there, an
/// erasing one removes the layer's own paint, never the image below. The layer is then composited with its blend mode,
/// opacity and mask like any other. Coordinates are those of the image the layer is composited onto.
/// </remarks>
public sealed record BmagePaintLayer : BmageLayer
{
    private readonly BmagePaintStroke[] _strokes = [];

    /// <summary>A layer with no paint yet.</summary>
    public BmagePaintLayer()
    {
    }

    /// <param name="strokes">The strokes in the order they were painted.</param>
    public BmagePaintLayer(IReadOnlyList<BmagePaintStroke> strokes) => Strokes = strokes;

    /// <summary>The strokes in the order they were painted.</summary>
    public IReadOnlyList<BmagePaintStroke> Strokes
    {
        get => _strokes;
        init => _strokes = value?.ToArray() ?? throw new ArgumentNullException(nameof(Strokes));
    }

    /// <summary>The layer with one more stroke painted on top.</summary>
    public BmagePaintLayer Add(BmagePaintStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        return this with { Strokes = [.. _strokes, stroke] };
    }

    public override string Kind => "Paint";

    public override bool IsNoOp => base.IsNoOp || !_strokes.Any(s => !s.Erase);

    protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context)
    {
        var area = PaintedArea(target.Size);
        if (area.IsEmptyArea()) return;
        using var raster = Rasterize(area, context);
        BlendRows(target, area, context, y => raster.GetRow(y - area.Y));
    }

    /// <summary>The pixels of the image the strokes can reach.</summary>
    private Rectangle PaintedArea(Size size)
    {
        var bounds = RectangleF.Empty;
        foreach (var stroke in _strokes)
        {
            if (stroke.Erase) continue;
            bounds = bounds.IsEmpty ? stroke.GetBounds() : RectangleF.Union(bounds, stroke.GetBounds());
        }
        if (bounds.IsEmpty) return Rectangle.Empty;
        var area = Rectangle.FromLTRB((int)MathF.Floor(Math.Max(bounds.Left, -1)), (int)MathF.Floor(Math.Max(bounds.Top, -1)),
            (int)MathF.Ceiling(Math.Min(bounds.Right, size.Width + 1)), (int)MathF.Ceiling(Math.Min(bounds.Bottom, size.Height + 1)));
        area.Intersect(new Rectangle(Point.Empty, size));
        return area;
    }

    /// <summary>The strokes painted in order onto transparency, over <paramref name="area"/>. The caller owns the result.</summary>
    internal BmageImageBuffer Rasterize(Rectangle area, BmageOperationContext context)
    {
        var raster = BmageImageBuffer.Create(area.Width, area.Height, context.Allocator, clear: true, BmageImageLimits.Unrestricted);
        var length = area.Width * area.Height;
        var coverage = ArrayPool<float>.Shared.Rent(length);
        try
        {
            foreach (var stroke in _strokes)
            {
                context.ThrowIfCancellationRequested();
                var reach = Rectangle.Round(stroke.GetBounds());
                reach.Inflate(1, 1);
                reach.Intersect(area);
                if (reach.IsEmptyArea()) continue;
                var cells = coverage.AsSpan(0, length);
                ClearRows(cells, area, reach);
                stroke.RenderCoverage(cells, area, reach, context.CancellationToken);
                Lay(raster, cells, area, reach, stroke);
            }
            return raster;
        }
        catch
        {
            raster.Dispose();
            throw;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(coverage);
        }
    }

    private static void ClearRows(Span<float> coverage, Rectangle area, Rectangle reach)
    {
        for (var y = reach.Top; y < reach.Bottom; y++)
            coverage.Slice((y - area.Top) * area.Width + (reach.Left - area.Left), reach.Width).Clear();
    }

    /// <summary>Lays one stroke's paint onto the layer, or takes the layer's paint away for an eraser.</summary>
    private static void Lay(BmageImageBuffer raster, ReadOnlySpan<float> coverage, Rectangle area, Rectangle reach, BmagePaintStroke stroke)
    {
        var opacity = stroke.Opacity * (stroke.Erase ? 1f : stroke.Color.A / 255f);
        var color = new Vector3(stroke.Color.R, stroke.Color.G, stroke.Color.B);
        for (var y = reach.Top; y < reach.Bottom; y++)
        {
            var cells = coverage.Slice((y - area.Top) * area.Width, area.Width);
            var row = raster.GetRow(y - area.Top);
            for (var x = reach.Left; x < reach.Right; x++)
            {
                var a = cells[x - area.Left] * opacity;
                if (a <= 0) continue;
                ref var p = ref row[x - area.Left];
                var below = p.A / 255f;
                if (stroke.Erase)
                {
                    p = new BmageRgba32(p.R, p.G, p.B, BmageRgba32.ClampToByte(below * (1 - a) * 255f));
                    continue;
                }
                // Source over, in straight alpha.
                var outAlpha = a + below * (1 - a);
                var mixed = (color * a + new Vector3(p.R, p.G, p.B) * below * (1 - a)) / outAlpha;
                p = new BmageRgba32(BmageRgba32.ClampToByte(mixed.X), BmageRgba32.ClampToByte(mixed.Y), BmageRgba32.ClampToByte(mixed.Z), BmageRgba32.ClampToByte(outAlpha * 255f));
            }
        }
    }

    protected override BmageLayer ScaleCore(double scale) => this with { Strokes = [.. _strokes.Select(s => s.Scale(scale))] };

    /// <summary>Every point of every stroke is mapped exactly, and the brush sizes scale with the image.</summary>
    protected override BmageLayer TransformCore(Matrix3x2 matrix)
    {
        var scale = BmageAffineParts.Of(matrix).Scale;
        return this with { Strokes = [.. _strokes.Select(s => s with { Points = [.. s.Points.Select(p => Vector2.Transform(p, matrix))], Size = Math.Max(0.01f, s.Size * scale) })] };
    }
}
