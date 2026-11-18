namespace Bit.Bmage;

/// <summary>Adds a constant to the RGB channels. <c>Amount</c> is in [-1,1]; 0 is unchanged.</summary>
public sealed class BmageBrightnessAdjustment : BmageColorMatrixOperation
{
    public BmageBrightnessAdjustment(float amount) : base(BmageColorMatrix.Translate(amount, amount, amount), "Brightness") => Amount = amount;
    public float Amount { get; }
}
