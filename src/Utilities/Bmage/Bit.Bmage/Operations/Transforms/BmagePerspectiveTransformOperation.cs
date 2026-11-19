using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Maps the four corners of the image to four arbitrary destination points (projective / perspective transform).
/// Useful for correcting keystone distortion in photographed documents.
/// </summary>
public sealed class BmagePerspectiveTransformOperation : BmageImageOperation, IBmageGeometricOperation
{
    private readonly PointF[] _corners;

    /// <param name="destinationCorners">Destination positions of the top-left, top-right, bottom-right and bottom-left corners, in output pixel coordinates.</param>
    /// <param name="outputSize">Output canvas size. Null uses the bounding box of the destination corners.</param>
    /// <param name="background">Colour for destination pixels that map outside the source.</param>
    /// <param name="sampling">Interpolation used when sampling the source.</param>
    public BmagePerspectiveTransformOperation(IReadOnlyList<PointF> destinationCorners, Size? outputSize = null, BmageRgba32 background = default, BmageWarpSampling sampling = BmageWarpSampling.Bilinear)
    {
        ArgumentNullException.ThrowIfNull(destinationCorners);
        if (destinationCorners.Count != 4) throw new ArgumentException("Exactly four corners are required.", nameof(destinationCorners));
        _corners = destinationCorners.ToArray();
        OutputSize = outputSize;
        Background = background;
        Sampling = sampling;
    }

    /// <summary>
    /// Creates a transform that straightens a quadrilateral region of the source (e.g. a photographed page) into an axis
    /// aligned rectangle of the given size.
    /// </summary>
    public static BmagePerspectiveTransformOperation Straighten(IReadOnlyList<PointF> sourceQuad, Size outputSize, BmageRgba32 background = default, BmageWarpSampling sampling = BmageWarpSampling.Bilinear)
    {
        ArgumentNullException.ThrowIfNull(sourceQuad);
        if (sourceQuad.Count != 4) throw new ArgumentException("Exactly four corners are required.", nameof(sourceQuad));
        return new BmagePerspectiveTransformOperation(sourceQuad, outputSize, background, sampling) { _straighten = true };
    }

    private bool _straighten;

    /// <summary>
    /// True for a transform made by <see cref="Straighten"/>, whose corners are a quadrilateral of the source to straighten
    /// rather than where the source's corners go.
    /// </summary>
    public bool IsStraightening => _straighten;

    public IReadOnlyList<PointF> DestinationCorners => _corners;
    public Size? OutputSize { get; }
    public BmageRgba32 Background { get; }
    public BmageWarpSampling Sampling { get; }
    public override string Name => "Perspective";

    public Matrix3x2? GetTransform(Size inputSize) => null;

    public override Size GetOutputSize(Size inputSize)
    {
        if (OutputSize is { } s) return s;
        float maxX = 0, maxY = 0;
        foreach (var c in _corners) { maxX = MathF.Max(maxX, c.X); maxY = MathF.Max(maxY, c.Y); }
        return new Size(Math.Max(1, (int)MathF.Ceiling(maxX)), Math.Max(1, (int)MathF.Ceiling(maxY)));
    }

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var size = GetOutputSize(source.Size);
        // Homography from destination quad to source rectangle (inverse mapping).
        var srcRect = new[] { new PointF(0, 0), new PointF(source.Width, 0), new PointF(source.Width, source.Height), new PointF(0, source.Height) };
        var outRect = new[] { new PointF(0, 0), new PointF(size.Width, 0), new PointF(size.Width, size.Height), new PointF(0, size.Height) };
        var h = _straighten
            ? BmageHomography.Compute(outRect, _corners)     // output rectangle -> source quad
            : BmageHomography.Compute(_corners, srcRect);    // destination quad -> source rectangle
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            BmageWarp.Run(source, output, p => h.Transform(p), Sampling, Background, context);
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    public override IBmageImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var s = (float)scale;
        var corners = _corners.Select(c => new PointF(c.X * s, c.Y * s)).ToArray();
        Size? os = OutputSize is { } o ? new Size(Math.Max(1, (int)Math.Round(o.Width * scale)), Math.Max(1, (int)Math.Round(o.Height * scale))) : null;
        return new BmagePerspectiveTransformOperation(corners, os, Background, Sampling) { _straighten = _straighten };
    }
}
