namespace Bit.Bmage;

/// <summary>Rotates hue by the given number of degrees.</summary>
public sealed class BmageHueAdjustment : BmageColorMatrixOperation
{
    public BmageHueAdjustment(float degrees) : base(BmageColorMatrix.HueRotate(degrees), "Hue") => Degrees = degrees;
    public float Degrees { get; }
}
