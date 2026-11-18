namespace Bit.Bmage;

/// <summary>Shifts towards magenta (positive) or green (negative). <c>Amount</c> in [-1,1].</summary>
public sealed class BmageTintAdjustment : BmageColorMatrixOperation
{
    public BmageTintAdjustment(float amount) : base(BmageColorMatrix.Scale(1f, 1f - 0.25f * amount, 1f), "Tint") => Amount = amount;
    public float Amount { get; }
}
