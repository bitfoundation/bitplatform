using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

/// <summary>
/// Differential and edge-case tests for geometry, orientation and resampling. Each test states an invariant that must
/// hold in a correct implementation; a failure is a defect rather than a tightened assertion.
/// </summary>
[TestClass]
public sealed class AdversarialGeometryTests
{
    internal static BmageImageBuffer Noise(int w, int h, int seed = 1)
    {
        var rnd = new Random(seed);
        var img = BmageImageBuffer.Create(w, h);
        var px = img.Pixels;
        for (var i = 0; i < px.Length; i++)
            px[i] = new BmageRgba32((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256), 255);
        return img;
    }

    /// <summary>The affine matrix an orientation reports must agree with the pixels it actually produces.</summary>
    [TestMethod]
    [DataRow(BmageOrientation.Normal)]
    [DataRow(BmageOrientation.FlipHorizontal)]
    [DataRow(BmageOrientation.Rotate180)]
    [DataRow(BmageOrientation.FlipVertical)]
    [DataRow(BmageOrientation.Transpose)]
    [DataRow(BmageOrientation.Rotate90)]
    [DataRow(BmageOrientation.Transverse)]
    [DataRow(BmageOrientation.Rotate270)]
    public void OrientationMatrixAgreesWithPixels(BmageOrientation o)
    {
        using var src = Noise(7, 5, 3);
        using var dst = new BmageOrientationOperation(o).Apply(src, BmageOperationContext.Default);
        var m = o.ToMatrix(src.Size);
        for (var y = 0; y < src.Height; y++)
        {
            for (var x = 0; x < src.Width; x++)
            {
                var p = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), m);
                var dx = (int)MathF.Floor(p.X);
                var dy = (int)MathF.Floor(p.Y);
                Assert.IsInRange(0, dst.Width - 1, dx);
                Assert.IsInRange(0, dst.Height - 1, dy);
                Assert.AreEqual(src[x, y], dst[dx, dy]);
            }
        }
    }

    /// <summary>Composing two orientations must equal applying them one after the other.</summary>
    [TestMethod]
    public void OrientationCompositionMatchesSequentialApplication()
    {
        var all = Enum.GetValues<BmageOrientation>().Where(o => o != BmageOrientation.Unspecified).ToArray();
        foreach (var a in all)
        {
            foreach (var b in all)
            {
                using var src = Noise(6, 4, 7);
                using var step1 = new BmageOrientationOperation(a).Apply(src, BmageOperationContext.Default);
                using var step2 = new BmageOrientationOperation(b).Apply(step1, BmageOperationContext.Default);
                using var combined = new BmageOrientationOperation(a.Then(b)).Apply(src, BmageOperationContext.Default);
                Assert.AreEqual(step2.Size, combined.Size);
                Assert.IsTrue(step2.Bytes.SequenceEqual(combined.Bytes), $"{a} then {b} != {a.Then(b)}");
            }
        }
    }

    [TestMethod]
    public void OrientationInverseUndoesOrientation()
    {
        foreach (var o in Enum.GetValues<BmageOrientation>().Where(o => o != BmageOrientation.Unspecified))
        {
            using var src = Noise(6, 4, 11);
            using var fwd = new BmageOrientationOperation(o).Apply(src, BmageOperationContext.Default);
            using var back = new BmageOrientationOperation(o.Inverse()).Apply(fwd, BmageOperationContext.Default);
            Assert.AreEqual(src.Size, back.Size);
            Assert.IsTrue(src.Bytes.SequenceEqual(back.Bytes), $"{o} then inverse is not the identity");
        }
    }

    /// <summary>The in-place mirror path must produce the same pixels as the allocating path.</summary>
    [TestMethod]
    [DataRow(BmageOrientation.FlipHorizontal)]
    [DataRow(BmageOrientation.FlipVertical)]
    [DataRow(BmageOrientation.Rotate180)]
    public void InPlaceOrientationMatchesAllocatingPath(BmageOrientation o)
    {
        foreach (var (w, h) in new[] { (7, 5), (8, 8), (1, 9), (9, 1), (2, 3), (5000, 2) })
        {
            using var a = Noise(w, h, 13);
            using var b = a.Clone();
            using var expected = new BmageOrientationOperation(o).Apply(a, BmageOperationContext.Default);
            var mutated = new BmageOrientationOperation(o).Apply(b, BmageOperationContext.Default.WithMutation(true));
            Assert.IsTrue(expected.Bytes.SequenceEqual(mutated.Bytes), $"{o} in-place differs from allocating at {w}x{h}");
        }
    }

    /// <summary>The box-reduce fast path must not shift or stretch the image relative to the direct path.</summary>
    [TestMethod]
    [DataRow(1001, 1001, 5, 5)]
    [DataRow(4032, 101, 400, 101)]
    [DataRow(999, 37, 60, 37)]
    [DataRow(1000, 1000, 7, 7)]
    [DataRow(1023, 1023, 8, 8)]
    public void PrescalePathAgreesWithDirectPath(int sw, int sh, int dw, int dh)
    {
        // A hard vertical edge exactly in the middle: any geometric shift or stretch moves the edge.
        using var src = BmageImageBuffer.Create(sw, sh, BmageRgba32.Black);
        for (var y = 0; y < sh; y++)
        {
            var row = src.GetRow(y);
            for (var x = sw / 2; x < sw; x++) row[x] = BmageRgba32.White;
        }

        using var viaPrescale = new BmageResizeOperation(dw, dh, BmageResizeMode.Stretch, BmageResamplingFilter.Lanczos3)
            .Apply(src, BmageOperationContext.Default);
        using var direct = ResampleWithoutPrescale(src, dw, dh);

        var mean = TestImages.MeanChannelDifference(viaPrescale, direct);
        Assert.IsTrue(mean < 10, $"prescale vs direct mean difference {mean:0.0} for {sw}x{sh} -> {dw}x{dh}");
    }

    /// <summary>Repeated halving never reaches the 4x ratio that triggers the box-reduce path.</summary>
    private static BmageImageBuffer ResampleWithoutPrescale(BmageImageBuffer src, int dw, int dh)
    {
        var cur = src.Clone();
        while (cur.Width > dw * 2 || cur.Height > dh * 2)
        {
            var nw = Math.Max(dw, cur.Width / 2);
            var nh = Math.Max(dh, cur.Height / 2);
            var next = new BmageResizeOperation(nw, nh, BmageResizeMode.Stretch, BmageResamplingFilter.Lanczos3).Apply(cur, BmageOperationContext.Default);
            cur.Dispose();
            cur = next;
        }
        var final = new BmageResizeOperation(dw, dh, BmageResizeMode.Stretch, BmageResamplingFilter.Lanczos3).Apply(cur, BmageOperationContext.Default);
        cur.Dispose();
        return final;
    }

    /// <summary>Downscaling a solid colour must preserve that colour whichever resampling path is taken.</summary>
    [TestMethod]
    [DataRow(1001, 1001, 5, 5)]
    [DataRow(4000, 3000, 40, 30)]
    [DataRow(4001, 3001, 41, 31)]
    [DataRow(100, 100, 33, 33)]
    public void DownscalingSolidColourIsExact(int sw, int sh, int dw, int dh)
    {
        var colour = new BmageRgba32(37, 129, 211);
        using var src = BmageImageBuffer.Create(sw, sh, colour);
        using var dst = new BmageResizeOperation(dw, dh, BmageResizeMode.Stretch, BmageResamplingFilter.Auto).Apply(src, BmageOperationContext.Default);
        foreach (var p in dst.Pixels.ToArray())
        {
            Assert.IsTrue(Math.Abs(p.R - colour.R) <= 1 && Math.Abs(p.G - colour.G) <= 1 && Math.Abs(p.B - colour.B) <= 1 && p.A == 255,
                $"solid colour not preserved: got {p}, expected {colour}, for {sw}x{sh} -> {dw}x{dh}");
        }
    }

    /// <summary>Resizing to the same size must not shift or blur the image.</summary>
    [TestMethod]
    [DataRow(BmageResamplingFilter.Bilinear)]
    [DataRow(BmageResamplingFilter.Bicubic)]
    [DataRow(BmageResamplingFilter.Lanczos3)]
    [DataRow(BmageResamplingFilter.Box)]
    public void IdentityResizeDoesNotShift(BmageResamplingFilter filter)
    {
        using var src = Noise(33, 21, 5);
        using var dst = new BmageResizeOperation(33, 21, BmageResizeMode.Stretch, filter).Apply(src, BmageOperationContext.Default);
        var max = TestImages.MaxChannelDifference(src, dst);
        Assert.IsTrue(max <= 1, $"{filter} identity resize changed pixels by {max}");
    }

    [TestMethod]
    public void CropEntirelyOutsideImageThrowsClearly()
    {
        using var src = Noise(10, 10);
        Assert.ThrowsExactly<ArgumentException>(() => new BmageCropOperation(50, 50, 10, 10).Apply(src, BmageOperationContext.Default));
    }

    [TestMethod]
    public void CropLargerThanImageIsClippedNotPadded()
    {
        using var src = Noise(10, 10);
        using var dst = new BmageCropOperation(-5, -5, 100, 100).Apply(src, BmageOperationContext.Default);
        Assert.AreEqual(new Size(10, 10), dst.Size);
        Assert.IsTrue(src.Bytes.SequenceEqual(dst.Bytes));
    }

    [TestMethod]
    [DataRow(0, 0, 0, 1)]
    [DataRow(0, 0, 1, 0)]
    [DataRow(0, 0, -1, 5)]
    public void CropWithNonPositiveSizeThrows(int x, int y, int w, int h)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageCropOperation(x, y, w, h));

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(1, 500)]
    [DataRow(500, 1)]
    [DataRow(2, 2)]
    public void ExtremeAspectResizeWorks(int w, int h)
    {
        using var src = Noise(64, 48, 29);
        using var dst = new BmageResizeOperation(w, h, BmageResizeMode.Stretch, BmageResamplingFilter.Auto).Apply(src, BmageOperationContext.Default);
        Assert.AreEqual(new Size(w, h), dst.Size);
    }

    /// <summary>Every resize mode must honour its documented contract for every source/target combination.</summary>
    [TestMethod]
    [DataRow(BmageResizeMode.Fit)]
    [DataRow(BmageResizeMode.Contain)]
    [DataRow(BmageResizeMode.Cover)]
    [DataRow(BmageResizeMode.Max)]
    [DataRow(BmageResizeMode.Min)]
    [DataRow(BmageResizeMode.Stretch)]
    public void ResizeModesHonourTheirContract(BmageResizeMode mode)
    {
        foreach (var (sw, sh) in new[] { (1, 1), (1, 1000), (1000, 1), (37, 91), (640, 480) })
        {
            foreach (var (tw, th) in new[] { (1, 1), (16, 16), (2000, 3), (300, 300) })
            {
                using var src = BmageImageBuffer.Create(sw, sh, new BmageRgba32(10, 20, 30));
                var op = new BmageResizeOperation(tw, th, mode);
                var predicted = op.GetOutputSize(src.Size);
                // Min enlarges until both dimensions reach the target, which for an extreme aspect ratio can demand
                // an enormous canvas. GetOutputSize must say so up front so a caller can check before allocating.
                if (!BmageImageLimits.Default.Allows(predicted.Width, predicted.Height))
                {
                    Assert.ThrowsExactly<BmageImageLimitExceededException>(() => op.Apply(src, BmageOperationContext.Default).Dispose());
                    continue;
                }
                using var dst = op.Apply(src, BmageOperationContext.Default);
                Assert.IsTrue(dst.Width > 0 && dst.Height > 0);
                Assert.AreEqual(predicted, dst.Size);
                if (mode is BmageResizeMode.Fit or BmageResizeMode.Max)
                    Assert.IsTrue(dst.Width <= tw && dst.Height <= th, $"{mode} {sw}x{sh} -> {tw}x{th} produced {dst.Size}");
                if (mode is BmageResizeMode.Contain or BmageResizeMode.Cover or BmageResizeMode.Stretch)
                    Assert.AreEqual(new Size(tw, th), dst.Size);
                if (mode is BmageResizeMode.Max)
                    Assert.IsTrue(dst.Width <= sw && dst.Height <= sh, $"Max enlarged {sw}x{sh} to {dst.Size}");
            }
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(90)]
    [DataRow(180)]
    [DataRow(270)]
    [DataRow(360)]
    [DataRow(-90)]
    [DataRow(45)]
    [DataRow(13.37)]
    [DataRow(359.99)]
    public void RotateProducesPredictedSize(double degrees)
    {
        using var src = Noise(40, 24, 31);
        var op = new BmageRotateOperation(degrees);
        var predicted = op.GetOutputSize(src.Size);
        using var dst = op.Apply(src, BmageOperationContext.Default);
        Assert.AreEqual(predicted, dst.Size);
    }

    [TestMethod]
    public void FourNinetyDegreeRotationsRestoreTheOriginal()
    {
        using var src = Noise(17, 11, 37);
        var cur = src.Clone();
        for (var i = 0; i < 4; i++)
        {
            var next = new BmageRotateOperation(90).Apply(cur, BmageOperationContext.Default);
            cur.Dispose();
            cur = next;
        }
        Assert.AreEqual(src.Size, cur.Size);
        Assert.IsTrue(src.Bytes.SequenceEqual(cur.Bytes));
        cur.Dispose();
    }

    /// <summary>An arbitrary rotation must not lose the image: rotating a filled frame keeps its centre opaque.</summary>
    [TestMethod]
    [DataRow(45)]
    [DataRow(13.37)]
    [DataRow(200.5)]
    [DataRow(359.99)]
    public void ArbitraryRotationKeepsContent(double degrees)
    {
        using var src = BmageImageBuffer.Create(64, 48, new BmageRgba32(200, 100, 50));
        using var dst = new BmageRotateOperation(degrees).Apply(src, BmageOperationContext.Default);
        var centre = dst[dst.Width / 2, dst.Height / 2];
        Assert.AreEqual(255, centre.A);
        Assert.IsInRange(190, 210, centre.R);
    }

    /// <summary>
    /// The affine fast path exists only for speed, so it must agree with the general delegate path. The two compute
    /// the same source position with a different order of floating point operations, so a pixel that lands exactly on
    /// a rounding boundary can differ by one unit; anything beyond that, or more than a handful of such pixels, would
    /// mean the fast path is sampling somewhere else.
    /// </summary>
    [TestMethod]
    [DataRow(BmageWarpSampling.NearestNeighbor)]
    [DataRow(BmageWarpSampling.Bilinear)]
    [DataRow(BmageWarpSampling.Bicubic)]
    public void AffineWarpFastPathMatchesTheGeneralPath(BmageWarpSampling sampling)
    {
        foreach (var degrees in new[] { 13.37, 37, 45, 200.5, 359.99 })
        {
            using var src = Noise(61, 43, 97);
            var radians = (float)(degrees * Math.PI / 180);
            var forward = Matrix3x2.CreateRotation(radians, new Vector2(src.Width / 2f, src.Height / 2f));
            Assert.IsTrue(Matrix3x2.Invert(forward, out var inverse));

            using var viaFastPath = BmageImageBuffer.Create(src.Width, src.Height, clear: false);
            using var viaGeneralPath = BmageImageBuffer.Create(src.Width, src.Height, clear: false);
            var background = new BmageRgba32(10, 20, 30, 200);

            BmageWarp.RunAffine(src, viaFastPath, inverse, sampling, background, BmageOperationContext.Default);
            BmageWarp.Run(src, viaGeneralPath, p => Vector2.Transform(p, inverse), sampling, background, BmageOperationContext.Default);

            var maxDifference = TestImages.MaxChannelDifference(viaFastPath, viaGeneralPath);
            Assert.IsTrue(maxDifference <= 1,
                $"{sampling} at {degrees} degrees differs by {maxDifference} between the affine and general warp paths");

            // A one-unit tolerance must not be able to hide a shifted image, so also require that almost every pixel
            // is bit-identical: a genuine geometric error would move a large fraction of them.
            var identical = 0;
            var a = viaFastPath.Pixels;
            var b = viaGeneralPath.Pixels;
            for (var i = 0; i < a.Length; i++) if (a[i] == b[i]) identical++;
            var fraction = identical / (double)a.Length;
            Assert.IsTrue(fraction > 0.97,
                $"{sampling} at {degrees} degrees: only {fraction:P1} of pixels matched exactly between the warp paths");
        }
    }

    [TestMethod]
    public void SkewWithDegenerateAnglesFailsAtConstructionNotUse()
    {
        // A 90 degree skew is not an invertible transform; it must be rejected up front rather than
        // throwing from GetOutputSize or Apply later on.
        Assert.Throws<ArgumentException>(() => new BmageSkewOperation(90, 90));
    }

    [TestMethod]
    public void AllocationBeyondLimitsThrowsBeforeAllocating()
        => Assert.ThrowsExactly<BmageImageLimitExceededException>(() => BmageImageBuffer.Create(100000, 100000));

    [TestMethod]
    public void ResizeBeyondLimitsThrowsInsteadOfExhaustingMemory()
    {
        using var src = Noise(16, 16);
        Assert.ThrowsExactly<BmageImageLimitExceededException>(
            () => new BmageResizeOperation(30000, 30000, BmageResizeMode.Stretch).Apply(src, BmageOperationContext.Default));
    }

    [TestMethod]
    public void RotationBeyondLimitsThrowsInsteadOfExhaustingMemory()
    {
        using var src = BmageImageBuffer.Create(16000, 3000, BmageRgba32.White);
        // The expanded canvas of a 45 degree rotation exceeds the default 16384px limit.
        Assert.ThrowsExactly<BmageImageLimitExceededException>(() => new BmageRotateOperation(45).Apply(src, BmageOperationContext.Default));
    }
}
