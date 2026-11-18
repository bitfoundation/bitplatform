namespace Bit.Bmage;

/// <summary>Applies a sepia tone. <c>Amount</c> in [0,1].</summary>
public sealed class BmageSepiaFilter : BmageColorMatrixOperation
{
    public BmageSepiaFilter(float amount = 1f) : base(BmageColorMatrix.Sepia(amount), "Sepia") => Amount = amount;
    public float Amount { get; }
}
