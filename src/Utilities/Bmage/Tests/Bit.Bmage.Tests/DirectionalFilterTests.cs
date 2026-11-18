using System.Drawing;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class MotionBlurTests
{
    /// <summary>A black image with one white pixel: the blur spreads it into the line it averages over.</summary>
    private static BmageImageBuffer Dot(int size, Point at)
    {
        var image = BmageImageBuffer.Create(size, size, BmageRgba32.Black);
        image[at.X, at.Y] = BmageRgba32.White;
        return image;
    }

    [TestMethod]
    public void AHorizontalBlurSpreadsADotAlongTheRow()
    {
        using var dot = Dot(41, new Point(20, 20));
        using var blurred = new BmageMotionBlurFilter(11, 0).Apply(dot, BmageOperationContext.Default);
        // Eleven pixels of the row share the dot's light equally; the rows above and below get none.
        for (var x = 0; x < 41; x++)
        {
            var expected = Math.Abs(x - 20) <= 5 ? 255 / 11.0 : 0;
            Assert.AreEqual(expected, blurred[x, 20].R, 1.0, $"column {x}");
            Assert.AreEqual(0, blurred[x, 19].R);
            Assert.AreEqual(0, blurred[x, 21].R);
        }
    }

    [TestMethod]
    public void AVerticalBlurSpreadsADotAlongTheColumn()
    {
        using var dot = Dot(41, new Point(20, 20));
        using var blurred = new BmageMotionBlurFilter(9, 90).Apply(dot, BmageOperationContext.Default);
        for (var y = 0; y < 41; y++)
        {
            Assert.AreEqual(Math.Abs(y - 20) <= 4 ? 255 / 9.0 : 0, blurred[20, y].R, 1.0, $"row {y}");
            Assert.AreEqual(0, blurred[19, y].R);
        }
    }

    [TestMethod]
    public void ADiagonalBlurFollowsItsAngle()
    {
        using var dot = Dot(41, new Point(20, 20));
        // 45 degrees rises to the right: the light lands up-right and down-left of the dot, never across.
        using var blurred = new BmageMotionBlurFilter(14, 45).Apply(dot, BmageOperationContext.Default);
        Assert.IsGreaterThan(0, (int)blurred[24, 16].R);
        Assert.IsGreaterThan(0, (int)blurred[16, 24].R);
        Assert.AreEqual(0, blurred[24, 24].R);
        Assert.AreEqual(0, blurred[16, 16].R);
        // Angles half a turn apart are the same line.
        using var opposite = new BmageMotionBlurFilter(14, -135).Apply(dot, BmageOperationContext.Default);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(blurred, opposite));
        Assert.AreEqual(45f, new BmageMotionBlurFilter(14, 225).Angle);
    }

    [TestMethod]
    public void TheTotalLightIsKept()
    {
        using var dot = Dot(61, new Point(30, 30));
        foreach (var (distance, angle) in new[] { (5f, 0f), (17.5f, 30f), (24f, -70f), (12f, 90f) })
        {
            using var blurred = new BmageMotionBlurFilter(distance, angle).Apply(dot, BmageOperationContext.Default);
            double sum = 0;
            for (var y = 0; y < 61; y++) foreach (var p in blurred.GetRow(y)) sum += p.R;
            Assert.AreEqual(255, sum, 255 * 0.08, $"{distance}px at {angle} degrees");
        }
    }

    [TestMethod]
    public void TheEdgesRepeatSoAnOpaqueImageStaysOpaque()
    {
        using var source = ParallelProcessingTests.Noise(90, 70, seed: 4);
        for (var y = 0; y < 70; y++) foreach (ref var p in source.GetRow(y)) p = new BmageRgba32(p.R, p.G, p.B, 255);
        using var blurred = new BmageMotionBlurFilter(40, 20).Apply(source, BmageOperationContext.Default);
        for (var y = 0; y < 70; y++)
            foreach (var p in blurred.GetRow(y)) Assert.AreEqual(255, p.A);
        // A uniform image is unchanged right up to its borders.
        using var flat = BmageImageBuffer.Create(30, 30, new BmageRgba32(10, 120, 200));
        using var flatBlurred = new BmageMotionBlurFilter(25, 63).Apply(flat, BmageOperationContext.Default);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(flat, flatBlurred));
    }

    [TestMethod]
    public void TransparentPixelsDoNotDarkenTheBlur()
    {
        // Red next to fully transparent black: the blur's colour stays red, only its alpha falls.
        using var image = BmageImageBuffer.Create(20, 3, BmageRgba32.Transparent);
        image.Fill(new Rectangle(0, 0, 10, 3), new BmageRgba32(255, 0, 0));
        using var blurred = new BmageMotionBlurFilter(8, 0).Apply(image, BmageOperationContext.Default);
        var edge = blurred[10, 1];
        Assert.IsTrue(edge.A is > 0 and < 255, $"alpha {edge.A}");
        Assert.AreEqual(255, edge.R, 1);
    }

    [TestMethod]
    public void TheDistanceChangesTheBlurSmoothly()
    {
        using var dot = Dot(41, new Point(20, 20));
        var previous = 255.0;
        for (var distance = 1f; distance <= 6f; distance += 0.25f)
        {
            using var blurred = new BmageMotionBlurFilter(distance, 0).Apply(dot, BmageOperationContext.Default);
            var centre = blurred[20, 20].R;
            Assert.IsLessThanOrEqualTo(previous, centre, $"at {distance}px");
            Assert.IsLessThan(255.0 / 3, previous - centre, $"no step larger than a third of the light at {distance}px");
            previous = centre;
        }
    }

    [TestMethod]
    public void ARegionLimitsTheBlurButReadsAcrossItsEdge()
    {
        using var source = ParallelProcessingTests.Noise(80, 60, seed: 2);
        var region = new Rectangle(20, 10, 30, 20);
        using var blurred = new BmageMotionBlurFilter(15, 10, region).Apply(source, BmageOperationContext.Default.WithMutation(false));
        for (var y = 0; y < 60; y++)
            for (var x = 0; x < 80; x++)
                if (!region.Contains(x, y)) Assert.AreEqual(source[x, y], blurred[x, y], $"{x},{y} is outside the region");
        using var whole = new BmageMotionBlurFilter(15, 10).Apply(source, BmageOperationContext.Default.WithMutation(false));
        Assert.AreEqual(whole[30, 20], blurred[30, 20], "inside, the region's pixels are those of the whole image's blur");
    }

    [TestMethod]
    public void ManyThreadsGiveTheSamePixels()
    {
        using var source = ParallelProcessingTests.Noise(700, 300, seed: 21);
        foreach (var angle in new[] { 0f, 17f, -50f, 90f })
        {
            using var one = new BmageMotionBlurFilter(23, angle).Apply(source, BmageOperationContext.Default.WithMutation(false));
            using var many = new BmageMotionBlurFilter(23, angle).Apply(source, new BmageOperationContext(maxDegreeOfParallelism: 4).WithMutation(false));
            Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many), $"angle {angle}");
        }
    }

    [TestMethod]
    public void ForScaleShortensTheStreak()
    {
        var scaled = (BmageMotionBlurFilter)new BmageMotionBlurFilter(40, 30, new Rectangle(100, 100, 400, 200)).ForScale(0.25);
        Assert.AreEqual(10f, scaled.Distance);
        Assert.AreEqual(30f, scaled.Angle);
        Assert.AreEqual(new Rectangle(25, 25, 100, 50), scaled.Region);
        Assert.IsTrue(new BmageMotionBlurFilter(0.5f).IsIdentity);
    }

    [TestMethod]
    public void InvalidSettingsAreRejectedAndCancellationIsHonoured()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageMotionBlurFilter(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageMotionBlurFilter(BmageMotionBlurFilter.MaxDistance + 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageMotionBlurFilter(5, float.NaN));
        using var source = TestImages.Gradient(300, 200);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => new BmageMotionBlurFilter(20).Apply(source, BmageOperationContext.Default.WithCancellation(cts.Token)).Dispose());
    }
}

[TestClass]
public sealed class HighPassTests
{
    [TestMethod]
    public void AFlatImageBecomesMidGrey()
    {
        using var flat = BmageImageBuffer.Create(20, 20, new BmageRgba32(30, 200, 90, 180));
        using var output = new BmageHighPassFilter(3).Apply(flat, BmageOperationContext.Default);
        for (var y = 0; y < 20; y++)
            foreach (var p in output.GetRow(y)) Assert.AreEqual(new BmageRgba32(128, 128, 128, 180), p);
    }

    [TestMethod]
    public void EdgesStandOutEitherSideOfGrey()
    {
        using var image = BmageImageBuffer.Create(40, 10, BmageRgba32.Black);
        image.Fill(new Rectangle(20, 0, 20, 10), BmageRgba32.White);
        using var output = BmageImagePipeline.Create().HighPass(2).Execute(image);
        Assert.IsLessThan(128, (int)output[19, 5].R, "the dark side of the edge goes below grey");
        Assert.IsGreaterThan(128, (int)output[20, 5].R, "the light side goes above it");
        Assert.AreEqual(128, output[2, 5].R, 1, "far from the edge, nothing");
    }

    [TestMethod]
    public void TheRadiusMustBePositive()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageHighPassFilter(0));
        Assert.AreEqual(0.5f, ((BmageHighPassFilter)new BmageHighPassFilter(1).ForScale(0.1)).Radius);
    }
}
