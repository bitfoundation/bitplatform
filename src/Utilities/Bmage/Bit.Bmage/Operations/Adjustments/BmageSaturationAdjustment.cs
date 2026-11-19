namespace Bit.Bmage;

/// <summary>Adjusts colour saturation. <c>Amount</c> in [-1,1]; -1 is grayscale, 0 unchanged.</summary>
public sealed class BmageSaturationAdjustment : BmageColorMatrixOperation
{
    public BmageSaturationAdjustment(float amount) : base(BmageColorMatrix.Saturate(1f + Math.Max(-1f, amount)), "Saturation") => Amount = amount;
    public float Amount { get; }
}
