namespace Bit.Bmage;

/// <summary>Inverts colours. <c>Amount</c> in [0,1].</summary>
public sealed class BmageInvertFilter : BmageColorMatrixOperation
{
    public BmageInvertFilter(float amount = 1f) : base(BmageColorMatrix.Invert(amount), "Invert") => Amount = amount;
    public float Amount { get; }
}
