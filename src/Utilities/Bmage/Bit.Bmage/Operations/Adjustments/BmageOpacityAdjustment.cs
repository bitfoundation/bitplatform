namespace Bit.Bmage;

/// <summary>Multiplies the alpha channel. <c>Opacity</c> in [0,1].</summary>
public sealed class BmageOpacityAdjustment : BmageColorMatrixOperation
{
    public BmageOpacityAdjustment(float opacity) : base(BmageColorMatrix.Scale(1f, 1f, 1f, Math.Clamp(opacity, 0f, 1f)), "Opacity") => Opacity = opacity;
    public float Opacity { get; }
}
