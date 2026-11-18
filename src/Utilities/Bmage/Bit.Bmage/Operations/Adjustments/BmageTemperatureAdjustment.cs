namespace Bit.Bmage;

/// <summary>Warms (positive) or cools (negative) the image. <c>Amount</c> in [-1,1].</summary>
public sealed class BmageTemperatureAdjustment : BmageColorMatrixOperation
{
    public BmageTemperatureAdjustment(float amount) : base(BmageColorMatrix.Scale(1f + 0.25f * amount, 1f, 1f - 0.25f * amount), "Temperature") => Amount = amount;
    public float Amount { get; }
}
