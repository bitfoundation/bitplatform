using System.Drawing;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;

namespace Bit.Bmage.Tests;

/// <summary>
/// The fast paths behind the operations must produce exactly the pixels of the straightforward algorithms they replaced.
/// Each test keeps that reference algorithm and compares every pixel of noisy, translucent images against it.
/// </summary>
[TestClass]
public sealed class FastPathEquivalenceTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    public void MedianMatchesSortingEveryWindow(int radius)
    {
        foreach (var (width, height) in new[] { (37, 23), (1, 9), (9, 1), (2, 2), (12, 40) })
        {
            using var source = Noise(width, height, seed: radius * 31 + width);
            using var expected = ReferenceMedian(source, radius, region: null);
            using var actual = new MedianFilter(radius).Apply(source, OperationContext.Default);
            AssertSamePixels(expected, actual, $"radius {radius}, {width}x{height}");
        }
    }

    [TestMethod]
    public void MedianOfARegionMatchesSortingEveryWindow()
    {
        using var source = Noise(40, 30, seed: 7);
        var region = new Rectangle(5, 3, 21, 17);
        foreach (var radius in new[] { 1, 2, 4 })
        {
            using var expected = ReferenceMedian(source, radius, region);
            using var actual = new MedianFilter(radius, region).Apply(source, OperationContext.Default);
            AssertSamePixels(expected, actual, $"radius {radius}");
        }
    }

    [TestMethod]
    public void StreamingTheBoxBlurMatchesBlurringTheWholeRegionAtOnce()
    {
        foreach (var (width, height, sigma) in new[] { (64, 48, 2f), (37, 91, 3.5f), (120, 30, 8f), (9, 7, 12f), (1, 40, 5f), (40, 1, 5f), (200, 150, 25f) })
        {
            using var source = Noise(width, height, seed: width + height);
            using var expected = source.Clone();
            ReferenceBoxBlur(expected, expected.Bounds, sigma);
            using var actual = new BlurFilter(sigma).Apply(source, OperationContext.Default);
            AssertSamePixels(expected, actual, $"sigma {sigma}, {width}x{height}");
        }

        using var image = Noise(80, 60, seed: 12);
        var region = new Rectangle(7, 5, 50, 41);
        using var expectedRegion = image.Clone();
        ReferenceBoxBlur(expectedRegion, region, 4f);
        using var actualRegion = new BlurFilter(4f, region).Apply(image, OperationContext.Default);
        AssertSamePixels(expectedRegion, actualRegion, "region");
    }

    [TestMethod]
    public void TiledWarpsMatchSamplingEveryPixelDirectly()
    {
        using var source = Noise(97, 61, seed: 31);
        var background = new Rgba32(30, 60, 90, 128);
        var transforms = new[]
        {
            System.Numerics.Matrix3x2.CreateRotation(0.6458f),
            System.Numerics.Matrix3x2.CreateRotation(-2.1f) * System.Numerics.Matrix3x2.CreateScale(1.7f),
            System.Numerics.Matrix3x2.CreateSkew(0.3f, -0.1f),
            System.Numerics.Matrix3x2.CreateScale(0.04f, 0.05f),
            System.Numerics.Matrix3x2.CreateScale(5f) * System.Numerics.Matrix3x2.CreateRotation(0.2f),
        };
        foreach (var matrix in transforms)
        foreach (var sampling in new[] { Bit.Bmage.Operations.Transforms.WarpSampling.Bilinear, Bit.Bmage.Operations.Transforms.WarpSampling.Bicubic })
        {
            var operation = new Bit.Bmage.Operations.Transforms.AffineTransformOperation(matrix, background: background, sampling: sampling);
            using var actual = operation.Apply(source, OperationContext.Default);
            System.Numerics.Matrix3x2.Invert(operation.GetTransform(source.Size)!.Value, out var inverse);
            using var expected = ReferenceWarp(source, actual.Size, inverse, sampling == Bit.Bmage.Operations.Transforms.WarpSampling.Bicubic, background);
            AssertSamePixels(expected, actual, $"{matrix}, {sampling}");
        }
    }

    /// <summary>The affine warp sampling every tap straight from the source, as it did before tiles.</summary>
    private static ImageBuffer ReferenceWarp(ImageBuffer source, System.Drawing.Size size, System.Numerics.Matrix3x2 inverse, bool bicubic, Rgba32 background)
    {
        const float inv = 1f / 255f;
        System.Numerics.Vector4 Load(Rgba32 p) { var a = p.A * inv; return new System.Numerics.Vector4(p.R * inv * a, p.G * inv * a, p.B * inv * a, a); }
        var bg = new System.Numerics.Vector4(background.R * inv * background.A * inv, background.G * inv * background.A * inv, background.B * inv * background.A * inv, background.A * inv);
        int sw = source.Width, sh = source.Height;
        System.Numerics.Vector4 Fetch(int x, int y) => (uint)x < (uint)sw && (uint)y < (uint)sh ? Load(source[x, y]) : bg;
        static float Cubic(float x)
        {
            const float a = -0.5f;
            x = MathF.Abs(x);
            if (x < 1f) return ((a + 2f) * x - (a + 3f)) * x * x + 1f;
            if (x < 2f) return ((a * x - 5f * a) * x + 8f * a) * x - 4f * a;
            return 0f;
        }
        var output = ImageBuffer.Create(size.Width, size.Height);
        for (var y = 0; y < size.Height; y++)
        {
            var origin = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(0.5f, y + 0.5f), inverse);
            for (var x = 0; x < size.Width; x++)
            {
                var sx = origin.X - 0.5f + x * inverse.M11;
                var sy = origin.Y - 0.5f + x * inverse.M12;
                System.Numerics.Vector4 p;
                if (bicubic)
                {
                    if (sx <= -2f || sy <= -2f || sx >= sw + 1 || sy >= sh + 1) p = bg;
                    else
                    {
                        var x0 = (int)MathF.Floor(sx); var y0 = (int)MathF.Floor(sy);
                        var acc = System.Numerics.Vector4.Zero;
                        for (var j = 0; j < 4; j++)
                        {
                            var row = System.Numerics.Vector4.Zero;
                            for (var i = 0; i < 4; i++) row += Fetch(x0 - 1 + i, y0 - 1 + j) * Cubic(sx - x0 - (i - 1));
                            acc += row * Cubic(sy - y0 - (j - 1));
                        }
                        p = acc;
                    }
                }
                else
                {
                    if (sx <= -1f || sy <= -1f || sx >= sw || sy >= sh) p = bg;
                    else
                    {
                        var x0 = (int)MathF.Floor(sx); var y0 = (int)MathF.Floor(sy);
                        float fx = sx - x0, fy = sy - y0;
                        var top = Fetch(x0, y0) + (Fetch(x0 + 1, y0) - Fetch(x0, y0)) * fx;
                        var bottom = Fetch(x0, y0 + 1) + (Fetch(x0 + 1, y0 + 1) - Fetch(x0, y0 + 1)) * fx;
                        p = top + (bottom - top) * fy;
                    }
                }
                if (!(p.W > 1e-5f)) { output[x, y] = Rgba32.Transparent; continue; }
                p = new System.Numerics.Vector4(p.X * (1f / p.W), p.Y * (1f / p.W), p.Z * (1f / p.W), p.W);
                var v = System.Numerics.Vector4.Clamp(p, System.Numerics.Vector4.Zero, System.Numerics.Vector4.One) * 255f + new System.Numerics.Vector4(0.5f);
                output[x, y] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
            }
        }
        return output;
    }

    [TestMethod]
    public void SharpeningWhileTheBlurStreamsMatchesSharpeningAgainstABlurredCopy()
    {
        // Small images take the arithmetic path, the 600x300 one the table.
        foreach (var (width, height, amount, radius, threshold) in new[] { (40, 30, 0.8f, 1f, 0f), (33, 47, 1.5f, 3f, 0.05f), (600, 300, 0.6f, 2.5f, 0.02f) })
        {
            using var source = Noise(width, height, seed: width);
            var region = new Rectangle(3, 2, width - 7, height - 5);
            using var actual = new SharpenFilter(amount, radius, threshold, region).Apply(source, OperationContext.Default);

            using var blurred = new BlurFilter(radius).Apply(source.CopyRegion(region), OperationContext.Default);
            using var expected = source.Clone();
            var t = (int)(threshold * 255);
            for (var y = 0; y < region.Height; y++)
            for (var x = 0; x < region.Width; x++)
            {
                var p = source[region.X + x, region.Y + y];
                var b = blurred[x, y];
                expected[region.X + x, region.Y + y] = new Rgba32(Sharpen(p.R, b.R), Sharpen(p.G, b.G), Sharpen(p.B, b.B), p.A);
            }
            AssertSamePixels(expected, actual, $"{width}x{height}");

            byte Sharpen(byte v, byte blur)
            {
                var diff = v - blur;
                return Math.Abs(diff) < t ? v : Rgba32.ClampToByte(v + diff * amount);
            }
        }
    }

    [TestMethod]
    public void SmallBlursAreRealGaussians()
    {
        using var flat = ImageBuffer.Create(30, 20, new Rgba32(90, 140, 200, 255));
        foreach (var sigma in new[] { 0.3f, 0.5f, 1f, 1.9f })
        {
            using var blurred = new BlurFilter(sigma).Apply(flat, OperationContext.Default);
            for (var y = 0; y < flat.Height; y++)
            for (var x = 0; x < flat.Width; x++)
                Assert.AreEqual(flat[x, y], blurred[x, y], $"a flat colour survives sigma {sigma}");
        }

        // A single bright pixel spreads to its neighbours symmetrically, even at a radius three box passes round to nothing.
        using var dot = ImageBuffer.Create(9, 9, new Rgba32(0, 0, 0, 255));
        dot[4, 4] = new Rgba32(255, 255, 255, 255);
        using var spread = new BlurFilter(0.5f).Apply(dot, OperationContext.Default);
        Assert.IsTrue(spread[4, 4].R < 255 && spread[3, 4].R > 0, "sigma 0.5 blurs");
        Assert.AreEqual(spread[3, 4], spread[5, 4]);
        Assert.AreEqual(spread[4, 3], spread[4, 5]);
        Assert.AreEqual(spread[3, 4], spread[4, 3]);
    }

    [TestMethod]
    public void BlurStrengthFollowsTheRequestedDeviation()
    {
        // Whether the sampled gaussian or the box passes run, a blurred dot spreads by about the deviation asked for, so
        // a slider dragged across the radii where one takes over from the other never jumps in strength, as a 3x3 box
        // (deviation 0.82) taking over from a deviation 0.99 gaussian did.
        using var dot = ImageBuffer.Create(41, 41, new Rgba32(0, 0, 0, 255));
        dot[20, 20] = new Rgba32(255, 255, 255, 255);
        var report = new System.Text.StringBuilder();
        var worst = 0.0;
        for (var step = 0; step <= 38; step++)
        {
            var sigma = 0.6f + step * 0.05f;
            using var blurred = new BlurFilter(sigma).Apply(dot, OperationContext.Default);
            double mass = 0, moment = 0;
            for (var y = 0; y < 41; y++)
                for (var x = 0; x < 41; x++)
                {
                    double v = blurred[x, y].R;
                    mass += v;
                    moment += v * ((x - 20) * (x - 20) + (y - 20) * (y - 20)) / 2;
                }
            var spread = Math.Sqrt(moment / mass);
            worst = Math.Max(worst, Math.Abs(spread - sigma) / sigma);
            report.Append($"{sigma:0.00}:{spread:0.000} ");
        }
        Assert.IsTrue(worst < 0.12, report.ToString());
    }

    /// <summary>The three-pass box blur as it was: premultiplied, each pass over the whole region in turn.</summary>
    private static void ReferenceBoxBlur(ImageBuffer buffer, Rectangle region, float sigma)
    {
        int w = region.Width, h = region.Height;
        var a = new System.Numerics.Vector4[w * h];
        var b = new System.Numerics.Vector4[w * h];
        const float inv = 1f / 255f;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var p = buffer[region.X + x, region.Y + y];
            var al = p.A * inv;
            a[y * w + x] = new System.Numerics.Vector4(p.R * inv * al, p.G * inv * al, p.B * inv * al, al);
        }
        var wIdeal = Math.Sqrt(12 * sigma * sigma / 3 + 1);
        var wl = (int)Math.Floor(wIdeal);
        if (wl % 2 == 0) wl--;
        var m = (int)Math.Round((12 * sigma * sigma - 3 * wl * wl - 12 * wl - 9) / (-4.0 * wl - 4));
        for (var i = 0; i < 3; i++)
        {
            var r = ((i < m ? wl : wl + 2) - 1) / 2;
            if (r <= 0) continue;
            var iarr = 1f / (r + r + 1);
            for (var y = 0; y < h; y++)
            {
                var row = y * w;
                var fv = a[row];
                var lv = a[row + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += a[row + Math.Min(j, w - 1)];
                for (var x = 0; x < w; x++)
                {
                    var add = x + r < w ? a[row + x + r] : lv;
                    var sub = x - r - 1 >= 0 ? a[row + x - r - 1] : fv;
                    val += add - sub;
                    b[row + x] = val * iarr;
                }
            }
            var sums = new System.Numerics.Vector4[w];
            for (var x = 0; x < w; x++)
            {
                var v = (r + 1) * b[x];
                for (var j = 0; j < r; j++) v += b[Math.Min(j, h - 1) * w + x];
                sums[x] = v;
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = sums[x] + b[Math.Min(y + r, h - 1) * w + x] - b[Math.Max(y - r - 1, 0) * w + x];
                sums[x] = v;
                a[y * w + x] = v * iarr;
            }
        }
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var p = a[y * w + x];
            if (p.W > 1e-5f) p = new System.Numerics.Vector4(p.X * (1f / p.W), p.Y * (1f / p.W), p.Z * (1f / p.W), p.W);
            else p = System.Numerics.Vector4.Zero;
            var v = System.Numerics.Vector4.Clamp(p, System.Numerics.Vector4.Zero, System.Numerics.Vector4.One) * 255f + new System.Numerics.Vector4(0.5f);
            buffer[region.X + x, region.Y + y] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
        }
    }

    [TestMethod]
    public void PointOperationsMatchRunningEveryRowThroughTheirStages()
    {
        PointOperation[] separable =
        [
            new BrightnessAdjustment(0.2f), new BrightnessAdjustment(-0.35f), new ContrastAdjustment(0.4f), new ContrastAdjustment(-0.6f),
            new ExposureAdjustment(1.3f), new TemperatureAdjustment(0.7f), new TintAdjustment(-0.5f), new OpacityAdjustment(0.6f),
            new InvertFilter(0.3f), new GammaAdjustment(1.8f), new LevelsAdjustment(0.1f, 0.85f, 1.2f, 0.05f, 0.95f),
            new PosterizeFilter(5), CurveOperation.FromFunction(v => v * v, ColorChannels.All),
            new ColorMatrixOperation(ColorMatrix.Scale(1.2f, 0.8f, 1.1f, 0.9f)),
        ];
        PointOperation[] mixing = [new SaturationAdjustment(0.5f), new VibranceAdjustment(0.4f), new HueAdjustment(40), new ShadowsHighlightsAdjustment(0.3f, -0.2f), new ThresholdFilter(0.4f)];

        var operations = new List<PointOperation>(separable)
        {
            // A mixing stage closed by curves, including one that changes alpha only.
            new FusedPointOperation([new SaturationAdjustment(0.6f), new GammaAdjustment(1.4f)]),
            new FusedPointOperation([new BrightnessAdjustment(0.1f), new SaturationAdjustment(1.2f), new PosterizeFilter(4), new GammaAdjustment(0.8f)]),
            new FusedPointOperation([new VibranceAdjustment(0.5f), new CurveOperation(null, null, null, CurveOperation.BuildTable(v => 1 - v))]),
        };
        var random = new Random(11);
        for (var i = 0; i < 40; i++)
        {
            // Runs of every shape: all per-channel (one table), a per-channel prefix then mixing stages, and mixing first.
            var stages = new List<PointOperation>();
            for (var s = random.Next(2, 6); s > 0; s--)
                stages.Add(random.Next(3) == 0 ? mixing[random.Next(mixing.Length)] : separable[random.Next(separable.Length)]);
            operations.Add(new FusedPointOperation(stages));
        }

        using var source = Noise(256, 64, seed: 3);
        foreach (var operation in operations)
        {
            using var expected = ReferencePointOperation(source, operation);
            using var actual = operation.Apply(source, OperationContext.Default);
            AssertSamePixels(expected, actual, operation.Name);
        }
    }

    /// <summary>The direct method: load every row, run the operation over it and store it.</summary>
    private static ImageBuffer ReferencePointOperation(ImageBuffer source, PointOperation operation)
    {
        var output = ImageBuffer.Create(source.Width, source.Height);
        var row = new System.Numerics.Vector4[source.Width];
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var p = source[x, y];
                row[x] = new System.Numerics.Vector4(p.R, p.G, p.B, p.A) * (1f / 255f);
            }
            operation.ProcessRow(row);
            for (var x = 0; x < source.Width; x++)
            {
                var v = System.Numerics.Vector4.Clamp(row[x], System.Numerics.Vector4.Zero, System.Numerics.Vector4.One) * 255f + new System.Numerics.Vector4(0.5f);
                output[x, y] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
            }
        }
        return output;
    }

    [TestMethod]
    public void RowConvertersMatchThePerChannelExpressions()
    {
        // Every red and alpha value against every other, with the remaining channels varied, covers every input byte.
        var pixels = new Rgba32[256 * 256];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Rgba32((byte)i, (byte)(i * 7 + 3), (byte)(255 - i), (byte)(i >> 8));
        const float inv = 1f / 255f;

        var loaded = new System.Numerics.Vector4[pixels.Length];
        PixelRows.Load(pixels, loaded);
        var premultiplied = new System.Numerics.Vector4[pixels.Length];
        PixelRows.LoadPremultiplied(pixels, premultiplied);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            Assert.AreEqual(new System.Numerics.Vector4(p.R, p.G, p.B, p.A) * inv, loaded[i], $"Load {p}");
            var a = p.A * inv;
            Assert.AreEqual(new System.Numerics.Vector4(p.R * inv * a, p.G * inv * a, p.B * inv * a, a), premultiplied[i], $"LoadPremultiplied {p}");
        }

        // Values outside [0,1], the rounding boundaries and the near transparent alphas the unpremultiply treats specially.
        var random = new Random(5);
        var values = new System.Numerics.Vector4[100_000];
        float[] special = [-1f, -0f, 0f, 1e-6f, 1e-5f, 2e-5f, 0.5f / 255f, 0.5f, 254.5f / 255f, 1f, 1.0001f, 7f];
        for (var i = 0; i < values.Length; i++)
        {
            float Next() => random.Next(8) == 0 ? special[random.Next(special.Length)] : (float)(random.NextDouble() * 1.4 - 0.2);
            values[i] = new System.Numerics.Vector4(Next(), Next(), Next(), Next());
        }
        var stored = new Rgba32[values.Length];
        PixelRows.Store(values, stored);
        var unpremultiplied = new Rgba32[values.Length];
        PixelRows.StoreUnpremultiplied(values, unpremultiplied);
        for (var i = 0; i < values.Length; i++)
        {
            Assert.AreEqual(ReferenceStore(values[i]), stored[i], $"Store {values[i]}");
            var p = values[i];
            var u = p.W > 1e-5f ? new System.Numerics.Vector4(p.X * (1f / p.W), p.Y * (1f / p.W), p.Z * (1f / p.W), p.W) : System.Numerics.Vector4.Zero;
            Assert.AreEqual(ReferenceStore(u), unpremultiplied[i], $"StoreUnpremultiplied {values[i]}");
        }

        static Rgba32 ReferenceStore(System.Numerics.Vector4 p)
        {
            var v = System.Numerics.Vector4.Clamp(p, System.Numerics.Vector4.Zero, System.Numerics.Vector4.One) * 255f + new System.Numerics.Vector4(0.5f);
            return new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
        }
    }

    /// <summary>The direct method: gather the edge-repeating window and sort each channel.</summary>
    private static ImageBuffer ReferenceMedian(ImageBuffer source, int r, Rectangle? region)
    {
        var area = region ?? source.Bounds;
        var output = source.Clone();
        using var input = source.CopyRegion(area);
        int w = input.Width, h = input.Height, window = (2 * r + 1) * (2 * r + 1);
        var rs = new byte[window]; var gs = new byte[window]; var bs = new byte[window];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var n = 0;
            for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                var p = input[Math.Clamp(x + dx, 0, w - 1), Math.Clamp(y + dy, 0, h - 1)];
                rs[n] = p.R; gs[n] = p.G; bs[n] = p.B; n++;
            }
            Array.Sort(rs); Array.Sort(gs); Array.Sort(bs);
            output[area.X + x, area.Y + y] = new Rgba32(rs[window / 2], gs[window / 2], bs[window / 2], input[x, y].A);
        }
        return output;
    }

    internal static ImageBuffer Noise(int width, int height, int seed)
    {
        var random = new Random(seed);
        var image = ImageBuffer.Create(width, height, clear: false);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            // Mostly full range noise, with runs of repeated values so the histograms see ties.
            var v = random.Next(4) == 0 ? (byte)(x * 7) : (byte)random.Next(256);
            image[x, y] = new Rgba32(v, (byte)random.Next(256), (byte)(255 - v), (byte)random.Next(256));
        }
        return image;
    }

    internal static void AssertSamePixels(ImageBuffer expected, ImageBuffer actual, string because)
    {
        Assert.AreEqual(expected.Size, actual.Size, because);
        for (var y = 0; y < expected.Height; y++)
        for (var x = 0; x < expected.Width; x++)
            if (expected[x, y] != actual[x, y])
                Assert.Fail($"{because}: pixel ({x},{y}) is {actual[x, y]}, expected {expected[x, y]}.");
    }
}
