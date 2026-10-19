using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// A resize immediately followed by a crop, executed as a single resample of just the region that survives the crop.
/// Produced by the pipeline optimiser; the result is bit-identical to running the two operations in sequence because
/// the filter kernels are still computed for the full resize output.
/// </summary>
public sealed class ResizeCropOperation : ImageOperation, IGeometricOperation
{
    internal ResizeCropOperation(ResizeOperation resize, Rectangle crop)
    {
        Resize = resize ?? throw new ArgumentNullException(nameof(resize));
        Crop = crop;
    }

    /// <summary>The resize step.</summary>
    public ResizeOperation Resize { get; }

    /// <summary>The crop rectangle, in the coordinate space of the resize output.</summary>
    public Rectangle Crop { get; }

    public override string Name => "Resize + crop";

    /// <summary>
    /// Returns a fused operation when the pair can be executed as one resample, otherwise null. Fusion requires the
    /// resize to fill its whole canvas (no letterbox padding), since padded pixels are not produced by the resampler.
    /// </summary>
    internal static ResizeCropOperation? TryCreate(ResizeOperation resize, CropOperation crop, Size inputSize)
    {
        var plan = resize.GetPlan(inputSize);
        if (plan.DestinationRect != new Rectangle(Point.Empty, plan.CanvasSize)) return null;
        var rect = crop.GetEffectiveRectangle(plan.CanvasSize);
        if (rect.IsEmptyArea()) return null;
        if (rect.Size == plan.CanvasSize) return null;   // nothing saved
        if (plan.IsIdentity(inputSize)) return null;     // a plain crop is already cheaper
        // Store the crop as written, not as clipped here: Resolve clips it against the real canvas at run time, which
        // matters if the size used for planning turns out to be wrong.
        return new ResizeCropOperation(resize, crop.Rectangle);
    }

    private (ResizePlan Plan, Rectangle Rect) Resolve(Size inputSize)
    {
        var plan = Resize.GetPlan(inputSize);
        var rect = Crop;
        rect.Intersect(new Rectangle(Point.Empty, plan.CanvasSize));
        return (plan, rect);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The crop does not intersect the resized image.</exception>
    public override Size GetOutputSize(Size inputSize)
    {
        var (_, rect) = Resolve(inputSize);
        if (rect.IsEmptyArea()) throw new ArgumentException($"Crop rectangle {Crop} does not intersect the resized image.");
        return rect.Size;
    }

    public Matrix3x2? GetTransform(Size inputSize)
    {
        var (_, rect) = Resolve(inputSize);
        var resizeMatrix = Resize.GetTransform(inputSize) ?? Matrix3x2.Identity;
        return resizeMatrix * Matrix3x2.CreateTranslation(-rect.X, -rect.Y);
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var (plan, rect) = Resolve(source.Size);
        if (rect.IsEmptyArea()) throw new ArgumentException("The crop rectangle does not intersect the resized image.");
        var output = context.Allocate(rect.Width, rect.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            // Shift the destination rectangle so the crop origin lands at (0,0) of the output. The resampler still
            // builds kernels for the full canvas, so the pixels match a full resize followed by a crop exactly.
            var shifted = new Rectangle(-rect.X, -rect.Y, plan.CanvasSize.Width, plan.CanvasSize.Height);
            Resampler.Resample(source, plan.SourceRect, output, shifted, Resize.Filter, context);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override IImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var resize = (ResizeOperation)Resize.ForScale(scale);
        var x = (int)Math.Round(Crop.X * scale);
        var y = (int)Math.Round(Crop.Y * scale);
        var w = Math.Max(1, (int)Math.Round(Crop.Right * scale) - x);
        var h = Math.Max(1, (int)Math.Round(Crop.Bottom * scale) - y);
        return new ResizeCropOperation(resize, new Rectangle(x, y, w, h));
    }

    public override string ToString() => $"{Resize} + Crop({Crop.X},{Crop.Y},{Crop.Width},{Crop.Height})";
}
