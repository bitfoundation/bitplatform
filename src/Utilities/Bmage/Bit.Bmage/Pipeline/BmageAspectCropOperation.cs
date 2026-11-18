using System.Drawing;

namespace Bit.Bmage;

/// <summary>Crops to the largest region with a given aspect ratio, positioned by an anchor.</summary>
public sealed class BmageAspectCropOperation : BmageImageOperation, IBmageGeometricOperation
{
    public BmageAspectCropOperation(BmageAspectRatio ratio, BmageAnchor anchor = BmageAnchor.Center)
    {
        if (ratio.Width <= 0 || ratio.Height <= 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        Ratio = ratio;
        Anchor = anchor;
    }

    public BmageAspectRatio Ratio { get; }
    public BmageAnchor Anchor { get; }
    public override string Name => "Crop to aspect";

    private BmageCropOperation Resolve(Size input) => new(Ratio.FitInside(new Rectangle(Point.Empty, input), Anchor));

    public override Size GetOutputSize(Size inputSize) => Resolve(inputSize).GetOutputSize(inputSize);
    public System.Numerics.Matrix3x2? GetTransform(Size inputSize) => Resolve(inputSize).GetTransform(inputSize);
    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context) => Resolve(source.Size).Apply(source, context);
    public override string ToString() => $"CropToAspect({Ratio})";
}
