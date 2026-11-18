using System.Numerics;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class ColorAdjustmentTests
{
    private static BmageRgba32 Apply(IBmageImageOperation operation, BmageRgba32 pixel)
    {
        using var image = BmageImageBuffer.Create(1, 1, pixel);
        using var output = operation.Apply(image, BmageOperationContext.Default.WithMutation(false));
        return output[0, 0];
    }

    private static void AssertClose(BmageRgba32 expected, BmageRgba32 actual, int tolerance = 1, string? message = null)
    {
        var difference = Math.Max(Math.Max(Math.Abs(expected.R - actual.R), Math.Abs(expected.G - actual.G)), Math.Max(Math.Abs(expected.B - actual.B), Math.Abs(expected.A - actual.A)));
        Assert.IsLessThanOrEqualTo(tolerance, difference, $"{message} expected {expected}, got {actual}");
    }

    // ---- Channel mixer ----

    [TestMethod]
    public void TheChannelMixerSwapsAndBlendsChannels()
    {
        var swap = new BmageChannelMixerAdjustment(Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX);
        Assert.AreEqual(new BmageRgba32(10, 50, 200, 77), Apply(swap, new BmageRgba32(200, 50, 10, 77)));

        var identity = new BmageChannelMixerAdjustment(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        Assert.IsTrue(identity.IsIdentity);
        Assert.IsFalse(identity.IsMonochrome);
    }

    [TestMethod]
    public void AMonochromeMixIsBlackAndWhiteFromTheChosenChannels()
    {
        var redFilter = BmageChannelMixerAdjustment.Monochrome(1, 0, 0);
        Assert.IsTrue(redFilter.IsMonochrome);
        Assert.AreEqual(new BmageRgba32(180, 180, 180), Apply(redFilter, new BmageRgba32(180, 40, 90)));
        // A blue sky through a red filter goes dark; through a blue one it goes light.
        var sky = new BmageRgba32(70, 130, 220);
        Assert.IsLessThan(Apply(BmageChannelMixerAdjustment.Monochrome(0, 0, 1), sky).R, Apply(redFilter, sky).R);
    }

    [TestMethod]
    public void ChannelMixerWeightsMustBeFinite()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageChannelMixerAdjustment(new Vector3(float.NaN, 0, 0), Vector3.UnitY, Vector3.UnitZ));

    // ---- White balance ----

    [TestMethod]
    public void APickedColourBecomesTheGreyOfItsLuminance()
    {
        var cast = new BmageRgba32(200, 180, 140);
        var balanced = Apply(BmageWhiteBalanceAdjustment.FromNeutral(cast), cast);
        Assert.IsLessThanOrEqualTo(1, Math.Abs(balanced.R - balanced.G));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(balanced.G - balanced.B));
        var luminance = 0.2126 * 200 + 0.7152 * 180 + 0.0722 * 140;
        Assert.IsLessThanOrEqualTo(1.5, Math.Abs(balanced.G - luminance));
    }

    [TestMethod]
    public void WhiteBalanceStrengthAndGainsAreBounded()
    {
        Assert.IsTrue(BmageWhiteBalanceAdjustment.FromNeutral(new BmageRgba32(200, 180, 140), strength: 0).IsIdentity);
        var dark = BmageWhiteBalanceAdjustment.FromNeutral(new BmageRgba32(0, 0, 255));
        foreach (var gain in new[] { dark.RedGain, dark.GreenGain, dark.BlueGain })
            Assert.IsTrue(gain >= BmageWhiteBalanceAdjustment.MinGain && gain <= BmageWhiteBalanceAdjustment.MaxGain, $"gain {gain}");
        Assert.AreEqual(4f, new BmageWhiteBalanceAdjustment(9, 1, 1).RedGain);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageWhiteBalanceAdjustment(float.PositiveInfinity, 1, 1));
    }

    [TestMethod]
    public void AutoWhiteBalanceRemovesACast()
    {
        // Grey patches photographed under bluish light.
        using var image = BmageImageBuffer.Create(40, 30);
        for (var y = 0; y < 30; y++)
            for (var x = 0; x < 40; x++)
            {
                var v = 40 + (x + y) * 3;
                image[x, y] = new BmageRgba32((byte)(v * 0.85), (byte)(v * 0.95), (byte)Math.Min(240, v * 1.2));
            }
        using var corrected = new BmageAutoWhiteBalanceOperation(1).Apply(image, BmageOperationContext.Default.WithMutation(false));
        static (double R, double G, double B) Mean(BmageImageBuffer i)
        {
            double r = 0, g = 0, b = 0;
            for (var y = 0; y < i.Height; y++) foreach (var p in i.GetRow(y)) { r += p.R; g += p.G; b += p.B; }
            var n = (double)i.Width * i.Height;
            return (r / n, g / n, b / n);
        }
        var before = Mean(image);
        var after = Mean(corrected);
        Assert.IsGreaterThan(20, before.B - before.R);
        Assert.IsLessThan(3, Math.Abs(after.B - after.R), $"after: {after}");
    }

    [TestMethod]
    public void AutoWhiteBalanceLeavesAnImageWithNothingToMeasureAlone()
    {
        // Only clipped highlights and black: no mid-tone to say what grey is.
        using var image = BmageImageBuffer.Create(4, 4, new BmageRgba32(255, 255, 255));
        image[0, 0] = BmageRgba32.Black;
        Assert.IsNull(new BmageAutoWhiteBalanceOperation().Measure(image));
        using var output = new BmageAutoWhiteBalanceOperation().Apply(image, BmageOperationContext.Default.WithMutation(false));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(image, output));
        Assert.IsTrue(new BmageAutoWhiteBalanceOperation(0).IsIdentity);
    }

    // ---- Whites and blacks ----

    [TestMethod]
    public void WhitesAndBlacksMoveTheEndsAndBarelyTheMiddle()
    {
        Assert.IsTrue(new BmageWhitesBlacksAdjustment(0, 0).IsIdentity);
        var whites = new BmageWhitesBlacksAdjustment(1, 0);
        Assert.IsGreaterThan(230, (int)Apply(whites, new BmageRgba32(215, 215, 215)).R);
        Assert.AreEqual(255, Apply(whites, new BmageRgba32(240, 240, 240)).R, "positive whites clip the top of the range");
        AssertClose(new BmageRgba32(100, 100, 100), Apply(whites, new BmageRgba32(100, 100, 100)), 1, "mid-tones under whites");

        var blacks = new BmageWhitesBlacksAdjustment(0, -1);
        Assert.AreEqual(0, Apply(blacks, new BmageRgba32(30, 30, 30)).R, "negative blacks crush the bottom of the range");
        AssertClose(new BmageRgba32(170, 170, 170), Apply(blacks, new BmageRgba32(170, 170, 170)), 1, "mid-tones under blacks");
        Assert.IsGreaterThan(40, (int)Apply(new BmageWhitesBlacksAdjustment(0, 1), new BmageRgba32(10, 10, 10)).R, "positive blacks lift the shadows");
    }

    [TestMethod]
    public void WhitesAndBlacksNeverReorderTones()
    {
        foreach (var whites in new[] { -1f, -0.5f, 0.5f, 1f })
            foreach (var blacks in new[] { -1f, -0.5f, 0.5f, 1f })
            {
                var curve = new BmageWhitesBlacksAdjustment(whites, blacks).Red!;
                for (var i = 1; i < 256; i++)
                    Assert.IsGreaterThanOrEqualTo(curve[i - 1], curve[i], $"whites {whites}, blacks {blacks} at {i}");
            }
    }

    // ---- Colorize ----

    [TestMethod]
    public void ColorizeGivesEveryPixelOneHueAndKeepsItsLightness()
    {
        var red = new BmageColorizeAdjustment(0, saturation: 1);
        AssertClose(new BmageRgba32(255, 0, 0), Apply(red, new BmageRgba32(128, 128, 128)), 1);
        AssertClose(new BmageRgba32(128, 0, 0), Apply(red, new BmageRgba32(64, 64, 64)), 1, "a quarter lightness");
        // Lightness (250 + 10) / 2 is a little over half, so the red is a little paler than pure.
        AssertClose(new BmageRgba32(255, 5, 5, 90), Apply(red, new BmageRgba32(10, 250, 120, 90)), 1, "alpha kept, hue replaced");
        Assert.IsTrue(new BmageColorizeAdjustment(200, 0.5f, 0, amount: 0).IsIdentity);
        Assert.AreEqual(340f, new BmageColorizeAdjustment(-20).Hue);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageColorizeAdjustment(float.NaN));
    }

    // ---- HSL ----

    [TestMethod]
    public void AnEmptyHslAdjustmentChangesNothing()
    {
        var hsl = new BmageHslAdjustment();
        Assert.IsTrue(hsl.IsIdentity);
        Assert.AreEqual(0, hsl.Shifts.Count());
    }

    [TestMethod]
    public void DesaturatingReadsTurnsRedGreyAndLeavesBlueAlone()
    {
        var hsl = new BmageHslAdjustment().With(BmageHueBand.Reds, new BmageHslShift(Saturation: -1));
        AssertClose(new BmageRgba32(128, 128, 128), Apply(hsl, new BmageRgba32(255, 0, 0)), 1);
        Assert.AreEqual(new BmageRgba32(0, 0, 255), Apply(hsl, new BmageRgba32(0, 0, 255)));
    }

    [TestMethod]
    public void GreysAreUntouchedWhateverTheShifts()
    {
        var hsl = new BmageHslAdjustment();
        foreach (var band in Enum.GetValues<BmageHueBand>()) hsl = hsl.With(band, new BmageHslShift(40, 1, -1));
        foreach (var v in new byte[] { 0, 37, 128, 200, 255 })
            Assert.AreEqual(new BmageRgba32(v, v, v), Apply(hsl, new BmageRgba32(v, v, v)));
    }

    [TestMethod]
    public void AHueShiftTurnsTheBandsColour()
    {
        // Green at 120 degrees, turned the full 60 the shift allows, is aqua.
        var hsl = new BmageHslAdjustment().With(BmageHueBand.Greens, new BmageHslShift(Hue: 90));
        Assert.AreEqual(60f, hsl[BmageHueBand.Greens].Hue, "the shift is clamped to 60 degrees");
        AssertClose(new BmageRgba32(0, 255, 255), Apply(hsl, new BmageRgba32(0, 255, 0)), 1);
    }

    [TestMethod]
    public void BandsBlendSmoothlyBetweenTheirCentres()
    {
        // Lighten the yellows only; colours on the way from yellow (60) to green (120) are lightened less and less.
        var hsl = new BmageHslAdjustment().With(BmageHueBand.Yellows, new BmageHslShift(Lightness: 1));
        var previous = int.MaxValue;
        for (var degrees = 60; degrees <= 120; degrees += 10)
        {
            var c = BmageColorMath.HslToRgb(degrees / 60f, 1, 0.4f);
            var pixel = new BmageRgba32((byte)Math.Round(c.X * 255), (byte)Math.Round(c.Y * 255), (byte)Math.Round(c.Z * 255));
            var output = Apply(hsl, pixel);
            var gain = output.R + output.G + output.B - (pixel.R + pixel.G + pixel.B);
            Assert.IsLessThanOrEqualTo(previous, gain, $"at {degrees} degrees");
            previous = gain;
            if (degrees == 120) Assert.IsLessThanOrEqualTo(2, gain, "pure green is outside the yellows");
        }
    }

    [TestMethod]
    public void HslAdjustmentsAreImmutable()
    {
        var original = new BmageHslAdjustment();
        var changed = original.With(BmageHueBand.Blues, new BmageHslShift(Saturation: 0.3f));
        Assert.IsTrue(original.IsIdentity);
        Assert.AreEqual(0.3f, changed[BmageHueBand.Blues].Saturation);
        Assert.AreEqual(BmageHueBand.Blues, changed.Shifts.Single().Key);
    }

    // ---- Gradient map ----

    [TestMethod]
    public void ADuotoneMapsBlackAndWhiteToItsTwoColours()
    {
        var navy = new BmageRgba32(20, 30, 90);
        var gold = new BmageRgba32(250, 210, 120);
        var duotone = BmageGradientMapOperation.Duotone(navy, gold);
        Assert.AreEqual(navy, Apply(duotone, BmageRgba32.Black));
        Assert.AreEqual(gold, Apply(duotone, BmageRgba32.White));
        AssertClose(new BmageRgba32(135, 120, 105), Apply(duotone, new BmageRgba32(128, 128, 128)), 2, "mid grey sits half way");
    }

    [TestMethod]
    public void ABlackToWhiteMapIsTheLuminance()
    {
        var map = BmageGradientMapOperation.Duotone(BmageRgba32.Black, BmageRgba32.White);
        var pixel = new BmageRgba32(200, 100, 50, 33);
        var l = (byte)Math.Round(0.2126 * 200 + 0.7152 * 100 + 0.0722 * 50);
        AssertClose(new BmageRgba32(l, l, l, 33), Apply(map, pixel), 1);
    }

    [TestMethod]
    public void StopsAreOrderedAndInterpolated()
    {
        var map = new BmageGradientMapOperation([(1f, BmageRgba32.White), (0f, BmageRgba32.Black), (0.5f, new BmageRgba32(255, 0, 0))]);
        CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f }, map.Stops.Select(s => s.Offset).ToArray());
        AssertClose(new BmageRgba32(255, 0, 0), Apply(map, new BmageRgba32(128, 128, 128)), 2);
        var half = new BmageGradientMapOperation([(0f, BmageRgba32.Black), (1f, BmageRgba32.Black)], amount: 0.5f);
        AssertClose(new BmageRgba32(100, 100, 100), Apply(half, new BmageRgba32(200, 200, 200)), 1);
        Assert.ThrowsExactly<ArgumentException>(() => new BmageGradientMapOperation([(0f, BmageRgba32.Black)]));
    }

    // ---- In pipelines ----

    [TestMethod]
    public void TheNewAdjustmentsFuseIntoOnePassWithTheSamePixels()
    {
        using var source = ParallelProcessingTests.Noise(64, 48, seed: 5);
        var pipeline = BmageImagePipeline.Create()
            .WhitesBlacks(0.3f, -0.2f)
            .Hsl(BmageHueBand.Blues, saturation: 0.4f)
            .WhiteBalance(new BmageRgba32(190, 180, 160))
            .Colorize(30, 0.3f, amount: 0.4f)
            .Duotone(new BmageRgba32(10, 20, 60), new BmageRgba32(240, 220, 180), 0.5f)
            .BlackAndWhite(0.5f, 0.3f, 0.2f);
        Assert.AreEqual(1, pipeline.Optimize(source.Size).Count, "the per-pixel run fuses into a single pass");

        using var fused = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = true });
        using var stepwise = pipeline.Execute(source, new BmagePipelineExecutionOptions { Optimize = false });
        // Fusing removes the rounding between stages, so the two differ by rounding alone.
        Assert.IsLessThanOrEqualTo(3, TestImages.MaxChannelDifference(stepwise, fused));
    }

    [TestMethod]
    public void TheNewAdjustmentsGiveTheSamePixelsOnAnyNumberOfThreads()
    {
        using var source = ParallelProcessingTests.Noise(300, 200, seed: 8);
        foreach (var operation in new IBmageImageOperation[]
        {
            new BmageHslAdjustment().With(BmageHueBand.Reds, new BmageHslShift(10, 0.5f, -0.3f)).With(BmageHueBand.Aquas, new BmageHslShift(-20, -0.5f, 0.2f)),
            BmageGradientMapOperation.Duotone(new BmageRgba32(0, 40, 80), new BmageRgba32(255, 200, 0)),
            new BmageColorizeAdjustment(210, 0.6f, 0.1f),
            new BmageAutoWhiteBalanceOperation(),
        })
        {
            using var one = operation.Apply(source, BmageOperationContext.Default.WithMutation(false));
            using var many = operation.Apply(source, new BmageOperationContext(maxDegreeOfParallelism: 4).WithMutation(false));
            Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many), operation.Name);
        }
    }
}
