using System.Drawing;
using Bit.Bmage.Analysis;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>Synthetic pages: lines of "words" on paper, lit evenly or not.</summary>
internal static class Pages
{
    /// <summary>
    /// A page of <paramref name="width"/> by <paramref name="height"/>: rows of dark word blocks 10 pixels tall, 24 apart,
    /// with gaps between words, on paper whose brightness <paramref name="light"/> gives for each column. The ink is a
    /// third of the paper's brightness wherever it is.
    /// </summary>
    public static ImageBuffer Text(int width, int height, Func<int, int>? light = null, int seed = 7)
    {
        light ??= _ => 245;
        var random = new Random(seed);
        var image = ImageBuffer.Create(width, height, Rgba32.White);
        var ink = new bool[width * height];
        for (var top = 30; top + 10 < height - 30; top += 24)
        {
            var x = 30;
            while (x < width - 40)
            {
                var word = random.Next(12, 60);
                for (var y = top; y < top + 10; y++)
                    for (var dx = 0; dx < word && x + dx < width - 30; dx++)
                        ink[y * width + x + dx] = true;
                x += word + random.Next(6, 14);
            }
        }
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var paper = light(x);
                var v = (byte)(ink[y * width + x] ? paper / 3 : paper);
                image[x, y] = new Rgba32(v, v, v);
            }
        return image;
    }

    public static bool IsInk(ImageBuffer page, int x, int y) => page[x, y].R < 128;
}

[TestClass]
public sealed class AdaptiveThresholdTests
{
    [TestMethod]
    public void AShadowAcrossThePageNeitherBlackensThePaperNorLosesTheText()
    {
        // Paper from 70 on the left to 250 on the right: no single level separates ink from paper everywhere.
        using var page = Pages.Text(400, 300, x => 70 + x * 180 / 399);
        using var expected = Pages.Text(400, 300);
        using var result = new AdaptiveThresholdFilter(radius: 15, bias: 0.2f).Apply(page, OperationContext.Default);

        int paper = 0, paperWhite = 0, ink = 0, inkBlack = 0;
        for (var y = 0; y < 300; y++)
            for (var x = 0; x < 400; x++)
            {
                var black = result[x, y].R == 0;
                Assert.IsTrue(result[x, y].R is 0 or 255, "The result is pure black and white.");
                if (Pages.IsInk(expected, x, y)) { ink++; if (black) inkBlack++; }
                else { paper++; if (!black) paperWhite++; }
            }
        Assert.IsGreaterThan(0.99, paperWhite / (double)paper, $"paper kept white: {paperWhite}/{paper}");
        Assert.IsGreaterThan(0.95, inkBlack / (double)ink, $"ink found: {inkBlack}/{ink}");

        // A single level fails on the same page: the level that keeps the dark side's paper white loses the bright side's text.
        using var global = new ThresholdFilter(0.35f).Apply(page, OperationContext.Default);
        var globalPaperBlack = 0;
        for (var y = 0; y < 300; y++)
            for (var x = 0; x < 100; x++)
                if (!Pages.IsInk(expected, x, y) && global[x, y].R == 0) globalPaperBlack++;
        Assert.IsGreaterThan(1000, globalPaperBlack, "The fixture is one a global threshold gets wrong.");
    }

    [TestMethod]
    public void TheMeanMethodSeparatesInkFromPaperToo()
    {
        using var page = Pages.Text(300, 200, x => 90 + x * 150 / 299);
        using var expected = Pages.Text(300, 200);
        using var result = new AdaptiveThresholdFilter(15, 0.06f, AdaptiveThresholdMethod.Mean).Apply(page, OperationContext.Default);
        var wrong = 0;
        for (var y = 0; y < 200; y++)
            for (var x = 0; x < 300; x++)
                if ((result[x, y].R == 0) != Pages.IsInk(expected, x, y)) wrong++;
        Assert.IsLessThan(0.02, wrong / 60000.0, $"{wrong} pixels wrong");
    }

    [TestMethod]
    public void AlphaIsKeptAndOnlyTheRegionChanges()
    {
        using var noise = ParallelProcessingTests.Noise(80, 60, seed: 5);
        var region = new Rectangle(10, 20, 30, 25);
        using var result = new AdaptiveThresholdFilter(5, region: region).Apply(noise, OperationContext.Default);
        for (var y = 0; y < 60; y++)
            for (var x = 0; x < 80; x++)
            {
                Assert.AreEqual(noise[x, y].A, result[x, y].A);
                if (!region.Contains(x, y)) Assert.AreEqual(noise[x, y], result[x, y], $"({x},{y}) is outside the region");
                else Assert.IsTrue(result[x, y].R is 0 or 255 && result[x, y].R == result[x, y].G);
            }
    }

    [TestMethod]
    public void TheSamePixelsComeOutOnOneThreadOrMany()
    {
        using var page = Pages.Text(500, 400, x => 60 + x / 3);
        using var one = new AdaptiveThresholdFilter(12).Apply(page, OperationContext.Default);
        using var many = new AdaptiveThresholdFilter(12).Apply(page, new OperationContext(maxDegreeOfParallelism: 4));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(one, many));
        // In place gives the same pixels as a copy.
        using var copy = page.Clone();
        var inPlace = new AdaptiveThresholdFilter(12).Apply(copy, new OperationContext(canMutateSource: true));
        Assert.AreSame(copy, inPlace);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(one, inPlace));
    }

    [TestMethod]
    public void AWindowLargerThanTheImageIsTheMeanOfTheWholeImage()
    {
        using var image = ImageBuffer.Create(3, 1, Rgba32.Black);
        image[0, 0] = new Rgba32(30, 30, 30);
        image[1, 0] = new Rgba32(100, 100, 100);
        image[2, 0] = new Rgba32(200, 200, 200);
        // Mean 110: the mean method with no offset turns 30 and 100 black and 200 white.
        using var result = new AdaptiveThresholdFilter(50, 0, AdaptiveThresholdMethod.Mean).Apply(image, OperationContext.Default);
        Assert.AreEqual(0, result[0, 0].R);
        Assert.AreEqual(0, result[1, 0].R);
        Assert.AreEqual(255, result[2, 0].R);
    }

    [TestMethod]
    public void TheRadiusScalesForAProxyAndArgumentsAreChecked()
    {
        var filter = new AdaptiveThresholdFilter(20, 0.3f, AdaptiveThresholdMethod.Mean, new Rectangle(100, 100, 200, 200));
        var half = (AdaptiveThresholdFilter)filter.ForScale(0.5);
        Assert.AreEqual(10f, half.Radius);
        Assert.AreEqual(0.3f, half.Bias);
        Assert.AreEqual(AdaptiveThresholdMethod.Mean, half.Method);
        Assert.AreEqual(new Rectangle(50, 50, 100, 100), half.Region);
        Assert.AreEqual(1f, ((AdaptiveThresholdFilter)new AdaptiveThresholdFilter(1).ForScale(0.1)).Radius, "Never below one pixel.");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AdaptiveThresholdFilter(0.5f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AdaptiveThresholdFilter(501));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AdaptiveThresholdFilter(10, 1.5f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AdaptiveThresholdFilter(10, float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AdaptiveThresholdFilter(10, 0.2f, (AdaptiveThresholdMethod)9));
    }

    [TestMethod]
    public void CancellationStopsIt()
    {
        using var page = Pages.Text(300, 300);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => new AdaptiveThresholdFilter(10).Apply(page, new OperationContext(cancellationToken: cts.Token)));
    }
}

[TestClass]
public sealed class DocumentSkewTests
{
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(2.5)]
    [DataRow(-4.0)]
    [DataRow(7.3)]
    [DataRow(-11.0)]
    public void TheTiltOfLinesOfTextIsFoundAndLevellingUndoesIt(double tilt)
    {
        using var page = Pages.Text(700, 900);
        using var tilted = new RotateOperation(tilt, Rgba32.White, expandCanvas: false).Apply(page, OperationContext.Default);
        var skew = DocumentSkew.Estimate(tilted);

        Assert.IsNotNull(skew);
        Assert.AreEqual(-tilt, skew.Value.Angle, 0.15, "The estimate is the rotation that undoes the tilt.");
        Assert.IsGreaterThan(0.5, skew.Value.Confidence, $"confidence {skew.Value.Confidence}");
    }

    [TestMethod]
    public void LightTextOnADarkPageIsMeasuredToo()
    {
        using var page = Pages.Text(600, 600);
        using var inverted = ImagePipeline.Create().Invert().Rotate(3, Rgba32.Black, expandCanvas: false).Execute(page);
        var skew = DocumentSkew.Estimate(inverted);
        Assert.IsNotNull(skew);
        Assert.AreEqual(-3, skew.Value.Angle, 0.15);
    }

    [TestMethod]
    public void ALargeScanIsMeasuredAtTheAnalysisSize()
    {
        using var page = Pages.Text(1400, 1800);
        using var tilted = new RotateOperation(-1.8, Rgba32.White, expandCanvas: false).Apply(page, OperationContext.Default);
        Assert.AreEqual(1.8, DocumentSkew.Estimate(tilted)!.Value.Angle, 0.15);
    }

    [TestMethod]
    public void ABlankPageOrOneWithoutLinesGivesNothingOrLittleConfidence()
    {
        using var blank = ImageBuffer.Create(400, 400, Rgba32.White);
        Assert.IsNull(DocumentSkew.Estimate(blank));
        using var tiny = ImageBuffer.Create(10, 10, Rgba32.White);
        Assert.IsNull(DocumentSkew.Estimate(tiny));
        // Scattered marks with no lines among them: an angle, maybe, but no confidence in it.
        var random = new Random(1);
        using var dots = ImageBuffer.Create(500, 400, Rgba32.White);
        for (var i = 0; i < 3000; i++) dots.Fill(new Rectangle(random.Next(500), random.Next(400), 3, 3), Rgba32.Black);
        var estimate = DocumentSkew.Estimate(dots);
        Assert.IsTrue(estimate is null || estimate.Value.Confidence < 0.3, $"confidence {estimate?.Confidence}");
        // Noise with a grain along the pixel grid may stand out more, but only ever near level.
        using var noise = ParallelProcessingTests.Noise(400, 400, seed: 11);
        var grain = DocumentSkew.Estimate(noise);
        Assert.IsTrue(grain is null || grain.Value.Confidence < 0.3 || Math.Abs(grain.Value.Angle) < 0.5, $"{grain}");
    }

    [TestMethod]
    public void TheSearchStaysWithinTheLimitAndArgumentsAreChecked()
    {
        using var page = Pages.Text(500, 500);
        using var tilted = new RotateOperation(9, Rgba32.White, expandCanvas: false).Apply(page, OperationContext.Default);
        var narrow = DocumentSkew.Estimate(tilted, maxAngle: 5);
        Assert.IsTrue(narrow is null || Math.Abs(narrow.Value.Angle) <= 5);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DocumentSkew.Estimate(page, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DocumentSkew.Estimate(page, 46));
        Assert.ThrowsExactly<ArgumentNullException>(() => DocumentSkew.Estimate(null!));
    }

    [TestMethod]
    public void ARegionIsMeasuredOnItsOwn()
    {
        // A page tilted one way pasted beside one tilted the other: each half reports its own tilt.
        using var a = new RotateOperation(4, Rgba32.White, expandCanvas: false).Apply(Pages.Text(400, 500, seed: 1), OperationContext.Default);
        using var b = new RotateOperation(-6, Rgba32.White, expandCanvas: false).Apply(Pages.Text(400, 500, seed: 2), OperationContext.Default);
        using var both = ImageBuffer.Create(800, 500, Rgba32.White);
        a.CopyTo(both, Point.Empty);
        b.CopyTo(both, new Point(400, 0));
        Assert.AreEqual(-4, DocumentSkew.Estimate(both, region: new Rectangle(0, 0, 400, 500))!.Value.Angle, 0.15);
        Assert.AreEqual(6, DocumentSkew.Estimate(both, region: new Rectangle(400, 0, 400, 500))!.Value.Angle, 0.15);
    }

    [TestMethod]
    public void OtsuSplitsTwoTones()
    {
        var histogram = new int[256];
        histogram[40] = 300;
        histogram[200] = 700;
        var level = DocumentSkew.OtsuLevel(histogram, 1000);
        Assert.IsTrue(level >= 40 && level < 200, $"level {level}");
    }
}

[TestClass]
public sealed class ImageSharpnessTests
{
    [TestMethod]
    public void BlurLowersTheScoreStepByStep()
    {
        using var sharp = Pages.Text(600, 400);
        var previous = ImageSharpness.Measure(sharp);
        foreach (var radius in new[] { 0.8f, 1.5f, 3f, 6f })
        {
            using var blurred = new BlurFilter(radius).Apply(sharp, OperationContext.Default);
            var score = ImageSharpness.Measure(blurred);
            Assert.IsLessThan(previous, score, $"radius {radius}: {score} after {previous}");
            previous = score;
        }
        using var soft = new BlurFilter(6).Apply(sharp, OperationContext.Default);
        Assert.IsGreaterThan(100 * ImageSharpness.Measure(soft), ImageSharpness.Measure(sharp), "Heavy blur takes away nearly all of the score.");
    }

    [TestMethod]
    public void APlainImageScoresZero()
    {
        using var plain = ImageBuffer.Create(300, 200, new Rgba32(120, 80, 40));
        Assert.AreEqual(0, ImageSharpness.Measure(plain));
        using var tiny = ImageBuffer.Create(2, 2, Rgba32.White);
        Assert.AreEqual(0, ImageSharpness.Measure(tiny));
    }

    [TestMethod]
    public void ALargeBlockAveragesWithoutOverflowing()
    {
        // 250 x 250 white pixels a block sum to about 4e9, past an int: the average must still be white.
        using var white = ImageBuffer.Create(4000, 2000, Rgba32.White);
        var plane = LuminancePlane.Create(white, new Rectangle(0, 0, white.Width, white.Height), 16, CancellationToken.None);
        Assert.AreEqual(250, plane.Factor);
        Assert.IsTrue(plane.Values.All(v => v == 255), "Every block of a white image is white.");
    }

    [TestMethod]
    public void APhotoAndItsReductionToTheAnalysisSizeScoreAlike()
    {
        using var large = Pages.Text(1024, 768);
        using var reduced = new ResizeOperation(512, 384, Geometry.ResizeMode.Stretch, Geometry.ResamplingFilter.Box).Apply(large, OperationContext.Default);
        var a = ImageSharpness.Measure(large);
        var b = ImageSharpness.Measure(reduced);
        Assert.AreEqual(1, a / b, 0.05, $"{a} against {b}");
    }

    [TestMethod]
    public void ARegionIsMeasuredOnItsOwn()
    {
        using var image = Pages.Text(400, 300);
        using var half = ImagePipeline.Create().Blur(4, new Rectangle(200, 0, 200, 300)).Execute(image);
        Assert.IsGreaterThan(ImageSharpness.Measure(half, new Rectangle(200, 0, 200, 300)) * 3, ImageSharpness.Measure(half, new Rectangle(0, 0, 200, 300)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImageSharpness.Measure(image, analysisSize: 8));
    }
}
