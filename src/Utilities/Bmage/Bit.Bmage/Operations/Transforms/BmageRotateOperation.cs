using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Rotates by an arbitrary angle around the image centre. Multiples of 90° are delegated to the lossless <see cref="BmageOrientationOperation"/>.</summary>
public sealed class BmageRotateOperation : BmageImageOperation, IBmageGeometricOperation
{
    public BmageRotateOperation(double degrees, BmageRgba32 background = default, bool expandCanvas = true, BmageWarpSampling sampling = BmageWarpSampling.Bicubic)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        Degrees = ((degrees % 360) + 360) % 360;
        Background = background;
        ExpandCanvas = expandCanvas;
        Sampling = sampling;
    }

    /// <summary>Clockwise rotation in degrees, normalised to [0,360).</summary>
    public double Degrees { get; }
    public BmageRgba32 Background { get; }
    /// <summary>When true the canvas grows to contain the whole rotated image; otherwise the original size is kept and corners are clipped.</summary>
    public bool ExpandCanvas { get; }
    public BmageWarpSampling Sampling { get; }

    public override string Name => "Rotate";
    public override bool IsIdentity => Degrees == 0;

    /// <summary>
    /// A quarter turn can be done by moving pixels, but a 90° or 270° turn swaps the dimensions, so it only matches this
    /// operation when the canvas may grow or the image is square.
    /// </summary>
    private BmageOrientationOperation? Lossless(Size input)
    {
        if (Degrees % 90 != 0) return null;
        if (!ExpandCanvas && Degrees % 180 != 0 && input.Width != input.Height) return null;
        return BmageOrientationOperation.FromRotation((int)Degrees);
    }

    private BmageAffineTransformOperation BuildAffine(Size input)
    {
        var rad = (float)(Degrees * Math.PI / 180);
        var center = new Vector2(input.Width / 2f, input.Height / 2f);
        var m = Matrix3x2.CreateRotation(rad, center);
        if (ExpandCanvas) return new BmageAffineTransformOperation(m, null, Background, Sampling, Name);
        return new BmageAffineTransformOperation(m, input, Background, Sampling, Name);
    }

    public override Size GetOutputSize(Size inputSize)
        => Lossless(inputSize) is { } o ? o.GetOutputSize(inputSize) : BuildAffine(inputSize).GetOutputSize(inputSize);

    public Matrix3x2? GetTransform(Size inputSize)
        => Lossless(inputSize) is { } o ? o.GetTransform(inputSize) : BuildAffine(inputSize).GetTransform(inputSize);

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
        => Lossless(source.Size) is { } o ? o.Apply(source, context) : BuildAffine(source.Size).Apply(source, context);

    public override string ToString() => $"Rotate({Degrees:0.##}°)";
}
