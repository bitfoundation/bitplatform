
namespace Bit.BbScanner.TestKit;

/// <summary>
/// Geometric and photometric transforms that turn a clean synthetic render into something that
/// looks like a phone camera frame: rotated, blurred, unevenly lit, seen at an angle, small in a
/// large frame. Every transform returns a new buffer and leaves the input untouched.
/// </summary>
public static class ImageTransforms
{
    /// <summary>Places the image in the middle of a larger frame filled with <paramref name="background"/>.</summary>
    public static BbLuminanceBuffer Frame(BbLuminanceBuffer source, int width, int height, byte background = 255)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = BbLuminanceBuffer.Rent(width, height);
        result.Pixels.Fill(background);
        Paste(source, result, (width - source.Width) / 2, (height - source.Height) / 2);
        return result;
    }

    /// <summary>Copies <paramref name="source"/> into <paramref name="target"/> at the given position, clipping at the edges.</summary>
    public static void Paste(BbLuminanceBuffer source, BbLuminanceBuffer target, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        for (var sy = 0; sy < source.Height; sy++)
        {
            var ty = y + sy;
            if ((uint)ty >= (uint)target.Height)
            {
                continue;
            }

            for (var sx = 0; sx < source.Width; sx++)
            {
                var tx = x + sx;
                if ((uint)tx >= (uint)target.Width)
                {
                    continue;
                }

                target.Pixels[(ty * target.Width) + tx] = source.Pixels[(sy * source.Width) + sx];
            }
        }
    }

    /// <summary>Rotates the image about its centre by <paramref name="degrees"/> using bilinear sampling. The output is large enough to hold the whole rotated image.</summary>
    public static BbLuminanceBuffer Rotate(BbLuminanceBuffer source, double degrees, byte background = 255)
    {
        ArgumentNullException.ThrowIfNull(source);
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        var w = source.Width;
        var h = source.Height;
        var outW = (int)Math.Ceiling((Math.Abs(w * cos) + Math.Abs(h * sin)));
        var outH = (int)Math.Ceiling((Math.Abs(w * sin) + Math.Abs(h * cos)));

        var result = BbLuminanceBuffer.Rent(outW, outH);
        var cx = (w - 1) / 2.0;
        var cy = (h - 1) / 2.0;
        var ocx = (outW - 1) / 2.0;
        var ocy = (outH - 1) / 2.0;

        for (var y = 0; y < outH; y++)
        {
            for (var x = 0; x < outW; x++)
            {
                var dx = x - ocx;
                var dy = y - ocy;
                var sx = (cos * dx) + (sin * dy) + cx;
                var sy = (-sin * dx) + (cos * dy) + cy;
                result.Pixels[(y * outW) + x] = Sample(source, sx, sy, background);
            }
        }

        return result;
    }

    /// <summary>Resizes the image by <paramref name="factor"/> using box filtering when shrinking and bilinear sampling when enlarging.</summary>
    public static BbLuminanceBuffer Scale(BbLuminanceBuffer source, double factor)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(factor, 0);

        var outW = Math.Max(1, (int)Math.Round(source.Width * factor));
        var outH = Math.Max(1, (int)Math.Round(source.Height * factor));
        var result = BbLuminanceBuffer.Rent(outW, outH);

        if (factor >= 1)
        {
            for (var y = 0; y < outH; y++)
            {
                for (var x = 0; x < outW; x++)
                {
                    result.Pixels[(y * outW) + x] = Sample(source, x / factor, y / factor, 255);
                }
            }

            return result;
        }

        // Box filter: average every source pixel that maps into the destination pixel.
        for (var y = 0; y < outH; y++)
        {
            var sy0 = (int)(y / factor);
            var sy1 = Math.Min(source.Height, (int)Math.Ceiling((y + 1) / factor));
            for (var x = 0; x < outW; x++)
            {
                var sx0 = (int)(x / factor);
                var sx1 = Math.Min(source.Width, (int)Math.Ceiling((x + 1) / factor));
                var sum = 0;
                var count = 0;
                for (var sy = sy0; sy < sy1; sy++)
                {
                    for (var sx = sx0; sx < sx1; sx++)
                    {
                        sum += source.Pixels[(sy * source.Width) + sx];
                        count++;
                    }
                }

                result.Pixels[(y * outW) + x] = (byte)(count == 0 ? 255 : sum / count);
            }
        }

        return result;
    }

    /// <summary>Applies a separable Gaussian blur with the given standard deviation in pixels.</summary>
    public static BbLuminanceBuffer Blur(BbLuminanceBuffer source, double sigma)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sigma <= 0)
        {
            var copy = BbLuminanceBuffer.Rent(source.Width, source.Height);
            source.Pixels.CopyTo(copy.Pixels);
            return copy;
        }

        var radius = Math.Max(1, (int)Math.Ceiling(sigma * 3));
        var kernel = new double[(2 * radius) + 1];
        var total = 0.0;
        for (var i = -radius; i <= radius; i++)
        {
            kernel[i + radius] = Math.Exp(-(i * i) / (2 * sigma * sigma));
            total += kernel[i + radius];
        }

        for (var i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= total;
        }

        var w = source.Width;
        var h = source.Height;
        var temp = new double[w * h];
        var src = source.Pixels;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var acc = 0.0;
                for (var k = -radius; k <= radius; k++)
                {
                    var sx = Math.Clamp(x + k, 0, w - 1);
                    acc += src[(y * w) + sx] * kernel[k + radius];
                }

                temp[(y * w) + x] = acc;
            }
        }

        var result = BbLuminanceBuffer.Rent(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var acc = 0.0;
                for (var k = -radius; k <= radius; k++)
                {
                    var sy = Math.Clamp(y + k, 0, h - 1);
                    acc += temp[(sy * w) + x] * kernel[k + radius];
                }

                result.Pixels[(y * w) + x] = (byte)Math.Clamp(Math.Round(acc), 0, 255);
            }
        }

        return result;
    }

    /// <summary>
    /// Warps the image so that its four corners land on the given destination points, simulating a
    /// symbol photographed at an angle. Points are in output pixel coordinates in the order
    /// top-left, top-right, bottom-right, bottom-left. The output size is the bounding box of the points.
    /// </summary>
    public static BbLuminanceBuffer Perspective(
        BbLuminanceBuffer source,
        (double X, double Y) topLeft,
        (double X, double Y) topRight,
        (double X, double Y) bottomRight,
        (double X, double Y) bottomLeft,
        byte background = 255)
    {
        ArgumentNullException.ThrowIfNull(source);
        var outW = (int)Math.Ceiling(Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomRight.X, bottomLeft.X))) + 1;
        var outH = (int)Math.Ceiling(Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomRight.Y, bottomLeft.Y))) + 1;

        // Map output quadrilateral -> unit square -> source rectangle.
        var toUnit = SquareToQuadrilateral(topLeft, topRight, bottomRight, bottomLeft).Inverse();

        var result = BbLuminanceBuffer.Rent(outW, outH);
        for (var y = 0; y < outH; y++)
        {
            for (var x = 0; x < outW; x++)
            {
                var (u, v) = toUnit.Transform(x, y);
                if (u < 0 || v < 0 || u > 1 || v > 1)
                {
                    result.Pixels[(y * outW) + x] = background;
                    continue;
                }

                result.Pixels[(y * outW) + x] = Sample(source, u * (source.Width - 1), v * (source.Height - 1), background);
            }
        }

        return result;
    }

    /// <summary>Tilts the image by <paramref name="degrees"/> around its vertical axis, a typical hand-held camera pose.</summary>
    public static BbLuminanceBuffer Tilt(BbLuminanceBuffer source, double degrees)
    {
        ArgumentNullException.ThrowIfNull(source);
        var w = source.Width;
        var h = source.Height;
        var shrink = Math.Cos(degrees * Math.PI / 180.0);
        var skew = Math.Sin(Math.Abs(degrees) * Math.PI / 180.0) * 0.25;
        var rightHeight = h * (1 - skew);
        var top = (h - rightHeight) / 2;
        var newW = w * shrink;
        return Perspective(
            source,
            (0, 0),
            (newW, top),
            (newW, top + rightHeight),
            (0, h - 1));
    }

    /// <summary>Multiplies luminance by a horizontal gradient from <paramref name="leftFactor"/> to <paramref name="rightFactor"/>, simulating uneven lighting.</summary>
    public static BbLuminanceBuffer LightingGradient(BbLuminanceBuffer source, double leftFactor, double rightFactor)
    {
        ArgumentNullException.ThrowIfNull(source);
        var w = source.Width;
        var h = source.Height;
        var result = BbLuminanceBuffer.Rent(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var t = w <= 1 ? 0 : (double)x / (w - 1);
                var factor = leftFactor + ((rightFactor - leftFactor) * t);
                result.Pixels[(y * w) + x] = (byte)Math.Clamp(Math.Round(source.Pixels[(y * w) + x] * factor), 0, 255);
            }
        }

        return result;
    }

    /// <summary>Compresses the luminance range to <c>[dark, light]</c>, simulating a washed out or underexposed capture.</summary>
    public static BbLuminanceBuffer Contrast(BbLuminanceBuffer source, byte dark, byte light)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = BbLuminanceBuffer.Rent(source.Width, source.Height);
        var range = light - dark;
        for (var i = 0; i < result.Pixels.Length; i++)
        {
            result.Pixels[i] = (byte)(dark + ((source.Pixels[i] * range) / 255));
        }

        return result;
    }

    /// <summary>Inverts luminance, producing a light-on-dark symbol.</summary>
    public static BbLuminanceBuffer Invert(BbLuminanceBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = BbLuminanceBuffer.Rent(source.Width, source.Height);
        for (var i = 0; i < result.Pixels.Length; i++)
        {
            result.Pixels[i] = (byte)(255 - source.Pixels[i]);
        }

        return result;
    }

    /// <summary>Fills the background with a busy texture (text-like stripes and blobs) so the symbol is not the only structure in frame.</summary>
    public static void Clutter(BbLuminanceBuffer target, int seed, int keepOutLeft, int keepOutTop, int keepOutWidth, int keepOutHeight)
    {
        ArgumentNullException.ThrowIfNull(target);
        var random = new Random(seed);
        var w = target.Width;
        var h = target.Height;
        var pixels = target.Pixels;

        for (var n = 0; n < 40; n++)
        {
            var rw = random.Next(6, 60);
            var rh = random.Next(2, 12);
            var rx = random.Next(0, Math.Max(1, w - rw));
            var ry = random.Next(0, Math.Max(1, h - rh));
            var shade = (byte)random.Next(0, 120);

            for (var y = ry; y < ry + rh; y++)
            {
                for (var x = rx; x < rx + rw; x++)
                {
                    if (x >= keepOutLeft && x < keepOutLeft + keepOutWidth && y >= keepOutTop && y < keepOutTop + keepOutHeight)
                    {
                        continue;
                    }

                    pixels[(y * w) + x] = shade;
                }
            }
        }
    }

    /// <summary>Expands the image to packed RGBA, tinting slightly so the colour path is exercised.</summary>
    public static byte[] ToRgba(BbLuminanceBuffer source, bool tint = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        var pixels = source.Pixels;
        var rgba = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var v = pixels[i];
            rgba[(i * 4) + 0] = tint ? (byte)Math.Min(255, v + 10) : v;
            rgba[(i * 4) + 1] = v;
            rgba[(i * 4) + 2] = tint ? (byte)Math.Max(0, v - 10) : v;
            rgba[(i * 4) + 3] = 255;
        }

        return rgba;
    }

    private static byte Sample(BbLuminanceBuffer source, double x, double y, byte background)
    {
        var w = source.Width;
        var h = source.Height;
        if (x < -0.5 || y < -0.5 || x > w - 0.5 || y > h - 0.5)
        {
            return background;
        }

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var fx = x - x0;
        var fy = y - y0;

        var p00 = Pixel(source, x0, y0, background);
        var p10 = Pixel(source, x0 + 1, y0, background);
        var p01 = Pixel(source, x0, y0 + 1, background);
        var p11 = Pixel(source, x0 + 1, y0 + 1, background);

        var top = (p00 * (1 - fx)) + (p10 * fx);
        var bottom = (p01 * (1 - fx)) + (p11 * fx);
        return (byte)Math.Clamp(Math.Round((top * (1 - fy)) + (bottom * fy)), 0, 255);
    }

    private static int Pixel(BbLuminanceBuffer source, int x, int y, byte background) =>
        (uint)x < (uint)source.Width && (uint)y < (uint)source.Height
            ? source.Pixels[(y * source.Width) + x]
            : background;

    private static Homography SquareToQuadrilateral(
        (double X, double Y) p0, (double X, double Y) p1, (double X, double Y) p2, (double X, double Y) p3)
    {
        var dx3 = p0.X - p1.X + p2.X - p3.X;
        var dy3 = p0.Y - p1.Y + p2.Y - p3.Y;
        if (Math.Abs(dx3) < 1e-9 && Math.Abs(dy3) < 1e-9)
        {
            return new Homography(p1.X - p0.X, p2.X - p1.X, p0.X, p1.Y - p0.Y, p2.Y - p1.Y, p0.Y, 0, 0, 1);
        }

        var dx1 = p1.X - p2.X;
        var dx2 = p3.X - p2.X;
        var dy1 = p1.Y - p2.Y;
        var dy2 = p3.Y - p2.Y;
        var denominator = (dx1 * dy2) - (dx2 * dy1);
        var a13 = ((dx3 * dy2) - (dx2 * dy3)) / denominator;
        var a23 = ((dx1 * dy3) - (dx3 * dy1)) / denominator;
        return new Homography(
            p1.X - p0.X + (a13 * p1.X), p3.X - p0.X + (a23 * p3.X), p0.X,
            p1.Y - p0.Y + (a13 * p1.Y), p3.Y - p0.Y + (a23 * p3.Y), p0.Y,
            a13, a23, 1);
    }

    private readonly record struct Homography(
        double A11, double A21, double A31,
        double A12, double A22, double A32,
        double A13, double A23, double A33)
    {
        public Homography Inverse() => new(
            (A22 * A33) - (A23 * A32), (A23 * A31) - (A21 * A33), (A21 * A32) - (A22 * A31),
            (A13 * A32) - (A12 * A33), (A11 * A33) - (A13 * A31), (A12 * A31) - (A11 * A32),
            (A12 * A23) - (A13 * A22), (A13 * A21) - (A11 * A23), (A11 * A22) - (A12 * A21));

        public (double X, double Y) Transform(double x, double y)
        {
            var denominator = (A13 * x) + (A23 * y) + A33;
            return (((A11 * x) + (A21 * y) + A31) / denominator, ((A12 * x) + (A22 * y) + A32) / denominator);
        }
    }
}
