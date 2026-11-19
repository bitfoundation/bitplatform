using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A colour over the whole image, usually shaped by a mask and combined through a blend mode: a colour wash with
/// <see cref="BmageBlendMode.Color"/>, a duotone with <see cref="BmageBlendMode.Multiply"/> and <see cref="BmageBlendMode.Screen"/>,
/// a gradient fade through a <see cref="BmageLinearGradientMask"/>.
/// </summary>
/// <param name="Color">The fill colour, alpha included.</param>
public sealed record BmageFillLayer(BmageRgba32 Color) : BmageLayer
{
    public override string Kind => "Fill";

    public override bool IsNoOp => base.IsNoOp || (Color.A == 0 && BlendMode != BmageBlendMode.Copy);

    protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context)
    {
        var row = new BmageRgba32[target.Width];
        row.AsSpan().Fill(Color);
        BlendRows(target, target.Bounds, context, _ => row);
    }

    protected override BmageLayer ScaleCore(double scale) => this;
}
