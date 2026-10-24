using System.Numerics;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class ColorAdjustmentTests
{
    private static Rgba32 Apply(IImageOperation operation, Rgba32 pixel)
    {
        using var image = ImageBuffer.Create(1, 1, pixel);
        using var output = operation.Apply(image, OperationContext.Default.WithMutation(false));
        return output[0, 0];
    }

    private static void AssertClose(Rgba32 expected, Rgba32 actual, int tolerance = 1, string? message = null)
    {
        var difference = Math.Max(Math.Max(Math.Abs(expected.R - actual.R), Math.Abs(expected.G - actual.G)), Math.Max(Math.Abs(expected.B - actual.B), Math.Abs(expected.A - actual.A)));
        Assert.IsLessThanOrEqualTo(tolerance, difference, $"{message} expected {expected}, got {actual}");
    }

    // ---- Channel mixer ----

    [TestMethod]
    public void TheChannelMixerSwapsAndBlendsChannels()
    {
        var swap = new ChannelMixerAdjustment(Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX);
        Assert.AreEqual(new Rgba32(10, 50, 200, 77), Apply(swap, new Rgba32(200, 50, 10, 77)));

        var identity = new ChannelMixerAdjustment(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        Assert.IsTrue(identity.IsIdentity);
        Assert.IsFalse(identity.IsMonochrome);
    }

    [TestMethod]
    public void AMonochromeMixIsBlackAndWhiteFromTheChosenChannels()
    {
        var redFilter = ChannelMixerAdjustment.Monochrome(1, 0, 0);
        Assert.IsTrue(redFilter.IsMonochrome);
        Assert.AreEqual(new Rgba32(180, 180, 180), Apply(redFilter, new Rgba32(180, 40, 90)));
        // A blue sky through a red filter goes dark; through a blue one it goes light.
        var sky = new Rgba32(70, 130, 220);
        Assert.IsLessThan(Apply(ChannelMixerAdjustment.Monochrome(0, 0, 1), sky).R, Apply(redFilter, sky).R);
    }

    [TestMethod]
    public void ChannelMixerWeightsMustBeFinite()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ChannelMixerAdjustment(new Vector3(float.NaN, 0, 0), Vector3.UnitY, Vector3.UnitZ));

    // ---- White balance ----

    [TestMethod]
    public void APickedColourBecomesTheGreyOfItsLuminance()
    {
        var cast = new Rgba32(200, 180, 140);
        var balanced = Apply(WhiteBalanceAdjustment.FromNeutral(cast), cast);
        Assert.IsLessThanOrEqualTo(1, Math.Abs(balanced.R - balanced.G));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(balanced.G - balanced.B));
        var luminance = 0.2126 * 200 + 0.7152 * 180 + 0.0722 * 140;
        Assert.IsLessThanOrEqualTo(1.5, Math.Abs(balanced.G - luminance));
    }

    [TestMethod]
    public void WhiteBalanceStrengthAndGainsAreBounded()
    {
        Assert.IsTrue(WhiteBalanceAdjustment.FromNeutral(new Rgba32(200, 180, 140), strength: 0).IsIdentity);
        var dark = WhiteBalanceAdjustment.FromNeutral(new Rgba32(0, 0, 255));
        foreach (var gain in new[] { dark.RedGain, dark.GreenGain, dark.BlueGain })
            Assert.IsTrue(gain >= WhiteBalanceAdjustment.MinGain && gain <= WhiteBalanceAdjustment.MaxGain, $"gain {gain}");
        Assert.AreEqual(4f, new WhiteBalanceAdjustment(9, 1, 1).RedGain);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WhiteBalanceAdjustment(float.PositiveInfinity, 1, 1));
    }

    [TestMethod]
    public void AutoWhiteBalanceRemovesACast()
    {
        // Grey patches photographed under bluish light.
        using var image = ImageBuffer.Create(40, 30);
        for (var y = 0; y < 30; y++)
            for (var x = 0; x < 40; x++)
            {
                var v = 40 + (x + y) * 3;
                image[x, y] = new Rgba32((byte)(v * 0.85), (byte)(v * 0.95), (byte)Math.Min(240, v * 1.2));
            }
        using var corrected = new AutoWhiteBalanceOperation(1).Apply(image, OperationContext.Default.WithMutation(false));
        static (double R, double G, double B) Mean(ImageBuffer i)
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
        using var image = ImageBuffer.Create(4, 4, new Rgba32(255, 255, 255));
        image[0, 0] = Rgba32.Black;
        Assert.IsNull(new AutoWhiteBalanceOperation().Measure(image));
        using var output = new AutoWhiteBalanceOperation().Apply(image, OperationContext.Default.WithMutation(false));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(image, output));
        Assert.IsTrue(new AutoWhiteBalanceOperation(0).IsIdentity);
    }

    // ---- Whites and blacks ----

    [TestMethod]
    public void WhitesAndBlacksMoveTheEndsAndBarelyTheMiddle()
    {
        Assert.IsTrue(new WhitesBlacksAdjustment(0, 0).IsIdentity);
        var whites = new WhitesBlacksAdjustment(1, 0);
        Assert.IsGreaterThan(230, (int)Apply(whites, new Rgba32(215, 215, 215)).R);
        Assert.AreEqual(255, Apply(whites, new Rgba32(240, 240, 240)).R, "positive whites clip the top of the range");
        AssertClose(new Rgba32(100, 100, 100), Apply(whites, new Rgba32(100, 100, 100)), 1, "mid-tones under whites");

        var blacks = new WhitesBlacksAdjustment(0, -1);
        Assert.AreEqual(0, Apply(blacks, new Rgba32(30, 30, 30)).R, "negative blacks crush the bottom of the range");
        AssertClose(new Rgba32(170, 170, 170), Apply(blacks, new Rgba32(170, 170, 170)), 1, "mid-tones under blacks");
        Assert.IsGreaterThan(40, (int)Apply(new WhitesBlacksAdjustment(0, 1), new Rgba32(10, 10, 10)).R, "positive blacks lift the shadows");
    }

    [TestMethod]
    public void WhitesAndBlacksNeverReorderTones()
    {
        foreach (var whites in new[] { -1f, -0.5f, 0.5f, 1f })
            foreach (var blacks in new[] { -1f, -0.5f, 0.5f, 1f })
            {
                var curve = new WhitesBlacksAdjustment(whites, blacks).Red!;
                for (var i = 1; i < 256; i++)
                    Assert.IsGreaterThanOrEqualTo(curve[i - 1], curve[i], $"whites {whites}, blacks {blacks} at {i}");
            }
    }

    // ---- Colorize ----

    [TestMethod]
    public void ColorizeGivesEveryPixelOneHueAndKeepsItsLightness()
    {
        var red = new ColorizeAdjustment(0, saturation: 1);
        AssertClose(new Rgba32(255, 0, 0), Apply(red, new Rgba32(128, 128, 128)), 1);
        AssertClose(new Rgba32(128, 0, 0), Apply(red, new Rgba32(64, 64, 64)), 1, "a quarter lightness");
        // Lightness (250 + 10) / 2 is a little over half, so the red is a little paler than pure.
        AssertClose(new Rgba32(255, 5, 5, 90), Apply(red, new Rgba32(10, 250, 120, 90)), 1, "alpha kept, hue replaced");
        Assert.IsTrue(new ColorizeAdjustment(200, 0.5f, 0, amount: 0).IsIdentity);
        Assert.AreEqual(340f, new ColorizeAdjustment(-20).Hue);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ColorizeAdjustment(float.NaN));
    }

    // ---- HSL ----

    [TestMethod]
    public void AnEmptyHslAdjustmentChangesNothing()
    {
        var hsl = new HslAdjustment();
        Assert.IsTrue(hsl.IsIdentity);
        Assert.AreEqual(0, hsl.Shifts.Count());
    }

    [TestMethod]
    public void DesaturatingReadsTurnsRedGreyAndLeavesBlueAlone()
    {
        var hsl = new HslAdjustment().With(HueBand.Reds, new HslShift(Saturation: -1));
        AssertClose(new Rgba32(128, 128, 128), Apply(hsl, new Rgba32(255, 0, 0)), 1);
        Assert.AreEqual(new Rgba32(0, 0, 255), Apply(hsl, new Rgba32(0, 0, 255)));
    }

    [TestMethod]
    public void GreysAreUntouchedWhateverTheShifts()
    {
        var hsl = new HslAdjustment();
        foreach (var band in Enum.GetValues<HueBand>()) hsl = hsl.With(band, new HslShift(40, 1, -1));
        foreach (var v in new byte[] { 0, 37, 128, 200, 255 })
            Assert.AreEqual(new Rgba32(v, v, v), Apply(hsl, new Rgba32(v, v, v)));
    }

    [TestMethod]
    public void AHueShiftTurnsTheBandsColour()
    {
        // Green at 120 degrees, turned the full 60 the shift allows, is aqua.
        var hsl = new HslAdjustment().With(HueBand.Greens, new HslShift(Hue: 90));
        Assert.AreEqual(60f, hsl[HueBand.Greens].Hue, "the shift is clamped to 60 degrees");
        AssertClose(new Rgba32(0, 255, 255), Apply(hsl, new Rgba32(0, 255, 0)), 1);
    }

    [TestMethod]
    public void BandsBlendSmoothlyBetweenTheirCentres()
    {
        // Lighten the yellows only; colours on the way from yellow (60) to green (120) are lightened less and less.
        var hsl = new HslAdjustment().With(HueBand.Yellows, new HslShift(Lightness: 1));
        var previous = int.MaxValue;
        for (var degrees = 60; degrees <= 120; degrees += 10)
        {
            var c = Operations.Adjustments.ColorMath.HslToRgb(degrees / 60f, 1, 0.4f);
            var pixel = new Rgba32((byte)Math.Round(c.X * 255), (byte)Math.Round(c.Y * 255), (byte)Math.Round(c.Z * 255));
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
        var original = new HslAdjustment();
        var changed = original.With(HueBand.Blues, new HslShift(Saturation: 0.3f));
        Assert.IsTrue(original.IsIdentity);
        Assert.AreEqual(0.3f, changed[HueBand.Blues].Saturation);
        Assert.AreEqual(HueBand.Blues, changed.Shifts.Single().Key);
    }

    // ---- Gradient map ----

    [TestMethod]
    public void ADuotoneMapsBlackAndWhiteToItsTwoColours()
    {
        var navy = new Rgba32(20, 30, 90);
        var gold = new Rgba32(250, 210, 120);
        var duotone = GradientMapOperation.Duotone(navy, gold);
        Assert.AreEqual(navy, Apply(duotone, Rgba32.Black));
        Assert.AreEqual(gold, Apply(duotone, Rgba32.White));
        AssertClose(new Rgba32(135, 120, 105), Apply(duotone, new Rgba32(128, 128, 128)), 2, "mid grey sits half way");
    }

    [TestMethod]
    public void ABlackToWhiteMapIsTheLuminance()
    {
        var map = GradientMapOperation.Duotone(Rgba32.Black, Rgba32.White);
        var pixel = new Rgba32(200, 100, 50, 33);
        var l = (byte)Math.Round(0.2126 * 200 + 0.7152 * 100 + 0.0722 * 50);
        AssertClose(new Rgba32(l, l, l, 33), Apply(map, pixel), 1);
    }

    [TestMethod]
    public void StopsAreOrderedAndInterpolated()
    {
        var map = new GradientMapOperation([(1f, Rgba32.White), (0f, Rgba32.Black), (0.5f, new Rgba32(255, 0, 0))]);
        CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f }, map.Stops.Select(s => s.Offset).ToArray());
        AssertClose(new Rgba32(255, 0, 0), Apply(map, new Rgba32(128, 128, 128)), 2);
        var half = new GradientMapOperation([(0f, Rgba32.Black), (1f, Rgba32.Black)], amount: 0.5f);
        AssertClose(new Rgba32(100, 100, 100), Apply(half, new Rgba32(200, 200, 200)), 1);
        Assert.ThrowsExactly<ArgumentException>(() => new GradientMapOperation([(0f, Rgba32.Black)]));
    }

    // ---- In pipelines ----

    [TestMethod]
    public void TheNewAdjustmentsFuseIntoOnePassWithTheSamePixels()
    {
        using var source = ParallelProcessingTests.Noise(64, 48, seed: 5);
        var pipeline = ImagePipeline.Create()
            .WhitesBlacks(0.3f, -0.2f)
            .Hsl(HueBand.Blues, saturation: 0.4f)
            .WhiteBalance(new Rgba32(190, 180, 160))
            .Colorize(30, 0.3f, amount: 0.4f)
            .Duotone(new Rgba32(10, 20, 60), new Rgba32(240, 220, 180), 0.5f)
            .BlackAndWhite(0.5f, 0.3f, 0.2f);
        Assert.AreEqual(1, pipeline.Optimize(source.Size).Count, "the per-pixel run fuses into a single pass");

        using var fused = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = true });
        using var stepwise = pipeline.Execute(source, new PipelineExecutionOptions { Optimize = false });
        // Fusing removes the rounding between stages, so the two differ by rounding alone.
        Assert.IsLessThanOrEqualTo(3, TestImages.MaxChannelDifference(stepwise, fused));
    }

    [TestMethod]
    public void TheNewAdjustmentsGiveTheSamePixelsOnAnyNumberOfThreads()
    {
        using var source = ParallelProcessingTests.Noise(300, 200, seed: 8);
        foreach (var operation in new IImageOperation[]
        {
            new HslAdjustment().With(HueBand.Reds, new HslShift(10, 0.5f, -0.3f)).With(HueBand.Aquas, new HslShift(-20, -0.5f, 0.2f)),
            GradientMapOperation.Duotone(new Rgba32(0, 40, 80), new Rgba32(255, 200, 0)),
            new ColorizeAdjustment(210, 0.6f, 0.1f),
            new AutoWhiteBalanceOperation(),
        })
        {
            using var one = operation.Apply(source, OperationContext.Default.WithMutation(false));
            using var many = operation.Apply(source, new OperationContext(maxDegreeOfParallelism: 4).WithMutation(false));
            Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many), operation.Name);
        }
    }
}
