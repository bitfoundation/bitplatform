namespace Bit.Bmage;

/// <summary>Multiplies RGB by 2^EV, mimicking camera exposure compensation. Typical range [-5,5].</summary>
public sealed class BmageExposureAdjustment : BmageColorMatrixOperation
{
    public BmageExposureAdjustment(float ev) : base(BmageColorMatrix.Scale(MathF.Pow(2f, ev), MathF.Pow(2f, ev), MathF.Pow(2f, ev)), "Exposure") => Ev = ev;
    public float Ev { get; }
}
