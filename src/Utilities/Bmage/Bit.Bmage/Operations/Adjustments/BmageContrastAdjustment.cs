namespace Bit.Bmage;

/// <summary>Scales contrast around mid grey. <c>Amount</c> is in [-1,1]; 0 is unchanged, -1 is flat grey.</summary>
public sealed class BmageContrastAdjustment : BmageColorMatrixOperation
{
    public BmageContrastAdjustment(float amount) : base(BmageColorMatrix.Contrast(1f + Math.Max(-1f, amount)), "Contrast") => Amount = amount;
    public float Amount { get; }
}
