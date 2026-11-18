using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Levels a tilted photo: rotates it by a small angle about its centre and crops to the largest rectangle of the
/// photo's own aspect ratio that the rotated photo covers, so the result has no empty corners. A horizon, a scanned
/// page, a building's edge.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="BmageRotateOperation"/>, which keeps or grows the canvas and fills the corners with a background, every
/// output pixel here comes from the photo, so an opaque photo stays opaque. Output pixels are sampled at their centres,
/// half a pixel inside the crop, which puts every sample at least half a pixel inside the rotated photo: a bilinear
/// sample's taps all land on it, and the only bicubic taps beyond it carry no weight or a negative one.
/// </para>
/// <para>
/// The crop is a fraction of the image, the same at every resolution, so a straighten previewed on a proxy shows the
/// same framing as the full-resolution export, to within a pixel of rounding.
/// </para>
/// </remarks>
public sealed class BmageStraightenOperation : BmageImageOperation, IBmageGeometricOperation
{
    /// <summary>The largest tilt a straighten corrects, either way. Larger turns are a rotation by quarter turns first.</summary>
    public const double MaxDegrees = 45;

    /// <param name="degrees">Clockwise rotation, between -45 and 45 degrees. Negative turns anticlockwise.</param>
    /// <param name="sampling">How the rotated photo is sampled.</param>
    /// <exception cref="ArgumentOutOfRangeException">The angle is not a finite number between -45 and 45.</exception>
    public BmageStraightenOperation(double degrees, BmageWarpSampling sampling = BmageWarpSampling.Bicubic)
    {
        if (!double.IsFinite(degrees) || Math.Abs(degrees) > MaxDegrees)
            throw new ArgumentOutOfRangeException(nameof(degrees), degrees, $"A straighten turns by at most {MaxDegrees} degrees either way.");
        Degrees = degrees;
        Sampling = sampling;
    }

    /// <summary>Clockwise rotation in degrees, between -45 and 45.</summary>
    public double Degrees { get; }

    public BmageWarpSampling Sampling { get; }

    public override string Name => "Straighten";

    public override bool IsIdentity => Degrees == 0;

    /// <summary>
    /// The fraction of each dimension the straightened image keeps: the largest scale at which a rectangle of the
    /// image's aspect ratio, centred, fits inside the image rotated by <paramref name="degrees"/>.
    /// </summary>
    public static double GetCropScale(Size inputSize, double degrees)
    {
        if (inputSize.Width <= 0 || inputSize.Height <= 0) throw new ArgumentOutOfRangeException(nameof(inputSize));
        var radians = Math.Abs(degrees) * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        double w = inputSize.Width, h = inputSize.Height;
        // A centred w*k x h*k rectangle, turned back by the angle, must stay within the image: its half-extents along
        // the image's axes are (w*k*cos + h*k*sin)/2 and (w*k*sin + h*k*cos)/2.
        return Math.Min(1, Math.Min(w / (w * cos + h * sin), h / (w * sin + h * cos)));
    }

    public override Size GetOutputSize(Size inputSize)
    {
        if (IsIdentity) return inputSize;
        var k = GetCropScale(inputSize, Degrees);
        // Rounded down, with a hair of slack for an exact fit, so the crop never reaches past the rotated photo.
        return new Size(Math.Max(1, (int)Math.Floor(inputSize.Width * k + 1e-6)), Math.Max(1, (int)Math.Floor(inputSize.Height * k + 1e-6)));
    }

    public Matrix3x2? GetTransform(Size inputSize) => IsIdentity ? Matrix3x2.Identity : Forward(inputSize, GetOutputSize(inputSize));

    /// <summary>Rotates about the input's centre and moves that centre to the output's.</summary>
    private Matrix3x2 Forward(Size input, Size output)
    {
        var centre = new Vector2(input.Width / 2f, input.Height / 2f);
        return Matrix3x2.CreateRotation((float)(Degrees * Math.PI / 180), centre)
            * Matrix3x2.CreateTranslation(output.Width / 2f - centre.X, output.Height / 2f - centre.Y);
    }

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var size = GetOutputSize(source.Size);
        Matrix3x2.Invert(Forward(source.Size, size), out var inverse);
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            BmageWarp.RunAffine(source, output, inverse, Sampling, default, context);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override string ToString() => $"Straighten({Degrees:0.##}°)";
}
