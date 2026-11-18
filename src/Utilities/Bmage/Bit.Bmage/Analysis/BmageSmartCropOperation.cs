using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// Crops to an aspect ratio at the position <see cref="BmageSmartCrop"/> considers most interesting, instead of a fixed
/// anchor. The right default for avatars, thumbnails and product tiles.
/// </summary>
public sealed class BmageSmartCropOperation : BmageImageOperation
{
    public BmageSmartCropOperation(BmageAspectRatio ratio, BmageSmartCropOptions? options = null)
    {
        if (ratio.Width <= 0 || ratio.Height <= 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        Ratio = ratio;
        Options = options ?? BmageSmartCropOptions.Default;
    }

    public BmageAspectRatio Ratio { get; }
    public BmageSmartCropOptions Options { get; }

    public override string Name => "Smart crop";

    public override Size GetOutputSize(Size inputSize) => Ratio.FitInside(new Rectangle(Point.Empty, inputSize)).Size;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var result = BmageSmartCrop.FindCrop(source, Ratio, Rectangle.Empty, Options, context);
        context.ThrowIfCancellationRequested();
        return result.ToOperation().Apply(source, context);
    }

    public override string ToString() => $"SmartCrop({Ratio})";
}
