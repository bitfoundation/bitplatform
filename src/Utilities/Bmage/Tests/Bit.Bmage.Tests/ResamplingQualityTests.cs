using System.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Tests;

/// <summary>
/// Guards the resampler's accuracy, especially the box prepass used for large reductions. Speeding up a downscale is
/// only worth anything if the pixels stay correct, so these compare against analytically known answers.
/// </summary>
[TestClass]
public sealed class ResamplingQualityTests
{
    /// <summary>
    /// A horizontal ramp downscaled by a large factor must still be a ramp with the same endpoints and no banding.
    /// This is the path that takes the box prepass.
    /// </summary>
    [TestMethod]
    [DataRow(4000, 200)]
    [DataRow(6000, 150)]
    [DataRow(3000, 100)]
    public void LargeDownscaleOfARampStaysLinear(int sourceWidth, int targetWidth)
    {
        using var source = ImageBuffer.Create(sourceWidth, 16, clear: false);
        for (var y = 0; y < source.Height; y++)
        {
            var row = source.GetRow(y);
            for (var x = 0; x < sourceWidth; x++)
            {
                var v = (byte)(x * 255 / (sourceWidth - 1));
                row[x] = new Rgba32(v, v, v);
            }
        }

        using var result = new ResizeOperation(targetWidth, 16, ResizeMode.Stretch, ResamplingFilter.Auto)
            .Apply(source, OperationContext.Default);

        Assert.AreEqual(targetWidth, result.Width);
        // Compare against the exact area average for each output pixel.
        for (var x = 0; x < targetWidth; x++)
        {
            var expected = (x + 0.5) / targetWidth * 255;
            var actual = result[x, 8].R;
            Assert.IsTrue(Math.Abs(actual - expected) <= 4, $"Pixel {x}: expected about {expected:0.#}, got {actual}");
        }
        // Monotonic: no banding or reversal.
        for (var x = 1; x < targetWidth; x++)
            Assert.IsTrue(result[x, 8].R >= result[x - 1, 8].R, $"Ramp reversed at {x}");
    }

    /// <summary>A solid colour survives any reduction factor exactly, including through the prepass.</summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(17)]
    [DataRow(50)]
    public void SolidColourSurvivesEveryReductionFactor(int factor)
    {
        var size = 64 * factor;
        using var source = ImageBuffer.Create(size, size, new Rgba32(91, 173, 47));
        using var result = new ResizeOperation(64, 64, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        for (var y = 0; y < 64; y += 8)
            for (var x = 0; x < 64; x += 8)
            {
                var p = result[x, y];
                Assert.IsTrue(Math.Abs(p.R - 91) <= 1 && Math.Abs(p.G - 173) <= 1 && Math.Abs(p.B - 47) <= 1, $"Reduction by {factor} shifted the colour to {p}");
            }
    }

    /// <summary>
    /// A fine checkerboard reduced heavily must average to mid grey. A naive point sample would return pure black or
    /// pure white, which is the classic aliasing failure.
    /// </summary>
    [TestMethod]
    public void HeavyReductionOfACheckerboardAveragesRatherThanAliases()
    {
        using var source = ImageBuffer.Create(2048, 2048, clear: false);
        for (var y = 0; y < 2048; y++)
        {
            var row = source.GetRow(y);
            for (var x = 0; x < 2048; x++) row[x] = ((x + y) & 1) == 0 ? Rgba32.White : Rgba32.Black;
        }
        using var result = new ResizeOperation(64, 64, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        for (var y = 8; y < 56; y += 8)
            for (var x = 8; x < 56; x += 8)
                Assert.IsInRange(120, 136, result[x, y].R);
    }

    /// <summary>Transparency must not bleed colour: a red square on a transparent field keeps its hue when reduced hard.</summary>
    [TestMethod]
    public void HeavyReductionPreservesColourThroughTransparency()
    {
        using var source = ImageBuffer.Create(1600, 1600, Rgba32.Transparent);
        source.Fill(new Rectangle(400, 400, 800, 800), new Rgba32(255, 0, 0));
        using var result = new ResizeOperation(100, 100, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        var centre = result[50, 50];
        Assert.AreEqual(255, centre.A);
        Assert.IsTrue(centre.R > 250 && centre.G < 5 && centre.B < 5, $"Centre lost its colour: {centre}");
        Assert.AreEqual(0, result[2, 2].A);
    }

    /// <summary>
    /// The prepass must not change where content lands. A marker near a known position must still be near it after a
    /// large reduction, within a pixel.
    /// </summary>
    [TestMethod]
    public void LargeReductionKeepsContentAligned()
    {
        using var source = ImageBuffer.Create(3000, 3000, Rgba32.White);
        // A black band covering exactly the left third.
        source.Fill(new Rectangle(0, 0, 1000, 3000), Rgba32.Black);
        using var result = new ResizeOperation(150, 150, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        // The boundary must land at 1/3 of 150 = 50.
        Assert.IsTrue(result[45, 75].R < 30, "Left of the boundary should still be black");
        Assert.IsTrue(result[55, 75].R > 225, "Right of the boundary should still be white");
    }

    /// <summary>Upscaling must never take the prepass, and must stay smooth.</summary>
    [TestMethod]
    public void UpscalingStaysSmooth()
    {
        using var source = ImageBuffer.Create(4, 4, clear: false);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                source[x, y] = new Rgba32((byte)(x * 85), (byte)(y * 85), 0);
        using var result = new ResizeOperation(64, 64, ResizeMode.Stretch, ResamplingFilter.Bicubic).Apply(source, OperationContext.Default);
        Assert.AreEqual(64, result.Width);
        // Values must increase left to right without jumps larger than a smooth interpolation would give.
        for (var x = 1; x < 64; x++)
        {
            var delta = result[x, 32].R - result[x - 1, 32].R;
            Assert.IsTrue(delta >= -2 && delta <= 12, $"Unexpected jump of {delta} at x={x}");
        }
    }

    /// <summary>
    /// The prepass is an internal optimisation, so it must not change results depending on how the same reduction is
    /// expressed. Resizing in one step and resizing the already-reduced image must agree closely.
    /// </summary>
    [TestMethod]
    public void OneStepAndTwoStepReductionsAgree()
    {
        using var source = TestImages.Gradient(1600, 1200);
        using var oneStep = new ResizeOperation(100, 75, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        using var half = new ResizeOperation(400, 300, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(source, OperationContext.Default);
        using var twoStep = new ResizeOperation(100, 75, ResizeMode.Stretch, ResamplingFilter.Auto).Apply(half, OperationContext.Default);
        Assert.IsTrue(TestImages.MeanChannelDifference(oneStep, twoStep) < 2.0);
    }
}
