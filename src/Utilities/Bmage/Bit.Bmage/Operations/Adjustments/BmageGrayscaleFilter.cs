namespace Bit.Bmage;

/// <summary>Converts towards grayscale. <c>Amount</c> in [0,1]; 1 is fully grey.</summary>
public sealed class BmageGrayscaleFilter : BmageColorMatrixOperation
{
    public BmageGrayscaleFilter(float amount = 1f) : base(BmageColorMatrix.Grayscale(amount), "Grayscale") => Amount = amount;
    public float Amount { get; }
}
