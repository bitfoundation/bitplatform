namespace Bit.Bmage;

/// <summary>A colour and how much of the image it accounts for.</summary>
public readonly record struct BmageColorFrequency(BmageRgba32 Color, double Fraction)
{
    /// <summary>The share as a percentage, for display.</summary>
    public double Percent => Fraction * 100;
}
