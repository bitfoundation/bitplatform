using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Resizes an image according to a <see cref="BmageResizeMode"/>.</summary>
public sealed class BmageResizeOperation : BmageImageOperation, IBmageGeometricOperation
{
    public BmageResizeOperation(int? width, int? height, BmageResizeMode mode = BmageResizeMode.Fit, BmageResamplingFilter filter = BmageResamplingFilter.Auto, BmageAnchor anchor = BmageAnchor.Center, BmageRgba32 background = default)
    {
        if (width is null && height is null) throw new ArgumentException("At least one of width or height must be specified.");
        if (width is <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        Mode = mode;
        Filter = filter;
        Anchor = anchor;
        Background = background;
    }

    /// <summary>Creates a resize to an exact size using <see cref="BmageResizeMode.Stretch"/>.</summary>
    public static BmageResizeOperation Exact(int width, int height, BmageResamplingFilter filter = BmageResamplingFilter.Auto) => new(width, height, BmageResizeMode.Stretch, filter);

    /// <summary>Creates a resize that scales proportionally by a factor.</summary>
    public static BmageResizeOperation Scale(Size source, double factor, BmageResamplingFilter filter = BmageResamplingFilter.Auto)
    {
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return new BmageResizeOperation(Math.Max(1, (int)Math.Round(source.Width * factor)), Math.Max(1, (int)Math.Round(source.Height * factor)), BmageResizeMode.Stretch, filter);
    }

    /// <summary>Creates a resize that shrinks an image to fit within the given bounds without enlarging it.</summary>
    public static BmageResizeOperation Max(int maxWidth, int maxHeight, BmageResamplingFilter filter = BmageResamplingFilter.Auto) => new(maxWidth, maxHeight, BmageResizeMode.Max, filter);

    public int? Width { get; }
    public int? Height { get; }
    public BmageResizeMode Mode { get; }
    public BmageResamplingFilter Filter { get; }
    public BmageAnchor Anchor { get; }
    /// <summary>Colour used for padding in <see cref="BmageResizeMode.Contain"/>.</summary>
    public BmageRgba32 Background { get; }

    public override string Name => "Resize";

    /// <summary>Resolves the concrete plan for an input of the given size.</summary>
    public BmageResizePlan GetPlan(Size inputSize) => BmageResizePlan.Compute(inputSize, Width, Height, Mode, Anchor);

    public override Size GetOutputSize(Size inputSize) => GetPlan(inputSize).CanvasSize;

    public Matrix3x2? GetTransform(Size inputSize)
    {
        var p = GetPlan(inputSize);
        var sx = (float)p.ScaleX;
        var sy = (float)p.ScaleY;
        // (x - src.X) * scale + dst.X
        return new Matrix3x2(sx, 0, 0, sy, p.DestinationRect.X - p.SourceRect.X * sx, p.DestinationRect.Y - p.SourceRect.Y * sy);
    }

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var plan = GetPlan(source.Size);
        if (plan.IsIdentity(source.Size))
            return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var needsBackground = plan.DestinationRect != new Rectangle(Point.Empty, plan.CanvasSize);
        var output = context.Allocate(plan.CanvasSize.Width, plan.CanvasSize.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            if (needsBackground) output.Fill(Background);
            BmageResampler.Resample(source, plan.SourceRect, output, plan.DestinationRect, Filter, context);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override IBmageImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        static int? S(int? v, double scale) => v is null ? null : Math.Max(1, (int)Math.Round(v.Value * scale));
        return new BmageResizeOperation(S(Width, scale), S(Height, scale), Mode, Filter, Anchor, Background);
    }

    public override string ToString() => $"Resize({Width?.ToString() ?? "auto"}x{Height?.ToString() ?? "auto"}, {Mode})";
}
