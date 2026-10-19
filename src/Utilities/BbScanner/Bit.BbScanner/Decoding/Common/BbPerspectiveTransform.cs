namespace Bit.BbScanner;

/// <summary>
/// A 2D projective transform, used to map the sampling grid of a matrix symbol onto the
/// quadrilateral the detector found in the image.
/// </summary>
/// <remarks>
/// A camera looking at a barcode from an angle produces a perspective distortion that an affine
/// transform cannot model: opposite edges of the symbol are no longer parallel. The eight degree
/// of freedom projective transform recovered here is what lets the sampler read a symbol held at
/// a realistic angle rather than flat on.
/// </remarks>
public sealed class BbPerspectiveTransform
{
    private readonly float _a11;
    private readonly float _a12;
    private readonly float _a13;
    private readonly float _a21;
    private readonly float _a22;
    private readonly float _a23;
    private readonly float _a31;
    private readonly float _a32;
    private readonly float _a33;

    private BbPerspectiveTransform(
        float a11, float a21, float a31,
        float a12, float a22, float a32,
        float a13, float a23, float a33)
    {
        _a11 = a11; _a12 = a12; _a13 = a13;
        _a21 = a21; _a22 = a22; _a23 = a23;
        _a31 = a31; _a32 = a32; _a33 = a33;
    }

    /// <summary>Builds the transform that maps the four quadrilateral points onto the four square points.</summary>
    /// <param name="x0">First quadrilateral point, x.</param>
    /// <param name="y0">First quadrilateral point, y.</param>
    /// <param name="x1">Second quadrilateral point, x.</param>
    /// <param name="y1">Second quadrilateral point, y.</param>
    /// <param name="x2">Third quadrilateral point, x.</param>
    /// <param name="y2">Third quadrilateral point, y.</param>
    /// <param name="x3">Fourth quadrilateral point, x.</param>
    /// <param name="y3">Fourth quadrilateral point, y.</param>
    /// <param name="x0p">First square point, x.</param>
    /// <param name="y0p">First square point, y.</param>
    /// <param name="x1p">Second square point, x.</param>
    /// <param name="y1p">Second square point, y.</param>
    /// <param name="x2p">Third square point, x.</param>
    /// <param name="y2p">Third square point, y.</param>
    /// <param name="x3p">Fourth square point, x.</param>
    /// <param name="y3p">Fourth square point, y.</param>
    public static BbPerspectiveTransform QuadrilateralToQuadrilateral(
        float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
        float x0p, float y0p, float x1p, float y1p, float x2p, float y2p, float x3p, float y3p)
    {
        var qToS = QuadrilateralToSquare(x0, y0, x1, y1, x2, y2, x3, y3);
        var sToQ = SquareToQuadrilateral(x0p, y0p, x1p, y1p, x2p, y2p, x3p, y3p);
        return sToQ.Times(qToS);
    }

    /// <summary>Builds the transform mapping the unit square onto the given quadrilateral.</summary>
    /// <param name="x0">Corner at (0,0), x.</param>
    /// <param name="y0">Corner at (0,0), y.</param>
    /// <param name="x1">Corner at (1,0), x.</param>
    /// <param name="y1">Corner at (1,0), y.</param>
    /// <param name="x2">Corner at (1,1), x.</param>
    /// <param name="y2">Corner at (1,1), y.</param>
    /// <param name="x3">Corner at (0,1), x.</param>
    /// <param name="y3">Corner at (0,1), y.</param>
    public static BbPerspectiveTransform SquareToQuadrilateral(
        float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        var dx3 = x0 - x1 + x2 - x3;
        var dy3 = y0 - y1 + y2 - y3;
        if (dx3 == 0.0f && dy3 == 0.0f)
        {
            // The mapping is affine: opposite edges are parallel.
            return new BbPerspectiveTransform(
                x1 - x0, x2 - x1, x0,
                y1 - y0, y2 - y1, y0,
                0.0f, 0.0f, 1.0f);
        }

        var dx1 = x1 - x2;
        var dx2 = x3 - x2;
        var dy1 = y1 - y2;
        var dy2 = y3 - y2;
        var denominator = (dx1 * dy2) - (dx2 * dy1);
        var a13 = ((dx3 * dy2) - (dx2 * dy3)) / denominator;
        var a23 = ((dx1 * dy3) - (dx3 * dy1)) / denominator;
        return new BbPerspectiveTransform(
            x1 - x0 + (a13 * x1), x3 - x0 + (a23 * x3), x0,
            y1 - y0 + (a13 * y1), y3 - y0 + (a23 * y3), y0,
            a13, a23, 1.0f);
    }

    /// <summary>Builds the transform mapping the given quadrilateral onto the unit square.</summary>
    /// <param name="x0">Corner mapping to (0,0), x.</param>
    /// <param name="y0">Corner mapping to (0,0), y.</param>
    /// <param name="x1">Corner mapping to (1,0), x.</param>
    /// <param name="y1">Corner mapping to (1,0), y.</param>
    /// <param name="x2">Corner mapping to (1,1), x.</param>
    /// <param name="y2">Corner mapping to (1,1), y.</param>
    /// <param name="x3">Corner mapping to (0,1), x.</param>
    /// <param name="y3">Corner mapping to (0,1), y.</param>
    public static BbPerspectiveTransform QuadrilateralToSquare(
        float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3) =>
        SquareToQuadrilateral(x0, y0, x1, y1, x2, y2, x3, y3).BuildAdjoint();

    private BbPerspectiveTransform BuildAdjoint() => new(
        (_a22 * _a33) - (_a23 * _a32),
        (_a23 * _a31) - (_a21 * _a33),
        (_a21 * _a32) - (_a22 * _a31),
        (_a13 * _a32) - (_a12 * _a33),
        (_a11 * _a33) - (_a13 * _a31),
        (_a12 * _a31) - (_a11 * _a32),
        (_a12 * _a23) - (_a13 * _a22),
        (_a13 * _a21) - (_a11 * _a23),
        (_a11 * _a22) - (_a12 * _a21));

    private BbPerspectiveTransform Times(BbPerspectiveTransform other) => new(
        (_a11 * other._a11) + (_a21 * other._a12) + (_a31 * other._a13),
        (_a11 * other._a21) + (_a21 * other._a22) + (_a31 * other._a23),
        (_a11 * other._a31) + (_a21 * other._a32) + (_a31 * other._a33),
        (_a12 * other._a11) + (_a22 * other._a12) + (_a32 * other._a13),
        (_a12 * other._a21) + (_a22 * other._a22) + (_a32 * other._a23),
        (_a12 * other._a31) + (_a22 * other._a32) + (_a32 * other._a33),
        (_a13 * other._a11) + (_a23 * other._a12) + (_a33 * other._a13),
        (_a13 * other._a21) + (_a23 * other._a22) + (_a33 * other._a23),
        (_a13 * other._a31) + (_a23 * other._a32) + (_a33 * other._a33));

    /// <summary>Transforms a span of interleaved x, y pairs in place.</summary>
    /// <param name="points">Interleaved coordinates: x0, y0, x1, y1 and so on.</param>
    public void TransformPoints(Span<float> points)
    {
        for (var i = 0; i < points.Length; i += 2)
        {
            var x = points[i];
            var y = points[i + 1];
            var denominator = (_a13 * x) + (_a23 * y) + _a33;
            points[i] = (((_a11 * x) + (_a21 * y) + _a31) / denominator);
            points[i + 1] = (((_a12 * x) + (_a22 * y) + _a32) / denominator);
        }
    }

    /// <summary>Transforms two parallel coordinate arrays in place.</summary>
    /// <param name="xValues">X coordinates.</param>
    /// <param name="yValues">Y coordinates.</param>
    public void TransformPoints(Span<float> xValues, Span<float> yValues)
    {
        for (var i = 0; i < xValues.Length; i++)
        {
            var x = xValues[i];
            var y = yValues[i];
            var denominator = (_a13 * x) + (_a23 * y) + _a33;
            xValues[i] = ((_a11 * x) + (_a21 * y) + _a31) / denominator;
            yValues[i] = ((_a12 * x) + (_a22 * y) + _a32) / denominator;
        }
    }

    /// <summary>Transforms a single point.</summary>
    /// <param name="point">The point to transform.</param>
    public BbScanPoint Transform(BbScanPoint point)
    {
        var denominator = (_a13 * point.X) + (_a23 * point.Y) + _a33;
        return new BbScanPoint(
            ((_a11 * point.X) + (_a21 * point.Y) + _a31) / denominator,
            ((_a12 * point.X) + (_a22 * point.Y) + _a32) / denominator);
    }
}
