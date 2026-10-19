using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// Sampling method used by warp operations (arbitrary rotation, skew, perspective).
/// </summary>
/// <remarks>
/// The choice dominates the cost of a warp, because the sampler runs once per output pixel. Measured on a 12
/// megapixel image rotated by 37 degrees: <see cref="NearestNeighbor"/> about 40 MP/s, <see cref="Bilinear"/> about
/// 27 MP/s, <see cref="Bicubic"/> about 8 MP/s. A rotation by a multiple of 90 degrees never samples at all and runs
/// at roughly 450 MP/s. Interactive editing stays responsive because previews render against a downscaled proxy;
/// pick a cheaper sampler when warping a full-resolution image on the UI thread.
/// </remarks>
public enum WarpSampling
{
    Bilinear = 0,
    NearestNeighbor,
    Bicubic,
}

/// <summary>Shared inverse-mapping warp engine used by affine and perspective operations.</summary>
internal static class Warp
{
    private static Vector4 Premultiplied(Rgba32 background)
    {
        const float inv = 1f / 255f;
        return new Vector4(
            background.R * inv * background.A * inv,
            background.G * inv * background.A * inv,
            background.B * inv * background.A * inv,
            background.A * inv);
    }

    /// <summary>
    /// Fills <paramref name="output"/> by mapping each destination pixel centre through <paramref name="inverse"/> to a source
    /// location and sampling with premultiplied alpha. Pixels mapping outside the source get <paramref name="background"/>.
    /// </summary>
    /// <remarks>
    /// The general path, for mappings that are not affine. Affine transforms should use <see cref="RunAffine"/>.
    /// </remarks>
    public static void Run(ImageBuffer source, ImageBuffer output, Func<Vector2, Vector2> inverse, WarpSampling sampling, Rgba32 background, OperationContext context)
    {
        int sw = source.Width, sh = source.Height;
        var src = source.Pixels;
        var bg = Premultiplied(background);

        for (var y = 0; y < output.Height; y++)
        {
            if ((y & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)output.Height); }
            var dst = output.GetRow(y);
            for (var x = 0; x < output.Width; x++)
            {
                var p = inverse(new Vector2(x + 0.5f, y + 0.5f));
                dst[x] = Store(Sample(src, sw, sh, p.X - 0.5f, p.Y - 0.5f, bg, sampling));
            }
        }
        context.ReportProgress(1);
    }

    /// <summary>
    /// The affine fast path. Exploits the fact that an affine map advances by a constant vector along a scanline, so
    /// the source position costs a multiply-add per pixel instead of a delegate call and a full matrix transform, and
    /// the sampling mode is resolved once per image rather than branched on per pixel. The reassociated arithmetic can
    /// put a pixel on the other side of a rounding boundary, so results match <see cref="Run"/> to within one unit per
    /// channel rather than exactly.
    /// </summary>
    /// <remarks>
    /// Measured on a 12 megapixel image rotated 37 degrees: nearest 359 ms to 309 ms, bilinear 617 ms to 453 ms,
    /// bicubic 1497 ms to 1474 ms. The saving is real but only visible when sampling is not the dominant cost;
    /// bicubic's sixteen taps per pixel swamp it. See <see cref="WarpSampling"/> for the trade-off.
    /// </remarks>
    public static void RunAffine(ImageBuffer source, ImageBuffer output, Matrix3x2 inverse, WarpSampling sampling, Rgba32 background, OperationContext context)
    {
        int sw = source.Width, sh = source.Height;
        var src = source.Pixels;
        var bg = Premultiplied(background);
        // Derivative of the mapped position with respect to the destination column.
        var stepX = inverse.M11;
        var stepY = inverse.M12;
        var width = output.Width;

        for (var y = 0; y < output.Height; y++)
        {
            if ((y & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)output.Height); }
            var dst = output.GetRow(y);
            // Recomputed per row rather than accumulated, so a long row cannot drift.
            var origin = Vector2.Transform(new Vector2(0.5f, y + 0.5f), inverse);
            var baseX = origin.X - 0.5f;
            var baseY = origin.Y - 0.5f;

            switch (sampling)
            {
                case WarpSampling.NearestNeighbor:
                    for (var x = 0; x < width; x++)
                    {
                        var ix = (int)MathF.Floor(baseX + x * stepX + 0.5f);
                        var iy = (int)MathF.Floor(baseY + x * stepY + 0.5f);
                        dst[x] = Store((uint)ix < (uint)sw && (uint)iy < (uint)sh ? Load(src[iy * sw + ix]) : bg);
                    }
                    break;
                case WarpSampling.Bicubic:
                    for (var x = 0; x < width; x++)
                        dst[x] = Store(SampleBicubic(src, sw, sh, baseX + x * stepX, baseY + x * stepY, bg));
                    break;
                default:
                    for (var x = 0; x < width; x++)
                        dst[x] = Store(SampleBilinear(src, sw, sh, baseX + x * stepX, baseY + x * stepY, bg));
                    break;
            }
        }
        context.ReportProgress(1);
    }

    private static Vector4 Sample(ReadOnlySpan<Rgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg, WarpSampling sampling)
    {
        switch (sampling)
        {
            case WarpSampling.NearestNeighbor:
            {
                var ix = (int)MathF.Floor(sx + 0.5f);
                var iy = (int)MathF.Floor(sy + 0.5f);
                return (uint)ix < (uint)sw && (uint)iy < (uint)sh ? Load(src[iy * sw + ix]) : bg;
            }
            case WarpSampling.Bicubic:
                return SampleBicubic(src, sw, sh, sx, sy, bg);
            default:
                return SampleBilinear(src, sw, sh, sx, sy, bg);
        }
    }

    private static Vector4 Load(Rgba32 p)
    {
        const float inv = 1f / 255f;
        var a = p.A * inv;
        return new Vector4(p.R * inv * a, p.G * inv * a, p.B * inv * a, a);
    }

    private static Rgba32 Store(Vector4 p)
    {
        var a = p.W;
        if (a > 1e-5f)
        {
            var ia = 1f / a;
            p = new Vector4(p.X * ia, p.Y * ia, p.Z * ia, a);
        }
        else return Rgba32.Transparent;
        var v = Vector4.Clamp(p, Vector4.Zero, Vector4.One) * 255f + new Vector4(0.5f);
        return new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
    }

    private static Vector4 Fetch(ReadOnlySpan<Rgba32> src, int sw, int sh, int x, int y, in Vector4 bg)
        => (uint)x < (uint)sw && (uint)y < (uint)sh ? Load(src[y * sw + x]) : bg;

    private static Vector4 SampleBilinear(ReadOnlySpan<Rgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -1f || sy <= -1f || sx >= sw || sy >= sh) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        var c00 = Fetch(src, sw, sh, x0, y0, bg);
        var c10 = Fetch(src, sw, sh, x0 + 1, y0, bg);
        var c01 = Fetch(src, sw, sh, x0, y0 + 1, bg);
        var c11 = Fetch(src, sw, sh, x0 + 1, y0 + 1, bg);
        var top = c00 + (c10 - c00) * fx;
        var bottom = c01 + (c11 - c01) * fx;
        return top + (bottom - top) * fy;
    }

    private static float Cubic(float x)
    {
        const float a = -0.5f;
        x = MathF.Abs(x);
        if (x < 1f) return ((a + 2f) * x - (a + 3f)) * x * x + 1f;
        if (x < 2f) return ((a * x - 5f * a) * x + 8f * a) * x - 4f * a;
        return 0f;
    }

    private static Vector4 SampleBicubic(ReadOnlySpan<Rgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -2f || sy <= -2f || sx >= sw + 1 || sy >= sh + 1) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        Span<float> wx = stackalloc float[4];
        Span<float> wy = stackalloc float[4];
        for (var i = 0; i < 4; i++)
        {
            wx[i] = Cubic(fx - (i - 1));
            wy[i] = Cubic(fy - (i - 1));
        }
        var acc = Vector4.Zero;
        for (var j = 0; j < 4; j++)
        {
            var row = Vector4.Zero;
            for (var i = 0; i < 4; i++) row += Fetch(src, sw, sh, x0 - 1 + i, y0 - 1 + j, bg) * wx[i];
            acc += row * wy[j];
        }
        return acc;
    }
}

/// <summary>
/// Applies an arbitrary affine transform (rotation, scale, skew, translation). The output canvas is either given explicitly
/// or computed to contain the transformed bounds of the input.
/// </summary>
public sealed class AffineTransformOperation : ImageOperation, IGeometricOperation
{
    public AffineTransformOperation(Matrix3x2 matrix, Size? outputSize = null, Rgba32 background = default, WarpSampling sampling = WarpSampling.Bilinear, string? name = null)
    {
        if (!Matrix3x2.Invert(matrix, out _)) throw new ArgumentException("The matrix is not invertible.", nameof(matrix));
        Matrix = matrix;
        OutputSize = outputSize;
        Background = background;
        Sampling = sampling;
        _name = name ?? "Transform";
    }

    private readonly string _name;

    /// <summary>Maps input pixel coordinates to output coordinates. When <see cref="OutputSize"/> is null the output is translated so the transformed bounds start at the origin.</summary>
    public Matrix3x2 Matrix { get; }
    public Size? OutputSize { get; }
    public Rgba32 Background { get; }
    public WarpSampling Sampling { get; }

    public override string Name => _name;

    public override bool IsIdentity => Matrix.IsIdentity && OutputSize is null;

    private (Matrix3x2 Forward, Size Size) Resolve(Size input)
    {
        if (OutputSize is { } s) return (Matrix, s);
        var bounds = TransformBounds(Matrix, input);
        var translate = Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y);
        var w = Math.Max(1, (int)Math.Round(bounds.Width));
        var h = Math.Max(1, (int)Math.Round(bounds.Height));
        return (Matrix * translate, new Size(w, h));
    }

    internal static RectangleF TransformBounds(Matrix3x2 m, Size input)
    {
        Span<Vector2> pts = [Vector2.Transform(Vector2.Zero, m), Vector2.Transform(new Vector2(input.Width, 0), m), Vector2.Transform(new Vector2(0, input.Height), m), Vector2.Transform(new Vector2(input.Width, input.Height), m)];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        // Snap to avoid 1px overflow from floating point noise.
        minX = MathF.Round(minX, 3); minY = MathF.Round(minY, 3); maxX = MathF.Round(maxX, 3); maxY = MathF.Round(maxY, 3);
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    public override Size GetOutputSize(Size inputSize) => Resolve(inputSize).Size;

    public Matrix3x2? GetTransform(Size inputSize) => Resolve(inputSize).Forward;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var (forward, size) = Resolve(source.Size);
        Matrix3x2.Invert(forward, out var inverse);
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            Warp.RunAffine(source, output, inverse, Sampling, Background, context);
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    public override IImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var s = (float)scale;
        // Conjugate: scale down to original space, apply, scale back up.
        var m = Matrix3x2.CreateScale(1 / s) * Matrix * Matrix3x2.CreateScale(s);
        Size? os = OutputSize is { } o ? new Size(Math.Max(1, (int)Math.Round(o.Width * scale)), Math.Max(1, (int)Math.Round(o.Height * scale))) : null;
        return new AffineTransformOperation(m, os, Background, Sampling, _name);
    }
}

/// <summary>Rotates by an arbitrary angle around the image centre. Multiples of 90° are delegated to the lossless <see cref="OrientationOperation"/>.</summary>
public sealed class RotateOperation : ImageOperation, IGeometricOperation
{
    public RotateOperation(double degrees, Rgba32 background = default, bool expandCanvas = true, WarpSampling sampling = WarpSampling.Bicubic)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        Degrees = ((degrees % 360) + 360) % 360;
        Background = background;
        ExpandCanvas = expandCanvas;
        Sampling = sampling;
    }

    /// <summary>Clockwise rotation in degrees, normalised to [0,360).</summary>
    public double Degrees { get; }
    public Rgba32 Background { get; }
    /// <summary>When true the canvas grows to contain the whole rotated image; otherwise the original size is kept and corners are clipped.</summary>
    public bool ExpandCanvas { get; }
    public WarpSampling Sampling { get; }

    public override string Name => "Rotate";
    public override bool IsIdentity => Degrees == 0;

    /// <summary>
    /// A quarter turn can be done by moving pixels, but a 90° or 270° turn swaps the dimensions, so it only matches this
    /// operation when the canvas may grow or the image is square.
    /// </summary>
    private OrientationOperation? Lossless(Size input)
    {
        if (Degrees % 90 != 0) return null;
        if (!ExpandCanvas && Degrees % 180 != 0 && input.Width != input.Height) return null;
        return OrientationOperation.FromRotation((int)Degrees);
    }

    private AffineTransformOperation BuildAffine(Size input)
    {
        var rad = (float)(Degrees * Math.PI / 180);
        var center = new Vector2(input.Width / 2f, input.Height / 2f);
        var m = Matrix3x2.CreateRotation(rad, center);
        if (ExpandCanvas) return new AffineTransformOperation(m, null, Background, Sampling, Name);
        return new AffineTransformOperation(m, input, Background, Sampling, Name);
    }

    public override Size GetOutputSize(Size inputSize)
        => Lossless(inputSize) is { } o ? o.GetOutputSize(inputSize) : BuildAffine(inputSize).GetOutputSize(inputSize);

    public Matrix3x2? GetTransform(Size inputSize)
        => Lossless(inputSize) is { } o ? o.GetTransform(inputSize) : BuildAffine(inputSize).GetTransform(inputSize);

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
        => Lossless(source.Size) is { } o ? o.Apply(source, context) : BuildAffine(source.Size).Apply(source, context);

    public override string ToString() => $"Rotate({Degrees:0.##}°)";
}

/// <summary>Skews (shears) the image by the given angles in degrees along each axis.</summary>
public sealed class SkewOperation : ImageOperation, IGeometricOperation
{
    /// <exception cref="ArgumentOutOfRangeException">
    /// The angles do not describe an invertible shear. A shear is singular at 90 degrees, and at any pair of angles
    /// whose tangents multiply to 1; such a transform collapses the image to a line and cannot be sampled.
    /// </exception>
    public SkewOperation(double degreesX, double degreesY, Rgba32 background = default, WarpSampling sampling = WarpSampling.Bilinear)
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
    public Rgba32 Background { get; }
    public WarpSampling Sampling { get; }
    public override string Name => "Skew";
    public override bool IsIdentity => DegreesX == 0 && DegreesY == 0;

    private AffineTransformOperation Build()
    {
        var m = Matrix3x2.CreateSkew((float)(DegreesX * Math.PI / 180), (float)(DegreesY * Math.PI / 180));
        return new AffineTransformOperation(m, null, Background, Sampling, Name);
    }

    public override Size GetOutputSize(Size inputSize) => Build().GetOutputSize(inputSize);
    public Matrix3x2? GetTransform(Size inputSize) => Build().GetTransform(inputSize);
    public override ImageBuffer Apply(ImageBuffer source, OperationContext context) => Build().Apply(source, context);
}

/// <summary>
/// Maps the four corners of the image to four arbitrary destination points (projective / perspective transform).
/// Useful for correcting keystone distortion in photographed documents.
/// </summary>
public sealed class PerspectiveTransformOperation : ImageOperation, IGeometricOperation
{
    private readonly PointF[] _corners;

    /// <param name="destinationCorners">Destination positions of the top-left, top-right, bottom-right and bottom-left corners, in output pixel coordinates.</param>
    /// <param name="outputSize">Output canvas size. Null uses the bounding box of the destination corners.</param>
    /// <param name="background">Colour for destination pixels that map outside the source.</param>
    /// <param name="sampling">Interpolation used when sampling the source.</param>
    public PerspectiveTransformOperation(IReadOnlyList<PointF> destinationCorners, Size? outputSize = null, Rgba32 background = default, WarpSampling sampling = WarpSampling.Bilinear)
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
    public static PerspectiveTransformOperation Straighten(IReadOnlyList<PointF> sourceQuad, Size outputSize, Rgba32 background = default, WarpSampling sampling = WarpSampling.Bilinear)
    {
        ArgumentNullException.ThrowIfNull(sourceQuad);
        if (sourceQuad.Count != 4) throw new ArgumentException("Exactly four corners are required.", nameof(sourceQuad));
        return new PerspectiveTransformOperation(sourceQuad, outputSize, background, sampling) { _straighten = true };
    }

    private bool _straighten;

    public IReadOnlyList<PointF> DestinationCorners => _corners;
    public Size? OutputSize { get; }
    public Rgba32 Background { get; }
    public WarpSampling Sampling { get; }
    public override string Name => "Perspective";

    public Matrix3x2? GetTransform(Size inputSize) => null;

    public override Size GetOutputSize(Size inputSize)
    {
        if (OutputSize is { } s) return s;
        float maxX = 0, maxY = 0;
        foreach (var c in _corners) { maxX = MathF.Max(maxX, c.X); maxY = MathF.Max(maxY, c.Y); }
        return new Size(Math.Max(1, (int)MathF.Ceiling(maxX)), Math.Max(1, (int)MathF.Ceiling(maxY)));
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var size = GetOutputSize(source.Size);
        // Homography from destination quad to source rectangle (inverse mapping).
        var srcRect = new[] { new PointF(0, 0), new PointF(source.Width, 0), new PointF(source.Width, source.Height), new PointF(0, source.Height) };
        var outRect = new[] { new PointF(0, 0), new PointF(size.Width, 0), new PointF(size.Width, size.Height), new PointF(0, size.Height) };
        var h = _straighten
            ? Homography.Compute(outRect, _corners)     // output rectangle -> source quad
            : Homography.Compute(_corners, srcRect);    // destination quad -> source rectangle
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            Warp.Run(source, output, p => h.Transform(p), Sampling, Background, context);
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    public override IImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var s = (float)scale;
        var corners = _corners.Select(c => new PointF(c.X * s, c.Y * s)).ToArray();
        Size? os = OutputSize is { } o ? new Size(Math.Max(1, (int)Math.Round(o.Width * scale)), Math.Max(1, (int)Math.Round(o.Height * scale))) : null;
        return new PerspectiveTransformOperation(corners, os, Background, Sampling) { _straighten = _straighten };
    }
}

/// <summary>A 3×3 projective transform.</summary>
public readonly struct Homography
{
    private readonly float _a, _b, _c, _d, _e, _f, _g, _h;

    private Homography(float a, float b, float c, float d, float e, float f, float g, float h)
    {
        _a = a; _b = b; _c = c; _d = d; _e = e; _f = f; _g = g; _h = h;
    }

    /// <summary>Transforms a point.</summary>
    public Vector2 Transform(Vector2 p)
    {
        var w = _g * p.X + _h * p.Y + 1f;
        if (MathF.Abs(w) < 1e-9f) w = 1e-9f;
        return new Vector2((_a * p.X + _b * p.Y + _c) / w, (_d * p.X + _e * p.Y + _f) / w);
    }

    /// <summary>Computes the homography mapping four source points onto four destination points.</summary>
    public static Homography Compute(IReadOnlyList<PointF> from, IReadOnlyList<PointF> to)
    {
        // Solve the 8x8 linear system using Gaussian elimination.
        var m = new double[8, 9];
        for (var i = 0; i < 4; i++)
        {
            double x = from[i].X, y = from[i].Y, u = to[i].X, v = to[i].Y;
            m[i * 2, 0] = x; m[i * 2, 1] = y; m[i * 2, 2] = 1; m[i * 2, 6] = -u * x; m[i * 2, 7] = -u * y; m[i * 2, 8] = u;
            m[i * 2 + 1, 3] = x; m[i * 2 + 1, 4] = y; m[i * 2 + 1, 5] = 1; m[i * 2 + 1, 6] = -v * x; m[i * 2 + 1, 7] = -v * y; m[i * 2 + 1, 8] = v;
        }
        for (var col = 0; col < 8; col++)
        {
            var pivot = col;
            for (var r = col + 1; r < 8; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col])) pivot = r;
            if (Math.Abs(m[pivot, col]) < 1e-12) throw new ArgumentException("The corner points are degenerate (collinear).");
            if (pivot != col) for (var k = 0; k < 9; k++) (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
            for (var r = 0; r < 8; r++)
            {
                if (r == col) continue;
                var f = m[r, col] / m[col, col];
                if (f == 0) continue;
                for (var k = col; k < 9; k++) m[r, k] -= f * m[col, k];
            }
        }
        var s = new float[8];
        for (var i = 0; i < 8; i++) s[i] = (float)(m[i, 8] / m[i, i]);
        return new Homography(s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]);
    }
}
