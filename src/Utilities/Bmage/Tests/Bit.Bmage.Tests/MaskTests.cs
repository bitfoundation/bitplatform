using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

/// <summary>
/// Masks restrict an edit to part of an image. The contract: where a mask is 0 the pixels are the original's, where it
/// is 255 they are exactly what the edit alone produces, and in between they mix by the coverage.
/// </summary>
[TestClass]
public sealed class MaskTests
{
    // ------------------------------------------------------------------ rendering shapes

    [TestMethod]
    public void ARectangleCoversItsPixelsAndNothingElse()
    {
        using var mask = new BmageRectangleMask(new RectangleF(10, 20, 30, 40)).Render(new Size(100, 100));

        Assert.AreEqual(255, mask[10, 20]);
        Assert.AreEqual(255, mask[39, 59]);
        Assert.AreEqual(0, mask[9, 20]);
        Assert.AreEqual(0, mask[40, 59]);
        Assert.AreEqual(new Rectangle(10, 20, 30, 40), mask.GetBounds());
    }

    [TestMethod]
    public void RelativeUnitsFitEveryImage()
    {
        var circle = BmageMask.Ellipse();
        foreach (var size in new[] { new Size(100, 100), new Size(400, 200), new Size(31, 77) })
        {
            using var rendered = circle.Render(size);
            Assert.AreEqual(255, rendered[size.Width / 2, size.Height / 2], $"centre of {size}");
            Assert.AreEqual(0, rendered[0, 0], $"corner of {size}");
            Assert.AreEqual(0, rendered[size.Width - 1, size.Height - 1], $"corner of {size}");
        }
    }

    [TestMethod]
    public void InvertingSwapsCoveredAndUncovered()
    {
        var rectangle = new BmageRectangleMask(new RectangleF(0, 0, 50, 100));
        using var plain = rectangle.Render(new Size(100, 100));
        using var inverted = rectangle.Invert().Render(new Size(100, 100));

        for (var i = 0; i < plain.Values.Length; i++) Assert.AreEqual(255 - plain.Values[i], inverted.Values[i]);
    }

    [TestMethod]
    public void OpacityScalesTheCoverage()
    {
        using var half = (BmageMask.All() with { Opacity = 0.5f }).Render(new Size(8, 8));
        Assert.AreEqual(128, half[3, 3]);

        var none = BmageMask.All() with { Opacity = 0 };
        Assert.IsTrue(none.IsEmpty);
        using var rendered = none.Render(new Size(8, 8));
        Assert.AreEqual(Rectangle.Empty, rendered.GetBounds());
    }

    [TestMethod]
    public void FeatheringSoftensTheEdgeSymmetrically_AndKeepsTheInteriorAndFarExterior()
    {
        var rectangle = new BmageRectangleMask(new RectangleF(50, 0, 100, 40)) { Feather = 6 };
        using var mask = rectangle.Render(new Size(200, 40));

        // Deep inside and far outside are untouched.
        Assert.AreEqual(255, mask[100, 20]);
        Assert.AreEqual(0, mask[5, 20]);
        Assert.AreEqual(0, mask[195, 20]);
        // At the edge, the coverage is about half, and it falls monotonically across the transition.
        Assert.IsTrue(Math.Abs(mask[50, 20] - 128) < 20, $"edge coverage {mask[50, 20]}");
        for (var x = 30; x < 70; x++) Assert.IsLessThanOrEqualTo(mask[x + 1, 20], mask[x, 20] , $"not monotonic at {x}");
        // The feather reaches about three standard deviations, not much further.
        Assert.IsTrue(Rectangle.FromLTRB(50 - 20, 0, 150 + 20, 40).Contains(mask.GetBounds()));
    }

    [TestMethod]
    public void AffectedBoundsCoverEverythingTheRenderedMaskTouches()
    {
        var size = new Size(300, 200);
        foreach (var mask in new BmageMask[]
        {
            new BmageRectangleMask(new RectangleF(40, 30, 50, 60)) { Feather = 8 },
            new BmageEllipseMask(new RectangleF(100, 50, 80, 40)),
            new BmageRadialGradientMask(new Vector2(150, 100), 60, 30) { Rotation = 30 },
            new BmagePolygonMask([new(10, 10), new(80, 20), new(30, 90)]),
            new BmageBrushMask([new BmageBrushStroke([new(20, 20), new(120, 60), new(200, 30)], 14)]) { Feather = 3 },
            new BmageEllipseMask(new RectangleF(10, 10, 40, 40)).Union(new BmageRectangleMask(new RectangleF(200, 100, 50, 50))),
        })
        {
            using var rendered = mask.Render(size);
            var touched = rendered.GetBounds();
            var affected = mask.GetAffectedBounds(size);
            Assert.IsTrue(affected.Contains(touched), $"{mask.Kind}: rendered {touched} escapes the affected bounds {affected}.");
        }
    }

    [TestMethod]
    public void ALinearGradientFadesFromStartToEnd()
    {
        using var mask = new BmageLinearGradientMask(new Vector2(0, 10), new Vector2(0, 90)).Render(new Size(20, 100));

        Assert.AreEqual(255, mask[5, 5]);
        Assert.AreEqual(0, mask[5, 95]);
        Assert.IsTrue(Math.Abs(mask[5, 50] - 128) <= 3, $"midpoint {mask[5, 50]}");
        for (var y = 10; y < 90; y++) Assert.IsLessThanOrEqualTo(mask[5, y - 1], mask[5, y]);
    }

    [TestMethod]
    public void ARadialGradientIsSolidInsideItsHardnessAndEmptyBeyondItsRadius()
    {
        using var mask = new BmageRadialGradientMask(new Vector2(50, 50), 40, 40) { Hardness = 0.5f }.Render(new Size(100, 100));

        Assert.AreEqual(255, mask[50, 50]);
        Assert.AreEqual(255, mask[50 + 18, 50]);
        Assert.AreEqual(0, mask[50 + 41, 50]);
        Assert.IsTrue(mask[50 + 30, 50] is > 0 and < 255);
    }

    [TestMethod]
    public void BrushStrokesPaintAndEraseInOrder()
    {
        var brush = new BmageBrushMask([new BmageBrushStroke([new(10, 50), new(90, 50)], 20)])
            .Add(new BmageBrushStroke([new(50, 30), new(50, 70)], 10) { Erase = true });
        using var mask = brush.Render(new Size(100, 100));

        Assert.AreEqual(255, mask[20, 50]);
        Assert.AreEqual(255, mask[80, 50]);
        Assert.AreEqual(0, mask[50, 50], "The erasing stroke did not remove what was painted under it.");
        Assert.AreEqual(0, mask[20, 80]);
    }

    [TestMethod]
    public void CombiningMasksFollowsFuzzySetRules()
    {
        var left = new BmageRectangleMask(new RectangleF(0, 0, 60, 10));
        var right = new BmageRectangleMask(new RectangleF(40, 0, 60, 10));
        var size = new Size(100, 10);

        using var union = left.Union(right).Render(size);
        using var intersection = left.Intersect(right).Render(size);
        using var difference = left.Subtract(right).Render(size);
        using var exclusive = left.Exclude(right).Render(size);

        Assert.AreEqual(new Rectangle(0, 0, 100, 10), union.GetBounds());
        Assert.AreEqual(new Rectangle(40, 0, 20, 10), intersection.GetBounds());
        Assert.AreEqual(new Rectangle(0, 0, 40, 10), difference.GetBounds());
        Assert.AreEqual(255, exclusive[10, 5]);
        Assert.AreEqual(0, exclusive[50, 5]);
        Assert.AreEqual(255, exclusive[90, 5]);
    }

    [TestMethod]
    public void ALuminanceRangeSelectsByBrightness()
    {
        using var image = BmageImageBuffer.Create(256, 1);
        for (var x = 0; x < 256; x++) image[x, 0] = new BmageRgba32((byte)x, (byte)x, (byte)x);

        using var highlights = new BmageLuminanceRangeMask(0.75f, 1f) { Softness = 0.1f }.Render(image.Size, image);

        Assert.AreEqual(255, highlights[250, 0]);
        Assert.AreEqual(255, highlights[192, 0]);
        Assert.AreEqual(0, highlights[100, 0]);
        Assert.IsTrue(highlights[180, 0] is > 0 and < 255, $"{highlights[180, 0]}");
    }

    [TestMethod]
    public void AColourRangeSelectsSimilarColours()
    {
        using var image = TestImages.Quadrants(20, 20);
        using var reds = new BmageColorRangeMask(new BmageRgba32(250, 10, 10)).Render(image.Size, image);

        Assert.AreEqual(255, reds[2, 2]);
        Assert.AreEqual(0, reds[15, 2]);
        Assert.AreEqual(0, reds[2, 15]);
        Assert.AreEqual(0, reds[15, 15]);
    }

    [TestMethod]
    public void ContentMasksNeedTheImage()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new BmageLuminanceRangeMask(0, 0.5f).Render(new Size(4, 4)));
    }

    [TestMethod]
    public void ABitmapMaskIsStretchedOverTheImage()
    {
        // A 2x1 matte: left half covered, right half not.
        using var matte = BmageImageBuffer.Create(2, 1);
        matte[0, 0] = new BmageRgba32(0, 0, 0, 255);
        matte[1, 0] = new BmageRgba32(0, 0, 0, 0);

        using var mask = new BmageBitmapMask(matte).Render(new Size(200, 100));

        Assert.AreEqual(255, mask[10, 50]);
        Assert.AreEqual(0, mask[190, 50]);
        Assert.IsTrue(mask[100, 50] is > 64 and < 192);
    }

    [TestMethod]
    public void PixelMasksScaleWithTheEdit_RelativeOnesDoNot()
    {
        var pixels = new BmageRectangleMask(new RectangleF(100, 50, 200, 100)) { Feather = 10 };
        var scaled = (BmageRectangleMask)pixels.ForScale(0.5);
        Assert.AreEqual(new RectangleF(50, 25, 100, 50), scaled.Rectangle);
        Assert.AreEqual(5f, scaled.Feather);

        var relative = BmageMask.Ellipse() with { Feather = 4 };
        var rescaled = (BmageEllipseMask)relative.ForScale(0.25);
        Assert.AreEqual(relative.Rectangle, rescaled.Rectangle);
        Assert.AreEqual(1f, rescaled.Feather);

        var combined = (BmageCombinedMask)pixels.Union(relative).ForScale(0.5);
        Assert.AreEqual(new RectangleF(50, 25, 100, 50), ((BmageRectangleMask)combined.First).Rectangle);
    }

    // ------------------------------------------------------------------ masked operations

    [TestMethod]
    public void AMaskedAdjustmentChangesOnlyTheCoveredPixels()
    {
        using var source = ParallelProcessingTests.Noise(120, 80, seed: 4);
        var region = new RectangleF(20, 10, 50, 40);
        using var full = new BmageBrightnessAdjustment(0.3f).Apply(source, BmageOperationContext.Default);
        using var masked = new BmageMaskedOperation(new BmageBrightnessAdjustment(0.3f), new BmageRectangleMask(region)).Apply(source, BmageOperationContext.Default);

        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var expected = region.Contains(x + 0.5f, y + 0.5f) ? full[x, y] : source[x, y];
                Assert.AreEqual(expected, masked[x, y], $"pixel ({x},{y})");
            }
    }

    [TestMethod]
    public void AFullyCoveringMaskReproducesTheOperationExactly()
    {
        using var source = ParallelProcessingTests.Noise(200, 150, seed: 8);
        foreach (var operation in new IBmageImageOperation[]
        {
            new BmageBrightnessAdjustment(0.2f),
            new BmageSaturationAdjustment(-0.5f),
            new BmageFusedPointOperation([new BmageContrastAdjustment(0.3f), new BmageHueAdjustment(40)]),
            new BmageBlurFilter(3),
            new BmageSharpenFilter(0.8f),
            new BmagePipelineOperation(BmageImagePipeline.Create().Exposure(0.5f).Vibrance(0.4f)),
        })
        {
            using var expected = operation.Apply(source, BmageOperationContext.Default);
            using var actual = new BmageMaskedOperation(operation, BmageMask.All()).Apply(source, BmageOperationContext.Default);
            Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes), $"{operation.Name} through a full mask differs from {operation.Name} alone.");
        }
    }

    [TestMethod]
    public void AnEmptyMaskLeavesTheImageAlone()
    {
        using var source = ParallelProcessingTests.Noise(64, 64, seed: 2);
        var nothing = new BmageRectangleMask(new RectangleF(500, 500, 10, 10));
        foreach (var operation in new IBmageImageOperation[] { new BmageBrightnessAdjustment(0.5f), new BmageBlurFilter(4) })
        {
            using var result = new BmageMaskedOperation(operation, nothing).Apply(source, BmageOperationContext.Default);
            Assert.IsTrue(source.Bytes.SequenceEqual(result.Bytes), operation.Name);
        }
        Assert.IsTrue(new BmageMaskedOperation(new BmageBrightnessAdjustment(0.5f), BmageMask.All() with { Opacity = 0 }).IsIdentity);
    }

    [TestMethod]
    public void PartialCoverageMixesTheOriginalAndTheEdit()
    {
        using var source = BmageImageBuffer.Create(4, 4, new BmageRgba32(100, 100, 100));
        using var result = new BmageMaskedOperation(new BmageInvertFilter(), BmageMask.All() with { Opacity = 0.5f }).Apply(source, BmageOperationContext.Default);

        // Inverting 100 gives 155; half way between is 127.5, which rounds to 128 with a coverage of 128/255.
        var p = result[1, 1];
        Assert.IsTrue(Math.Abs(p.R - 128) <= 1, $"{p}");
        Assert.AreEqual(255, p.A);
    }

    [TestMethod]
    public void TheInPlacePathMatchesTheCopyingPath()
    {
        var operation = new BmageMaskedOperation(new BmageContrastAdjustment(0.4f), new BmageEllipseMask(new RectangleF(10, 10, 80, 50)) { Feather = 5 });
        using var source = ParallelProcessingTests.Noise(100, 70, seed: 6);
        using var copy = source.Clone();
        using var copied = operation.Apply(source, BmageOperationContext.Default);
        var inPlace = operation.Apply(copy, BmageOperationContext.Default.WithMutation(true));

        Assert.AreSame(copy, inPlace);
        Assert.IsTrue(copied.Bytes.SequenceEqual(inPlace.Bytes));
    }

    [TestMethod]
    public void MaskingNeverModifiesTheSourceWithoutPermission()
    {
        using var source = ParallelProcessingTests.Noise(80, 60, seed: 3);
        var before = source.ToArray();
        foreach (var operation in new IBmageImageOperation[]
        {
            new BmageMaskedOperation(new BmageBrightnessAdjustment(0.3f), BmageMask.Ellipse()),
            new BmageMaskedOperation(new BmageBlurFilter(2), BmageMask.Ellipse()),
            new BmageAlphaMaskOperation(BmageMask.Ellipse()),
        })
        {
            using var result = operation.Apply(source, BmageOperationContext.Default);
            Assert.IsTrue(before.AsSpan().SequenceEqual(source.Bytes), $"{operation.Name} modified its source.");
        }
    }

    [TestMethod]
    public void OperationsThatChangeTheSizeAreRefused()
    {
        var masked = new BmageMaskedOperation(new BmageCropOperation(0, 0, 10, 10), BmageMask.All());
        Assert.ThrowsExactly<ArgumentException>(() => masked.GetOutputSize(new Size(100, 100)));
        using var source = BmageImageBuffer.Create(100, 100);
        Assert.ThrowsExactly<ArgumentException>(() => masked.Apply(source, BmageOperationContext.Default));
    }

    [TestMethod]
    public void AMaskedPipelineRunsOnManyThreadsWithIdenticalPixels()
    {
        using var source = ParallelProcessingTests.Noise(1024, 768, seed: 12);
        var pipeline = BmageImagePipeline.Create()
            .Masked(BmageMask.Gradient(new(0, 0), new(0, 0.6f)), sky => sky.Exposure(-0.6f).Saturation(0.3f))
            .Masked(new BmageRadialGradientMask(new Vector2(0.5f, 0.6f), 0.3f, 0.25f) { Units = BmageMaskUnits.Relative, Feather = 12 }, p => p.Blur(3));

        using var one = pipeline.Execute(source, new BmagePipelineExecutionOptions { MaxDegreeOfParallelism = 1 });
        using var many = pipeline.Execute(source, new BmagePipelineExecutionOptions { MaxDegreeOfParallelism = 8 });
        Assert.IsTrue(one.Bytes.SequenceEqual(many.Bytes));
    }

    [TestMethod]
    public void AMaskedEditPreviewedOnAProxyMatchesTheExportScaledDown()
    {
        using var source = BmageImageBuffer.Create(400, 300, new BmageRgba32(100, 100, 100));
        var pipeline = BmageImagePipeline.Create().Masked(new BmageRectangleMask(new RectangleF(100, 80, 200, 120)) { Feather = 4 }, p => p.Brightness(0.4f));

        using var full = pipeline.Execute(source);
        using var proxySource = new BmageResizeOperation(200, 150, BmageResizeMode.Stretch, BmageResamplingFilter.Box).Apply(source, BmageOperationContext.Default);
        using var preview = pipeline.ForScale(0.5).Execute(proxySource);

        // The preview is the export at half the size: brightened inside the rectangle, untouched outside it.
        Assert.AreEqual(full[200, 140], preview[100, 70]);
        Assert.AreEqual(full[20, 20], preview[10, 10]);
        Assert.AreNotEqual(full[200, 140], full[20, 20]);
    }

    [TestMethod]
    public void ApplyingAMaskCutsTheImageOut()
    {
        using var source = BmageImageBuffer.Create(64, 64, BmageRgba32.White);
        using var avatar = BmageImagePipeline.Create().ApplyMask(BmageMask.Ellipse()).Execute(source);

        Assert.AreEqual(255, avatar[32, 32].A);
        Assert.AreEqual(0, avatar[0, 0].A);
        Assert.AreEqual(0, avatar[63, 63].A);
        Assert.IsTrue(avatar[32, 0].A > 0, "The ellipse should reach the middle of the top edge.");
    }

    [TestMethod]
    public void MaskedOperationsSurviveTheOptimiserAsBarriers()
    {
        var pipeline = BmageImagePipeline.Create()
            .Brightness(0.1f)
            .Masked(BmageMask.Ellipse(), p => p.Contrast(0.2f))
            .Saturation(0.2f)
            .Masked(BmageMask.Ellipse(), p => p.Brightness(0));

        var optimised = pipeline.Optimize(new Size(100, 100));

        // The identity edit through a mask is dropped; the masked contrast is not fused with its neighbours.
        Assert.AreEqual(3, optimised.Count);
        Assert.IsInstanceOfType<BmageMaskedOperation>(optimised[1]);
    }

    [TestMethod]
    public void TheMaskBufferRoundTripsThroughAnImage()
    {
        using var mask = new BmageEllipseMask(new RectangleF(5, 5, 30, 20)).Render(new Size(40, 30));
        using var image = mask.ToImage();
        using var back = BmageMaskBuffer.FromAlpha(image);

        Assert.IsTrue(mask.Values.SequenceEqual(back.Values));
    }

    [TestMethod]
    public void ResizingAMaskKeepsItsShape()
    {
        using var mask = new BmageRectangleMask(new RectangleF(0, 0, 50, 100)).Render(new Size(100, 100));
        using var larger = mask.Resize(400, 400);
        using var smaller = mask.Resize(10, 10);

        Assert.AreEqual(255, larger[50, 200]);
        Assert.AreEqual(0, larger[350, 200]);
        Assert.AreEqual(255, smaller[1, 5]);
        Assert.AreEqual(0, smaller[8, 5]);
    }
}
