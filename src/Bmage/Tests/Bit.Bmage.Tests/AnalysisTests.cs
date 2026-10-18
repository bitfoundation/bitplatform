using System.Drawing;
using Bit.Bmage.Analysis;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class HistogramTests
{
    [TestMethod]
    public void MeasuresASolidColourExactly()
    {
        using var image = ImageBuffer.Create(64, 64, new Rgba32(100, 150, 200));
        var stats = ImageStatistics.Measure(image, maxSamples: 0);

        Assert.AreEqual(100, stats.Red.Minimum);
        Assert.AreEqual(100, stats.Red.Maximum);
        Assert.AreEqual(150, stats.Green.Median);
        Assert.AreEqual(200, stats.Blue.Mean, 0.5e-1);
        Assert.AreEqual(new Rgba32(100, 150, 200), stats.AverageColor);
        Assert.IsFalse(stats.HasTransparency);
        Assert.IsFalse(stats.IsGrayscale);
    }

    [TestMethod]
    public void DetectsGrayscale()
    {
        using var image = ImageBuffer.Create(32, 32, clear: false);
        for (var y = 0; y < 32; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < 32; x++)
            {
                var v = (byte)(x * 8);
                row[x] = new Rgba32(v, v, v);
            }
        }
        Assert.IsTrue(ImageStatistics.Measure(image, maxSamples: 0).IsGrayscale);
    }

    [TestMethod]
    public void PercentilesIgnoreOutliers()
    {
        // Almost everything is mid grey, with one white and one black pixel.
        using var image = ImageBuffer.Create(100, 100, new Rgba32(128, 128, 128));
        image[0, 0] = Rgba32.Black;
        image[99, 99] = Rgba32.White;

        var stats = ImageStatistics.Measure(image, maxSamples: 0);
        Assert.AreEqual(0, stats.Luminance.Minimum);
        Assert.AreEqual(255, stats.Luminance.Maximum);
        // Clipping 1% at each end must exclude the two outliers.
        Assert.AreEqual(128, stats.Luminance.GetPercentile(0.01));
        Assert.AreEqual(128, stats.Luminance.GetPercentile(0.99));
    }

    [TestMethod]
    public void ExcludesFullyTransparentPixelsFromColourStatistics()
    {
        using var image = ImageBuffer.Create(64, 64, Rgba32.Transparent);
        image.Fill(new Rectangle(0, 0, 32, 64), new Rgba32(200, 40, 40));

        var stats = ImageStatistics.Measure(image, maxSamples: 0);
        Assert.IsTrue(stats.HasTransparency);
        // The average must be the red, not red diluted with transparent black.
        Assert.IsTrue(stats.AverageColor.R > 190 && stats.AverageColor.G < 60, $"Average was {stats.AverageColor}");
        Assert.AreEqual(stats.TotalPixelCount / 2, stats.OpaquePixelCount);
    }

    [TestMethod]
    public void SamplingAgreesWithAFullPass()
    {
        using var image = TestImages.Gradient(600, 400);
        var full = ImageStatistics.Measure(image, maxSamples: 0);
        var sampled = ImageStatistics.Measure(image, maxSamples: 5000);
        // Sampling trades a little accuracy for speed; a fraction of a level is plenty for auto adjustments.
        Assert.IsTrue(Math.Abs(full.Luminance.Mean - sampled.Luminance.Mean) < 1.5,
            $"Sampled mean {sampled.Luminance.Mean:0.###} differed too much from {full.Luminance.Mean:0.###}");
        Assert.IsTrue(Math.Abs(full.AverageColor.R - sampled.AverageColor.R) <= 3);
    }

    [TestMethod]
    public void FindsDominantColours()
    {
        using var image = ImageBuffer.Create(100, 100, clear: false);
        // 60% blue, 30% red, 10% green.
        image.Fill(new Rectangle(0, 0, 100, 60), new Rgba32(20, 40, 200));
        image.Fill(new Rectangle(0, 60, 100, 30), new Rgba32(200, 30, 30));
        image.Fill(new Rectangle(0, 90, 100, 10), new Rgba32(30, 180, 60));

        var colors = ImageStatistics.GetDominantColors(image, count: 3, maxSamples: 0);
        Assert.AreEqual(3, colors.Count);
        Assert.IsTrue(colors[0].Color.B > 150, $"Most common should be blue, got {colors[0].Color}");
        Assert.IsInRange(0.5, 0.7, colors[0].Fraction);
        Assert.IsTrue(colors[1].Color.R > 150, $"Second should be red, got {colors[1].Color}");
    }

    [TestMethod]
    public void CanIgnoreNearWhiteBackgrounds()
    {
        // A product photo: mostly white, with a small teal subject.
        using var image = ImageBuffer.Create(100, 100, Rgba32.White);
        image.Fill(new Rectangle(40, 40, 20, 20), new Rgba32(10, 130, 130));

        var withWhite = ImageStatistics.GetDominantColors(image, 1, maxSamples: 0);
        Assert.IsTrue(withWhite[0].Color.R > 240, "White should dominate when it is included");

        var withoutWhite = ImageStatistics.GetDominantColors(image, 1, maxSamples: 0, ignoreNearWhiteAndBlack: true);
        Assert.IsTrue(withoutWhite[0].Color.G > 100 && withoutWhite[0].Color.R < 60, $"Expected the teal subject, got {withoutWhite[0].Color}");
    }
}

[TestClass]
public sealed class AutoAdjustmentTests
{
    /// <summary>Builds a low-contrast image occupying only the middle of the tonal range.</summary>
    private static ImageBuffer LowContrast(int width = 128, int height = 64)
    {
        var image = ImageBuffer.Create(width, height, clear: false);
        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var v = (byte)(96 + x * 64 / Math.Max(1, width - 1));   // 96..160 only
                row[x] = new Rgba32(v, v, v);
            }
        }
        return image;
    }

    [TestMethod]
    public void AutoLevelsExpandsTheTonalRange()
    {
        using var source = LowContrast();
        var before = ImageStatistics.Measure(source, maxSamples: 0).Luminance;
        Assert.IsInRange(50, 80, before.Maximum - before.Minimum);

        using var result = new AutoLevelsOperation().Apply(source, OperationContext.Default);
        var after = ImageStatistics.Measure(result, maxSamples: 0).Luminance;

        Assert.IsTrue(after.Minimum < 12, $"Blacks were not deepened: {after.Minimum}");
        Assert.IsTrue(after.Maximum > 243, $"Whites were not brightened: {after.Maximum}");
    }

    [TestMethod]
    public void AutoLevelsStrengthScalesTheEffect()
    {
        using var source = LowContrast();
        using var half = new AutoLevelsOperation(strength: 0.5f).Apply(source, OperationContext.Default);
        using var full = new AutoLevelsOperation(strength: 1f).Apply(source, OperationContext.Default);

        var original = ImageStatistics.Measure(source, maxSamples: 0).Luminance;
        var halfStats = ImageStatistics.Measure(half, maxSamples: 0).Luminance;
        var fullStats = ImageStatistics.Measure(full, maxSamples: 0).Luminance;

        var originalRange = original.Maximum - original.Minimum;
        var halfRange = halfStats.Maximum - halfStats.Minimum;
        var fullRange = fullStats.Maximum - fullStats.Minimum;
        Assert.IsTrue(halfRange > originalRange && halfRange < fullRange, $"{originalRange} -> {halfRange} -> {fullRange}");
    }

    [TestMethod]
    public void AutoLevelsLeavesAFullRangeImageAlone()
    {
        using var source = TestImages.Gradient(128, 64);
        using var result = new AutoLevelsOperation().Apply(source, OperationContext.Default);
        // A gradient already spans the range, so the change should be small.
        Assert.IsTrue(TestImages.MeanChannelDifference(source, result) < 6);
    }

    [TestMethod]
    public void AutoLevelsDoesNotExplodeOnAFlatImage()
    {
        using var source = ImageBuffer.Create(32, 32, new Rgba32(128, 128, 128));
        using var result = new AutoLevelsOperation().Apply(source, OperationContext.Default);
        // A single-valued histogram has no range to stretch; the result must stay sane rather than becoming noise.
        Assert.AreEqual(32, result.Width);
        var stats = ImageStatistics.Measure(result, maxSamples: 0);
        Assert.AreEqual(stats.Luminance.Minimum, stats.Luminance.Maximum);
    }

    [TestMethod]
    public void PerChannelAutoLevelsNeutralisesAColourCast()
    {
        // A blue-cast image: the red channel never reaches white.
        using var source = ImageBuffer.Create(128, 32, clear: false);
        for (var y = 0; y < 32; y++)
        {
            var row = source.GetRow(y);
            for (var x = 0; x < 128; x++)
            {
                var v = (byte)(x * 2);
                row[x] = new Rgba32((byte)(v * 0.5), (byte)(v * 0.7), v);
            }
        }
        using var result = new AutoLevelsOperation(perChannel: true).Apply(source, OperationContext.Default);
        var stats = ImageStatistics.Measure(result, maxSamples: 0);
        // After a per-channel stretch every channel should reach near white.
        Assert.IsTrue(stats.Red.Maximum > 240, $"Red only reached {stats.Red.Maximum}");
        Assert.IsTrue(stats.Green.Maximum > 240, $"Green only reached {stats.Green.Maximum}");
    }

    [TestMethod]
    public void AutoContrastBrightensADarkImage()
    {
        using var source = ImageBuffer.Create(64, 64, new Rgba32(50, 50, 50));
        using var result = new AutoContrastOperation(targetMean: 0.5f).Apply(source, OperationContext.Default);
        var after = ImageStatistics.Measure(result, maxSamples: 0).Luminance.Mean;
        Assert.IsTrue(after > 80, $"Mean only reached {after:0}, which is not brighter");
    }

    [TestMethod]
    public void AutoContrastDarkensABrightImage()
    {
        using var source = ImageBuffer.Create(64, 64, new Rgba32(220, 220, 220));
        using var result = new AutoContrastOperation(targetMean: 0.5f).Apply(source, OperationContext.Default);
        var after = ImageStatistics.Measure(result, maxSamples: 0).Luminance.Mean;
        Assert.IsTrue(after < 200, $"Mean stayed at {after:0}, which is not darker");
    }

    [TestMethod]
    public void AutoContrastReachesTheTargetAtFullStrength()
    {
        using var source = ImageBuffer.Create(64, 64, new Rgba32(50, 50, 50));
        using var result = new AutoContrastOperation(targetMean: 0.5f, strength: 1f).Apply(source, OperationContext.Default);
        var after = ImageStatistics.Measure(result, maxSamples: 0).Luminance.Mean / 255.0;
        Assert.IsTrue(Math.Abs(after - 0.5) < 0.05, $"Mean reached {after:0.###}, expected about 0.5");
    }

    [TestMethod]
    public void ZeroStrengthIsIdentity()
    {
        Assert.IsTrue(new AutoLevelsOperation(strength: 0).IsIdentity);
        Assert.IsTrue(new AutoContrastOperation(strength: 0).IsIdentity);
    }

    [TestMethod]
    public void RejectsInvalidClipFractions()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AutoLevelsOperation(clipFraction: -0.1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AutoLevelsOperation(clipFraction: 0.5));
    }
}

[TestClass]
public sealed class SmartCropTests
{
    /// <summary>An image that is mostly flat background with one detailed, saturated region.</summary>
    private static ImageBuffer WithSubjectAt(Rectangle subject, int width = 400, int height = 400)
    {
        var image = ImageBuffer.Create(width, height, new Rgba32(210, 210, 208));
        var rng = new Random(4);
        for (var y = subject.Top; y < subject.Bottom; y++)
        {
            var row = image.GetRow(y);
            for (var x = subject.Left; x < subject.Right; x++)
                row[x] = new Rgba32((byte)rng.Next(180, 255), (byte)rng.Next(0, 60), (byte)rng.Next(0, 60));
        }
        return image;
    }

    [TestMethod]
    public void FindsTheSubjectRatherThanTheCentre()
    {
        // Subject in the top-left corner; a centre crop would miss most of it.
        var subject = new Rectangle(20, 20, 120, 120);
        using var image = WithSubjectAt(subject);

        var result = SmartCrop.FindCrop(image, AspectRatio.Square);
        var centre = new Rectangle((400 - 400) / 2, 0, 400, 400);
        _ = centre;

        // The chosen crop must contain most of the subject.
        var overlap = Rectangle.Intersect(result.Rectangle, subject);
        var covered = (double)overlap.Width * overlap.Height / (subject.Width * subject.Height);
        Assert.IsTrue(covered > 0.9, $"Only {covered:P0} of the subject was kept; crop was {result.Rectangle}");
    }

    [TestMethod]
    public void ProducesTheRequestedAspectRatio()
    {
        using var image = WithSubjectAt(new Rectangle(150, 150, 100, 100), 800, 400);
        foreach (var ratio in new[] { AspectRatio.Square, AspectRatio.Ratio16x9, AspectRatio.Ratio3x4 })
        {
            var result = SmartCrop.FindCrop(image, ratio);
            var actual = result.Rectangle.Width / (double)result.Rectangle.Height;
            Assert.IsTrue(Math.Abs(actual - ratio.Value) < 0.02, $"Wanted {ratio}, got {actual:0.###}");
            Assert.IsTrue(image.Bounds.Contains(result.Rectangle), $"Crop {result.Rectangle} escaped the image");
        }
    }

    [TestMethod]
    public void PrefersASubjectOnTheRightWhenThatIsWhereItIs()
    {
        using var left = WithSubjectAt(new Rectangle(20, 140, 120, 120), 600, 400);
        using var right = WithSubjectAt(new Rectangle(460, 140, 120, 120), 600, 400);

        var leftCrop = SmartCrop.FindCrop(left, AspectRatio.Square);
        var rightCrop = SmartCrop.FindCrop(right, AspectRatio.Square);

        Assert.IsTrue(leftCrop.Rectangle.X < rightCrop.Rectangle.X,
            $"Crops did not follow the subject: left={leftCrop.Rectangle.X} right={rightCrop.Rectangle.X}");
    }

    [TestMethod]
    public void HonoursARequiredRegion()
    {
        // Detail on the left, but we insist on including a region on the right.
        using var image = WithSubjectAt(new Rectangle(20, 140, 120, 120), 600, 400);
        var required = new Rectangle(480, 160, 60, 60);

        var result = SmartCrop.FindCrop(image, AspectRatio.Square, required);
        Assert.IsTrue(result.Rectangle.Contains(required), $"Crop {result.Rectangle} did not contain {required}");
    }

    [TestMethod]
    public void ReturnsTheWholeImageWhenItAlreadyMatches()
    {
        using var image = WithSubjectAt(new Rectangle(100, 100, 50, 50), 300, 300);
        var result = SmartCrop.FindCrop(image, AspectRatio.Square);
        Assert.AreEqual(image.Bounds, result.Rectangle);
    }

    [TestMethod]
    public void OperationCropsAndReportsItsSize()
    {
        using var image = WithSubjectAt(new Rectangle(30, 30, 80, 80), 400, 300);
        var operation = new SmartCropOperation(AspectRatio.Square);
        var expected = operation.GetOutputSize(image.Size);
        using var result = operation.Apply(image, OperationContext.Default);
        Assert.AreEqual(expected, result.Size);
        Assert.AreEqual(300, result.Width);
    }

    /// <summary>
    /// Regression: a neutral grey sits at the centre of the normalised red/green plane, so a loose skin-tone test
    /// classifies every grey wall as a face and the crop chases the background instead of the subject.
    /// </summary>
    [TestMethod]
    public void NeutralBackgroundsAreNotMistakenForSkin()
    {
        // A wide image with a vivid subject at the far left on a pale neutral background. Centre-cropping keeps none
        // of the subject, so anything that scores the background highly will fail this outright.
        var subject = new Rectangle(40, 180, 200, 240);
        using var image = WithSubjectAt(subject, 1200, 600);

        var result = SmartCrop.FindCrop(image, AspectRatio.Square);
        var overlap = Rectangle.Intersect(result.Rectangle, subject);
        var covered = (double)overlap.Width * overlap.Height / (subject.Width * subject.Height);
        Assert.IsTrue(covered > 0.95, $"Only {covered:P0} of the subject survived; crop was {result.Rectangle}");

        // And confirm the centre crop really would have missed it, so the test is measuring something.
        var centre = new Rectangle((1200 - 600) / 2, 0, 600, 600);
        Assert.IsTrue(Rectangle.Intersect(centre, subject).IsEmpty, "The test image no longer distinguishes the two");
    }

    [TestMethod]
    public void IsDeterministic()
    {
        using var image = WithSubjectAt(new Rectangle(60, 200, 100, 100), 500, 500);
        var a = SmartCrop.FindCrop(image, AspectRatio.Ratio4x3);
        var b = SmartCrop.FindCrop(image, AspectRatio.Ratio4x3);
        Assert.AreEqual(a.Rectangle, b.Rectangle);
    }
}

[TestClass]
public sealed class PlaceholderTests
{
    [TestMethod]
    public void CapturesTheDominantColours()
    {
        using var image = ImageBuffer.Create(200, 200, clear: false);
        image.Fill(new Rectangle(0, 0, 200, 100), new Rgba32(200, 30, 30));
        image.Fill(new Rectangle(0, 100, 200, 100), new Rgba32(30, 30, 200));

        var placeholder = ImagePlaceholder.Create(image, 2, 2);
        Assert.AreEqual(4, placeholder.Colors.Count);
        Assert.IsTrue(placeholder.Colors[0].R > 180, $"Top-left should be red, got {placeholder.Colors[0]}");
        Assert.IsTrue(placeholder.Colors[3].B > 180, $"Bottom-right should be blue, got {placeholder.Colors[3]}");
        Assert.AreEqual(200, placeholder.SourceWidth);
        Assert.AreEqual(1.0, placeholder.AspectRatio, 0.5e-3);
    }

    [TestMethod]
    public void RoundTripsThroughItsCompactString()
    {
        using var image = TestImages.Gradient(160, 120);
        var original = ImagePlaceholder.Create(image, 4, 4);
        var text = original.ToCompactString();

        // Small enough to inline anywhere.
        Assert.IsTrue(text.Length < 80, $"Placeholder string was {text.Length} characters");

        var restored = ImagePlaceholder.Parse(text, 160, 120);
        Assert.IsNotNull(restored);
        Assert.AreEqual(original.Columns, restored!.Columns);
        Assert.AreEqual(original.Rows, restored.Rows);
        for (var i = 0; i < original.Colors.Count; i++)
            Assert.AreEqual(original.Colors[i], restored.Colors[i]);
    }

    [TestMethod]
    public void RejectsMalformedStrings()
    {
        Assert.IsNull(ImagePlaceholder.Parse(null));
        Assert.IsNull(ImagePlaceholder.Parse(""));
        Assert.IsNull(ImagePlaceholder.Parse("!!!not base64!!!"));
        Assert.IsNull(ImagePlaceholder.Parse("AAAA"));
    }

    [TestMethod]
    public void RendersASmoothApproximation()
    {
        using var image = ImageBuffer.Create(200, 200, clear: false);
        image.Fill(new Rectangle(0, 0, 200, 100), new Rgba32(220, 20, 20));
        image.Fill(new Rectangle(0, 100, 200, 100), new Rgba32(20, 20, 220));

        var placeholder = ImagePlaceholder.Create(image, 4, 4);
        using var rendered = placeholder.Render(200, 200);

        Assert.AreEqual(200, rendered.Width);
        Assert.IsTrue(rendered[100, 20].R > 150, $"Top should stay red: {rendered[100, 20]}");
        Assert.IsTrue(rendered[100, 180].B > 150, $"Bottom should stay blue: {rendered[100, 180]}");
        // The boundary must be blended rather than a hard edge.
        var middle = rendered[100, 100];
        Assert.IsTrue(middle.R is > 40 and < 220, $"Boundary was not blended: {middle}");
    }

    [TestMethod]
    public void ProducesUsableCss()
    {
        using var image = TestImages.Gradient(64, 64);
        var css = ImagePlaceholder.Create(image, 3, 3).ToCssBackground();
        Assert.Contains("background-color:#", css, StringComparison.Ordinal);
        Assert.Contains("radial-gradient", css, StringComparison.Ordinal);
        // Invariant formatting: a comma decimal separator would break the CSS.
        Assert.DoesNotContain(",5%", css, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsInvalidGridSizes()
    {
        using var image = TestImages.Gradient(32, 32);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImagePlaceholder.Create(image, 0, 4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImagePlaceholder.Create(image, 4, 20));
    }
}
