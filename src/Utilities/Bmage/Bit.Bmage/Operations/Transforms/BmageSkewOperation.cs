using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Skews (shears) the image by the given angles in degrees along each axis.</summary>
public sealed class BmageSkewOperation : BmageImageOperation, IBmageGeometricOperation
{
    /// <exception cref="ArgumentOutOfRangeException">
    /// The angles do not describe an invertible shear. A shear is singular at 90 degrees, and at any pair of angles
    /// whose tangents multiply to 1; such a transform collapses the image to a line and cannot be sampled.
    /// </exception>
    public BmageSkewOperation(double degreesX, double degreesY, BmageRgba32 background = default, BmageWarpSampling sampling = BmageWarpSampling.Bilinear)
    {
        Validate(degreesX, nameof(degreesX));
        Validate(degreesY, nameof(degreesY));
        var determinant = 1 - Math.Tan(degreesX * Math.PI / 180) * Math.Tan(degreesY * Math.PI / 180);
        if (Math.Abs(determinant) < 1e-6 || double.IsNaN(determinant))
            throw new ArgumentOutOfRangeException(nameof(degreesX), $"A skew of ({degreesX}, {degreesY}) degrees is singular and cannot be inverted.");

        DegreesX = degreesX; DegreesY = degreesY; Background = background; Sampling = sampling;

        static void Validate(double degrees, string name)
        {
            if (double.IsNaN(degrees) || double.IsInfinity(degrees))
                throw new ArgumentOutOfRangeException(name, "Skew angle must be a finite number.");
            // tan is unbounded at 90 degrees plus any multiple of 180.
            if (Math.Abs(((degrees % 180) + 180) % 180 - 90) < 1e-6)
                throw new ArgumentOutOfRangeException(name, $"A skew of {degrees} degrees is singular and cannot be inverted.");
        }
    }

    public double DegreesX { get; }
    public double DegreesY { get; }
    public BmageRgba32 Background { get; }
    public BmageWarpSampling Sampling { get; }
    public override string Name => "Skew";
    public override bool IsIdentity => DegreesX == 0 && DegreesY == 0;

    private BmageAffineTransformOperation Build()
    {
        var m = Matrix3x2.CreateSkew((float)(DegreesX * Math.PI / 180), (float)(DegreesY * Math.PI / 180));
        return new BmageAffineTransformOperation(m, null, Background, Sampling, Name);
    }

    public override Size GetOutputSize(Size inputSize) => Build().GetOutputSize(inputSize);
    public Matrix3x2? GetTransform(Size inputSize) => Build().GetTransform(inputSize);
    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context) => Build().Apply(source, context);
}
