using System.Drawing;

namespace Bit.Bmage.Tests;

/// <summary>
/// Filter and adjustment tests. Region-limited filters are the sharp edge here: they are what redaction is built on,
/// so their behaviour at and beyond the image boundary has to be exact and consistent across every filter.
/// </summary>
[TestClass]
public sealed class AdversarialFilterTests
{
    private static BmageImageBuffer Noise(int w, int h, int seed = 1) => AdversarialGeometryTests.Noise(w, h, seed);

    public static IEnumerable<object[]> RegionFilters() =>
        new[]
        {
            "blur", "sharpen", "pixelate", "noise", "median", "convolution",
        }.Select(kind => new object[] { kind });

    private static BmageRegionOperation CreateRegionFilter(string kind, Rectangle? region) => kind switch
    {
        "blur" => new BmageBlurFilter(3f, region),
        "sharpen" => new BmageSharpenFilter(0.8f, 2f, 0f, region),
        "pixelate" => new BmagePixelateFilter(4, region),
        "noise" => new BmageNoiseFilter(0.3f, false, 7, region),
        "median" => new BmageMedianFilter(2, region),
        "convolution" => BmageConvolutionFilter.EdgeDetect(region),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>
    /// A region that lies entirely outside the image must be a no-op. Every region filter has to agree: an editor
    /// that redacts a rectangle the user dragged off-canvas must not fail on one filter and succeed on another.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void RegionEntirelyOutsideTheImageIsANoOp(string kind)
    {
        foreach (var region in new[]
                 {
                     new Rectangle(1000, 1000, 20, 20),
                     new Rectangle(-100, -100, 20, 20),
                     new Rectangle(64, 0, 10, 10),
                     new Rectangle(0, 48, 10, 10),
                 })
        {
            using var src = Noise(64, 48, 3);
            var expected = src.ToArray();
            var filter = CreateRegionFilter(kind, region);

            BmageImageBuffer? result = null;
            var ex = Capture.Exception(() => result = filter.Apply(src, BmageOperationContext.Default));
            using (result)
            {
                Assert.IsTrue(ex is null, $"{kind} with region {region} threw {ex?.GetType().Name}: {ex?.Message}");
                Assert.AreEqual(src.Size, result!.Size);
                Assert.IsTrue(expected.AsSpan().SequenceEqual(result.Bytes),
                    $"{kind} with the off-image region {region} changed pixels");
            }
        }
    }

    /// <summary>A region filter must not touch a single pixel outside its region.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void RegionFiltersLeaveTheRestOfTheImageUntouched(string kind)
    {
        var region = new Rectangle(16, 12, 24, 20);
        using var src = Noise(64, 48, 5);
        var before = src.Clone();
        using (before)
        {
            using var result = CreateRegionFilter(kind, region).Apply(src, BmageOperationContext.Default);
            for (var y = 0; y < src.Height; y++)
            {
                for (var x = 0; x < src.Width; x++)
                {
                    if (region.Contains(x, y)) continue;
                    Assert.IsTrue(before[x, y] == result[x, y],
                        $"{kind} changed pixel ({x},{y}) which lies outside region {region}");
                }
            }
        }
    }

    /// <summary>A region clipped by the image edge must behave as the intersection, not throw or run away.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void PartiallyOffImageRegionsAreClipped(string kind)
    {
        foreach (var region in new[]
                 {
                     new Rectangle(-10, -10, 30, 30),
                     new Rectangle(50, 40, 40, 40),
                     new Rectangle(-5, 20, 200, 5),
                 })
        {
            using var src = Noise(64, 48, 7);
            var filter = CreateRegionFilter(kind, region);
            BmageImageBuffer? result = null;
            var ex = Capture.Exception(() => result = filter.Apply(src, BmageOperationContext.Default));
            using (result)
            {
                Assert.IsTrue(ex is null, $"{kind} with region {region} threw {ex?.GetType().Name}: {ex?.Message}");
                Assert.AreEqual(src.Size, result!.Size);
            }
        }
    }

    /// <summary>The in-place path must produce the same pixels as the allocating path.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void InPlaceFilteringMatchesTheAllocatingPath(string kind)
    {
        foreach (Rectangle? region in new Rectangle?[] { null, new Rectangle(8, 8, 24, 16) })
        {
            using var a = Noise(48, 36, 11);
            using var b = a.Clone();
            using var expected = CreateRegionFilter(kind, region).Apply(a, BmageOperationContext.Default);
            var mutated = CreateRegionFilter(kind, region).Apply(b, BmageOperationContext.Default.WithMutation(true));
            Assert.IsTrue(expected.Bytes.SequenceEqual(mutated.Bytes),
                $"{kind} (region {region?.ToString() ?? "full"}) differs between the in-place and allocating paths");
        }
    }

    /// <summary>Filters must never modify the source when in-place mutation has not been granted.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void FiltersDoNotModifyTheSourceWithoutPermission(string kind)
    {
        using var src = Noise(48, 36, 13);
        var before = src.ToArray();
        using var result = CreateRegionFilter(kind, null).Apply(src, BmageOperationContext.Default);
        Assert.IsTrue(before.AsSpan().SequenceEqual(src.Bytes), $"{kind} modified its source buffer");
    }

    /// <summary>Filters must survive images too small for their own kernels.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void FiltersHandleTinyImages(string kind)
    {
        foreach (var (w, h) in new[] { (1, 1), (1, 8), (8, 1), (2, 2), (3, 3) })
        {
            using var src = Noise(w, h, w * 7 + h);
            var filter = CreateRegionFilter(kind, null);
            BmageImageBuffer? result = null;
            var ex = Capture.Exception(() => result = filter.Apply(src, BmageOperationContext.Default));
            using (result)
            {
                Assert.IsTrue(ex is null, $"{kind} on a {w}x{h} image threw {ex?.GetType().Name}: {ex?.Message}");
                Assert.AreEqual(src.Size, result!.Size);
            }
        }
    }

    /// <summary>Extreme but legal parameters must not crash or produce nonsense.</summary>
    [TestMethod]
    public void ExtremeFilterParametersAreHandled()
    {
        var filters = new IBmageImageOperation[]
        {
            new BmageBlurFilter(0f),
            new BmageBlurFilter(0.04f),
            new BmageBlurFilter(200f),
            new BmageSharpenFilter(0f, 1f),
            new BmageSharpenFilter(10f, 0.5f, 1f),
            new BmagePixelateFilter(1),
            new BmagePixelateFilter(1000),
            new BmageNoiseFilter(0f),
            new BmageNoiseFilter(1f, true, int.MaxValue),
            new BmageMedianFilter(1),
            new BmageMedianFilter(5),
            new BmageVignetteFilter(1f, 0f, 0f),
            new BmageVignetteFilter(0f, 1f, 1f),
            new BmageVignetteFilter(1f, 2f, 2f),
        };

        using var src = TestImages.Translucent(32, 32);
        foreach (var filter in filters)
        {
            using var result = filter.Apply(src, BmageOperationContext.Default);
            Assert.AreEqual(src.Size, result.Size);
        }
    }

    [TestMethod]
    public void InvalidFilterParametersAreRejectedAtConstruction()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageBlurFilter(-1f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageBlurFilter(float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageBlurFilter(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(-1f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(1f, 0f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(1f, float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageSharpenFilter(1f, float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageMedianFilter(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageMedianFilter(6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmagePosterizeFilter(1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmagePosterizeFilter(257));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageGammaAdjustment(0f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmageGammaAdjustment(-1f));
    }

    [TestMethod]
    public void AnAbsurdBlurRadiusFinishesAtOnce()
    {
        using var src = Noise(600, 400, 5);
        foreach (var filter in new IBmageImageOperation[] { new BmageBlurFilter(1e7f), new BmageSharpenFilter(0.5f, 1e7f), new BmageHighPassFilter(1e7f), new BmageBlurFilter(5e8f, new Rectangle(10, 10, 3, 300)) })
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using var result = filter.Apply(src, BmageOperationContext.Default);
            Assert.IsLessThan(2000, watch.ElapsedMilliseconds, $"{filter.Name} took {watch.ElapsedMilliseconds} ms.");
            Assert.AreEqual(src.Size, result.Size);
            // A blur that did nothing would leave the sharpen nothing to add.
            if (filter is BmageSharpenFilter) Assert.IsGreaterThan(0, TestImages.MaxChannelDifference(src, result), "The sharpen changed no pixel.");
        }
    }

    [TestMethod]
    public void ScalingAnAbsurdBlurRadiusUpStaysFinite()
    {
        var scaled = (BmageBlurFilter)new BmageBlurFilter(float.MaxValue).ForScale(4);
        Assert.AreEqual(float.MaxValue, scaled.Radius);
    }

    /// <summary>Past about a billion pixels the box radii overflowed and the blur did nothing at all.</summary>
    [TestMethod]
    public void ABlurRadiusTooWideForTheBoxesStillBlurs()
    {
        using var src = Noise(60, 40, 7);
        using var widest = new BmageBlurFilter(1e8f).Apply(src, BmageOperationContext.Default);
        Assert.IsGreaterThan(10, TestImages.MaxChannelDifference(src, widest));
        foreach (var radius in new[] { 2e9f, 1e20f, float.MaxValue })
        {
            using var result = new BmageBlurFilter(radius).Apply(src, BmageOperationContext.Default);
            Assert.AreEqual(0, TestImages.MaxChannelDifference(widest, result), $"radius {radius}");
        }
    }

    /// <summary>A blur must preserve the average colour of a flat region and must not tint it.</summary>
    [TestMethod]
    public void BlurPreservesFlatColour()
    {
        var colour = new BmageRgba32(90, 140, 200);
        using var src = BmageImageBuffer.Create(64, 64, colour);
        using var result = new BmageBlurFilter(8f).Apply(src, BmageOperationContext.Default);
        foreach (var p in result.Pixels.ToArray())
        {
            Assert.IsTrue(Math.Abs(p.R - colour.R) <= 1 && Math.Abs(p.G - colour.G) <= 1 && Math.Abs(p.B - colour.B) <= 1 && p.A == 255,
                $"blur shifted a flat colour to {p}");
        }
    }

    /// <summary>Blurring must not pull colour out of fully transparent pixels: that is the classic dark halo bug.</summary>
    [TestMethod]
    public void BlurDoesNotBleedColourFromTransparentPixels()
    {
        using var src = BmageImageBuffer.Create(64, 64, BmageRgba32.Transparent);
        // An opaque white square on a transparent field whose RGB is black.
        for (var y = 20; y < 44; y++)
            for (var x = 20; x < 44; x++)
                src[x, y] = BmageRgba32.White;

        using var result = new BmageBlurFilter(4f).Apply(src, BmageOperationContext.Default);

        // Every partially transparent edge pixel must stay white, not darken towards the transparent black around it.
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var p = result[x, y];
                if (p.A is 0 or 255) continue;
                Assert.IsTrue(p.R > 200 && p.G > 200 && p.B > 200,
                    $"blur darkened a semi-transparent edge pixel at ({x},{y}) to {p}");
            }
        }
    }

    /// <summary>Adjustments must leave the alpha channel alone unless they exist to change it.</summary>
    [TestMethod]
    public void RgbAdjustmentsPreserveAlpha()
    {
        var ops = new IBmageImageOperation[]
        {
            new BmageBrightnessAdjustment(1f), new BmageBrightnessAdjustment(-1f),
            new BmageContrastAdjustment(1f), new BmageContrastAdjustment(-1f),
            new BmageExposureAdjustment(5f), new BmageExposureAdjustment(-5f),
            new BmageSaturationAdjustment(1f), new BmageSaturationAdjustment(-1f),
            new BmageHueAdjustment(180f), new BmageHueAdjustment(-90f),
            new BmageGammaAdjustment(0.05f), new BmageGammaAdjustment(20f),
            new BmageVibranceAdjustment(1f), new BmageVibranceAdjustment(-1f),
            new BmageShadowsHighlightsAdjustment(1f, 1f),
            new BmageLevelsAdjustment(0.9f, 0.91f, 0.1f),
            new BmageTemperatureAdjustment(1f), new BmageTintAdjustment(-1f),
            new BmageGrayscaleFilter(), new BmageSepiaFilter(), new BmageInvertFilter(),
            new BmagePosterizeFilter(2), new BmageThresholdFilter(0.5f),
        };

        using var src = TestImages.Translucent(32, 32);
        foreach (var op in ops)
        {
            using var dst = op.Apply(src, BmageOperationContext.Default);
            for (var i = 0; i < src.PixelCount; i++)
                Assert.IsTrue(src.Pixels[i].A == dst.Pixels[i].A, $"{op.Name} changed alpha at pixel {i}");
        }
    }

    /// <summary>Opacity is the one adjustment that exists to change alpha, and it must change only alpha.</summary>
    [TestMethod]
    public void OpacityScalesAlphaAndLeavesColourAlone()
    {
        using var src = BmageImageBuffer.Create(16, 16, new BmageRgba32(200, 100, 50, 200));
        using var dst = new BmageOpacityAdjustment(0.5f).Apply(src, BmageOperationContext.Default);
        foreach (var p in dst.Pixels.ToArray())
        {
            Assert.AreEqual(200, p.R);
            Assert.AreEqual(100, p.G);
            Assert.AreEqual(50, p.B);
            Assert.IsInRange(99, 101, p.A);
        }
    }

    /// <summary>Identity parameters must be recognised, so the optimiser can drop the work entirely.</summary>
    [TestMethod]
    public void NeutralParametersReportIdentity()
    {
        Assert.IsTrue(new BmageBrightnessAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageContrastAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageSaturationAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageHueAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageExposureAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageVibranceAdjustment(0f).IsIdentity);
        Assert.IsTrue(new BmageShadowsHighlightsAdjustment(0f, 0f).IsIdentity);
        Assert.IsTrue(new BmageGammaAdjustment(1f).IsIdentity);
        Assert.IsTrue(new BmageBlurFilter(0f).IsIdentity);
        Assert.IsTrue(new BmageNoiseFilter(0f).IsIdentity);
        Assert.IsTrue(new BmagePixelateFilter(1).IsIdentity);
        Assert.IsTrue(new BmageVignetteFilter(0f).IsIdentity);
        Assert.IsTrue(new BmageGrayscaleFilter(0f).IsIdentity);
        Assert.IsTrue(new BmageSepiaFilter(0f).IsIdentity);
        Assert.IsTrue(new BmageInvertFilter(0f).IsIdentity);

        Assert.IsFalse(new BmageBrightnessAdjustment(0.1f).IsIdentity);
        Assert.IsFalse(new BmageGammaAdjustment(1.5f).IsIdentity);
        Assert.IsFalse(new BmageBlurFilter(2f).IsIdentity);
    }

    /// <summary>Noise must be deterministic for a given seed, or previews and exports would disagree.</summary>
    [TestMethod]
    public void NoiseIsDeterministicForASeed()
    {
        using var src = Noise(32, 32, 17);
        using var a = new BmageNoiseFilter(0.4f, false, 1234).Apply(src, BmageOperationContext.Default);
        using var b = new BmageNoiseFilter(0.4f, false, 1234).Apply(src, BmageOperationContext.Default);
        using var c = new BmageNoiseFilter(0.4f, false, 4321).Apply(src, BmageOperationContext.Default);

        Assert.IsTrue(a.Bytes.SequenceEqual(b.Bytes), "the same seed produced different noise");
        Assert.IsFalse(a.Bytes.SequenceEqual(c.Bytes), "different seeds produced identical noise");
    }

    /// <summary>
    /// Regression guard for the zero-area rectangle trap. System.Drawing's Rectangle.IsEmpty asks whether all four
    /// fields are zero, not whether the rectangle covers any pixels, and Rectangle.Intersect returns a zero-width or
    /// zero-height rectangle whenever two rectangles merely touch along an edge. Any API taking a region has to treat
    /// those as covering nothing rather than indexing past the end of a buffer.
    /// </summary>
    [TestMethod]
    public void ZeroAreaRegionsAreTreatedAsCoveringNothing()
    {
        var degenerate = new Rectangle(64, 0, 0, 48);
        Assert.IsFalse(degenerate.IsEmpty, "the premise of this test is that such a rectangle is not Rectangle.Empty");

        using var image = BmageImageBuffer.Create(64, 48, BmageRgba32.White);
        var before = image.ToArray();

        // Filling and copying must not throw or write anything.
        image.Fill(degenerate, BmageRgba32.Black);
        Assert.IsTrue(before.AsSpan().SequenceEqual(image.Bytes));
        Assert.ThrowsExactly<ArgumentException>(() => image.CopyRegion(degenerate).Dispose());

        // Drawing through a canvas clipped to nothing must draw nothing.
        using (var canvas = BmageImageCanvas.FromBuffer(image))
        {
            canvas.Clip = degenerate;
            canvas.FillRectangle(new RectangleF(0, 0, 64, 48), BmageRgba32.Black);
        }
        Assert.IsTrue(before.AsSpan().SequenceEqual(image.Bytes), "drawing through an empty clip changed pixels");

        // Statistics over an empty region must report a clear error, not crash.
        Assert.ThrowsExactly<ArgumentException>(() => Bit.Bmage.BmageImageStatistics.Measure(image, degenerate));
    }

    /// <summary>Scaling a region filter for a proxy must scale the region with it.</summary>
    [TestMethod]
    [DynamicData(nameof(RegionFilters))]
    public void ScalingAFilterScalesItsRegion(string kind)
    {
        var region = new Rectangle(40, 20, 80, 60);
        var scaled = (BmageRegionOperation)CreateRegionFilter(kind, region).ForScale(0.5);
        var r = scaled.Region!.Value;
        Assert.AreEqual(20, r.X);
        Assert.AreEqual(10, r.Y);
        Assert.AreEqual(40, r.Width);
        Assert.AreEqual(30, r.Height);
    }
}
