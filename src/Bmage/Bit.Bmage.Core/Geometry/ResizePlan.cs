using System.Drawing;

namespace Bit.Bmage.Geometry;

/// <summary>
/// The result of resolving a resize request: the size of the output canvas, the region of the source
/// that is sampled and the region of the canvas it is drawn into. Every <see cref="ResizeMode"/>
/// reduces to this description, which lets a single resampling routine implement all modes.
/// </summary>
public readonly record struct ResizePlan(Size CanvasSize, Rectangle SourceRect, Rectangle DestinationRect)
{
    /// <summary>True when the plan is a no-op (canvas equals source and the whole source maps 1:1).</summary>
    public bool IsIdentity(Size source)
        => CanvasSize == source && SourceRect == new Rectangle(Point.Empty, source) && DestinationRect == SourceRect;

    /// <summary>Horizontal scale factor from source to destination.</summary>
    public double ScaleX => DestinationRect.Width / (double)SourceRect.Width;

    /// <summary>Vertical scale factor from source to destination.</summary>
    public double ScaleY => DestinationRect.Height / (double)SourceRect.Height;

    /// <summary>
    /// Resolves a resize request. At least one of <paramref name="width"/> / <paramref name="height"/> must be set; a missing
    /// dimension is derived from the source aspect ratio.
    /// </summary>
    public static ResizePlan Compute(Size source, int? width, int? height, ResizeMode mode = ResizeMode.Fit, Anchor anchor = Anchor.Center)
    {
        if (source.Width <= 0 || source.Height <= 0) throw new ArgumentOutOfRangeException(nameof(source), "Source size must be positive.");
        if (width is null && height is null) throw new ArgumentException("At least one of width or height must be specified.");
        if (width is <= 0 || height is <= 0) throw new ArgumentOutOfRangeException(width is <= 0 ? nameof(width) : nameof(height), "Target dimensions must be positive.");

        var aspect = source.Width / (double)source.Height;
        var full = new Rectangle(Point.Empty, source);

        // Derive the missing dimension proportionally; in that case the mode collapses to a proportional scale.
        if (width is null || height is null)
        {
            int w = width ?? Math.Max(1, (int)Math.Round(height!.Value * aspect));
            int h = height ?? Math.Max(1, (int)Math.Round(width!.Value / aspect));
            if (mode == ResizeMode.Max && (w > source.Width || h > source.Height))
                return new ResizePlan(source, full, full);
            if (mode == ResizeMode.Min && (w < source.Width || h < source.Height))
                return new ResizePlan(source, full, full);
            var s = new Size(w, h);
            return new ResizePlan(s, full, new Rectangle(Point.Empty, s));
        }

        var target = new Size(width.Value, height.Value);
        switch (mode)
        {
            case ResizeMode.Stretch:
                return new ResizePlan(target, full, new Rectangle(Point.Empty, target));

            case ResizeMode.Fit:
            case ResizeMode.Max:
            {
                var scale = Math.Min(target.Width / (double)source.Width, target.Height / (double)source.Height);
                if (mode == ResizeMode.Max && scale >= 1) return new ResizePlan(source, full, full);
                var s = Scaled(source, scale, target);
                return new ResizePlan(s, full, new Rectangle(Point.Empty, s));
            }

            case ResizeMode.Min:
            {
                var scale = Math.Max(target.Width / (double)source.Width, target.Height / (double)source.Height);
                if (scale <= 1) return new ResizePlan(source, full, full);
                var s = Scaled(source, scale, null);
                return new ResizePlan(s, full, new Rectangle(Point.Empty, s));
            }

            case ResizeMode.Contain:
            {
                var scale = Math.Min(target.Width / (double)source.Width, target.Height / (double)source.Height);
                var s = Scaled(source, scale, target);
                return new ResizePlan(target, full, anchor.Place(s, target));
            }

            case ResizeMode.Cover:
            {
                var scale = Math.Max(target.Width / (double)source.Width, target.Height / (double)source.Height);
                // Region of the source that maps onto the target.
                var cropW = Math.Min(source.Width, Math.Max(1, (int)Math.Round(target.Width / scale)));
                var cropH = Math.Min(source.Height, Math.Max(1, (int)Math.Round(target.Height / scale)));
                var crop = anchor.Place(new Size(cropW, cropH), full);
                return new ResizePlan(target, crop, new Rectangle(Point.Empty, target));
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static Size Scaled(Size source, double scale, Size? clamp)
    {
        var w = Math.Max(1, (int)Math.Round(source.Width * scale));
        var h = Math.Max(1, (int)Math.Round(source.Height * scale));
        if (clamp is { } c)
        {
            w = Math.Min(w, c.Width);
            h = Math.Min(h, c.Height);
        }
        return new Size(w, h);
    }
}
