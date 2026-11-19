using System.Drawing;
using System.Numerics;
using System.Text.Json;

namespace Bit.Bmage.Tests;

/// <summary>
/// An edit is portable: every built-in operation, mask, layer and annotation writes to JSON and reads back as the same
/// edit, which renders the same pixels.
/// </summary>
[TestClass]
public sealed class SerializationTests
{
    private static readonly BmageImageBuffer Stamp = BmageImageBuffer.Create(16, 12, new BmageRgba32(10, 200, 30, 220));

    private static BmageJsonOptions WithImages() => new()
    {
        ImageReferences = image => ReferenceEquals(image, Stamp) ? "stamp" : throw new InvalidOperationException("An unexpected image."),
        ImageResolver = reference => reference == "stamp" ? Stamp : null,
    };

    private static IEnumerable<object[]> AllOperations() => new IBmageImageOperation[]
    {
        BmageAutoOrientOperation.Instance,
        new BmageCropOperation(10, 5, 120, 90),
        new BmageAspectCropOperation(BmageAspectRatio.Ratio16x9, BmageAnchor.TopRight),
        new BmageSmartCropOperation(BmageAspectRatio.Square, BmageSmartCropOptions.Default with { SkinWeight = 0.5, SearchSteps = 12 }),
        new BmageResizeOperation(100, null, BmageResizeMode.Max, BmageResamplingFilter.Lanczos3),
        new BmageResizeOperation(90, 70, BmageResizeMode.Contain, BmageResamplingFilter.Bicubic, BmageAnchor.Bottom, new BmageRgba32(1, 2, 3, 4)),
        (IBmageImageOperation)BmageImagePipeline.Create().Resize(100, 80, BmageResizeMode.Stretch).Crop(10, 10, 50, 40).Optimize(new Size(160, 120))[0],
        new BmageRotateOperation(33.25, new BmageRgba32(9, 8, 7, 6), expandCanvas: false, BmageWarpSampling.NearestNeighbor),
        new BmageOrientationOperation(BmageOrientation.Transverse),
        new BmageStraightenOperation(-7.25, BmageWarpSampling.Bilinear),
        new BmageCanvasSizeOperation(200, 90, BmageAnchor.BottomLeft, new BmageRgba32(1, 2, 3, 200)),
        BmageCanvasSizeOperation.Margins(12, -3, 0, 7, BmageRgba32.White),
        new BmageSkewOperation(12, -4, BmageRgba32.White),
        new BmageAffineTransformOperation(Matrix3x2.CreateRotation(0.3f) * Matrix3x2.CreateScale(0.8f), new Size(120, 100), BmageRgba32.Black, BmageWarpSampling.Bicubic, "Tilt"),
        new BmagePerspectiveTransformOperation([new PointF(5, 2), new PointF(140, 10), new PointF(150, 110), new PointF(0, 115)]),
        BmagePerspectiveTransformOperation.Straighten([new PointF(10, 10), new PointF(140, 5), new PointF(150, 110), new PointF(5, 100)], new Size(100, 80)),
        new BmageBrightnessAdjustment(0.12f), new BmageContrastAdjustment(-0.3f), new BmageExposureAdjustment(0.7f), new BmageSaturationAdjustment(0.4f),
        new BmageHueAdjustment(-45), new BmageTemperatureAdjustment(0.3f), new BmageTintAdjustment(-0.2f), new BmageOpacityAdjustment(0.6f),
        new BmageGrayscaleFilter(0.5f), new BmageSepiaFilter(0.8f), new BmageInvertFilter(), new BmageVibranceAdjustment(0.6f), new BmageGammaAdjustment(1.6f),
        new BmageLevelsAdjustment(0.1f, 0.9f, 1.2f, 0.05f, 0.95f, BmageColorChannels.Red | BmageColorChannels.Blue),
        new BmagePosterizeFilter(5), new BmageThresholdFilter(0.4f), new BmageShadowsHighlightsAdjustment(0.3f, -0.2f),
        new BmageWhitesBlacksAdjustment(0.3f, -0.4f),
        new BmageHslAdjustment().With(BmageHueBand.Blues, new BmageHslShift(-12, 0.3f, -0.2f)).With(BmageHueBand.Oranges, new BmageHslShift(Lightness: 0.25f)),
        BmageWhiteBalanceAdjustment.FromNeutral(new BmageRgba32(210, 190, 160), 0.9f), new BmageAutoWhiteBalanceOperation(0.6f),
        new BmageChannelMixerAdjustment(new Vector3(0.9f, 0.1f, 0), new Vector3(0.05f, 0.9f, 0.05f), new Vector3(0, 0.2f, 0.8f), new Vector3(0.02f, 0, -0.01f)),
        BmageChannelMixerAdjustment.Monochrome(0.5f, 0.3f, 0.2f), new BmageColorizeAdjustment(200, 0.4f, -0.1f, 0.8f),
        new BmageGradientMapOperation([(0f, new BmageRgba32(10, 0, 40)), (0.4f, new BmageRgba32(200, 40, 80)), (1f, new BmageRgba32(255, 240, 200))], 0.7f),
        new BmageColorMatrixOperation(BmageColorMatrix.Concat(BmageColorMatrix.Saturate(1.2f), BmageColorMatrix.Scale(1.05f, 1f, 0.9f)), "Warm"),
        BmageCurveOperation.FromFunction(v => v * v, BmageColorChannels.Green, "Square green"),
        new BmageFusedPointOperation([new BmageBrightnessAdjustment(0.1f), new BmageSaturationAdjustment(0.2f)]),
        new BmageAutoLevelsOperation(0.01, perChannel: true, strength: 0.8f), new BmageAutoContrastOperation(0.45f, 0.5f),
        new BmageBlurFilter(2.5f, new Rectangle(10, 10, 60, 40)), new BmageSharpenFilter(0.7f, 1.5f, 0.02f), new BmagePixelateFilter(6, new Rectangle(0, 0, 50, 50)),
        new BmageNoiseFilter(0.1f, monochrome: true, seed: 42), new BmageMedianFilter(2), BmageConvolutionFilter.Emboss(new Rectangle(5, 5, 40, 40)),
        new BmageVignetteFilter(0.6f, 0.7f, 0.4f, new BmageRgba32(20, 10, 0)),
        new BmageMotionBlurFilter(12.5f, 30, new Rectangle(10, 5, 100, 60)), new BmageMotionBlurFilter(7, -80), new BmageHighPassFilter(2.5f, new Rectangle(0, 0, 80, 80)),
        new BmageAdaptiveThresholdFilter(9.5f, 0.3f, BmageAdaptiveThresholdMethod.Sauvola, new Rectangle(5, 5, 90, 70)), new BmageAdaptiveThresholdFilter(4, 0.05f, BmageAdaptiveThresholdMethod.Mean),
        new BmageRadialBlurFilter(0.2f), new BmageRadialBlurFilter(0.4f, BmageRadialBlurMode.Spin, new PointF(0.3f, 0.6f), new Rectangle(0, 0, 100, 100)),
        new BmageColorLutAdjustment(BmageColorLookupTable.Create(5, c => new Vector3(c.Z, c.X * 0.9f, c.Y), "Swap"), 0.75f),
        new BmageMaskedOperation(new BmageBrightnessAdjustment(0.3f), BmageMask.Ellipse() with { Feather = 3 }),
        new BmageAlphaMaskOperation(new BmageRectangleMask(new RectangleF(5, 5, 100, 80)) { CornerRadius = 12 }),
        new BmagePipelineOperation(BmageImagePipeline.Create().Contrast(0.2f).Blur(1), "Pop"),
    }.Select(operation => new object[] { operation });

    private static IEnumerable<object[]> AllMasks() => new BmageMask[]
    {
        new BmageRectangleMask(new RectangleF(10, 10, 80, 60)) { CornerRadius = 8, Feather = 2, Inverted = true },
        BmageMask.Ellipse() with { Opacity = 0.7f },
        new BmagePolygonMask([new(5, 5), new(120, 20), new(60, 100)]) { Rule = BmageFillRule.EvenOdd },
        new BmagePathMask(new BmagePathBuilder().AddEllipse(new RectangleF(20, 20, 60, 40)).Build()),
        new BmageBrushMask([new BmageBrushStroke([new(10, 10), new(80, 50)], 12), new BmageBrushStroke([new(40, 0), new(40, 90)], 6) { Erase = true, Opacity = 0.5f }]),
        BmageMask.Gradient(new(0, 0), new(0, 0.7f)),
        new BmageRadialGradientMask(new Vector2(70, 50), 50, 30) { Hardness = 0.3f, Rotation = 20 },
        new BmageLuminanceRangeMask(0.6f, 1f) { Softness = 0.2f },
        new BmageColorRangeMask(new BmageRgba32(200, 40, 40)) { Tolerance = 0.2f, Softness = 0.05f },
        new BmageBitmapMask(Stamp) { Channel = BmageMaskChannel.Luminance },
        new BmageEllipseMask(new RectangleF(0, 0, 60, 60)).Subtract(new BmageRectangleMask(new RectangleF(20, 20, 20, 20))),
        new BmageFloodMask([new(10, 10), new(120, 80)]) { Tolerance = 0.3f, Softness = 0.1f, Feather = 1.5f },
        new BmageFloodMask([new(0.5f, 0.5f)]) { Units = BmageMaskUnits.Relative, Contiguous = false, Inverted = true },
        new BmageEllipseMask(new RectangleF(20, 20, 60, 40)).Expand(4.5f),
        new BmageRectangleMask(new RectangleF(10, 10, 50, 50)).Contract(3) with { Feather = 2 },
        new BmageRectangleMask(new RectangleF(10, 10, 50, 50)).Border(5),
    }.Select(mask => new object[] { mask });

    private static IEnumerable<object[]> AllLayers() => new BmageLayer[]
    {
        new BmageImageLayer { Image = Stamp, Bounds = new RectangleF(20, 10, 64, 48), Rotation = 15, Pipeline = BmageImagePipeline.Create().Grayscale(), Filter = BmageResamplingFilter.Bilinear, Name = "Logo" },
        new BmageFillLayer(new BmageRgba32(30, 60, 200, 128)) { BlendMode = BmageBlendMode.Color, Opacity = 0.8f, Mask = BmageMask.Gradient(new(0, 1), new(0, 0.4f)) },
        new BmageAdjustmentLayer(BmageImagePipeline.Create().Contrast(0.3f).Vibrance(0.2f)) { Mask = new BmageLuminanceRangeMask(0, 0.4f), IsVisible = false, IsLocked = true },
        new BmagePaintLayer([new BmagePaintStroke([new(10, 10), new(60, 40)], 9, new BmageRgba32(200, 30, 30)) { Pressures = [0.5f, 1f] }]) { Opacity = 0.6f },
        new BmageFillLayer(new BmageRgba32(40, 160, 90)) { Mask = new BmageFloodMask([new(30, 30)]) { Tolerance = 0.2f }, Name = "Bucket" },
        new BmageGroupLayer(
        [
            new BmageFillLayer(new BmageRgba32(200, 120, 20)) { Mask = new BmageEllipseMask(new RectangleF(10, 10, 50, 40)) },
            new BmageFillLayer(new BmageRgba32(20, 40, 220)) { ClipToLayerBelow = true, BlendMode = BmageBlendMode.Multiply },
            new BmageGroupLayer([new BmageAdjustmentLayer(BmageImagePipeline.Create().Invert())]) { Isolated = true, Opacity = 0.7f },
        ]) { Name = "Badge", Opacity = 0.8f, Mask = new BmageRectangleMask(new RectangleF(0.1f, 0.1f, 0.8f, 0.8f)) { Units = BmageMaskUnits.Relative } },
        new BmageGroupLayer([new BmageFillLayer(new BmageRgba32(90, 90, 90))]) { BlendMode = BmageBlendMode.Screen },
    }.Select(layer => new object[] { layer });

    private static IEnumerable<object[]> AllAnnotations() => new BmageAnnotation[]
    {
        new BmageRectangleAnnotation { Rectangle = new RectangleF(10, 10, 50, 30), CornerRadius = 4, ZIndex = 2, Tag = "box", Style = BmageShapeStyle.Default with { FillColor = new BmageRgba32(1, 2, 3, 100) } },
        new BmageEllipseAnnotation { Rectangle = new RectangleF(30, 20, 40, 40), Rotation = 30, Opacity = 0.5f },
        new BmageLineAnnotation { Start = new Vector2(1, 2), End = new Vector2(90, 70), Style = BmageShapeStyle.Default with { Stroke = new BmageStrokeStyle { Width = 5 }.WithDashes(6, 3) } },
        new BmageArrowAnnotation { Start = new Vector2(5, 80), End = new Vector2(100, 10), HeadLengthFactor = 5 },
        new BmageFreehandAnnotation { Points = [new(10, 10), new(20, 30), new(40, 25), new(60, 50)], Closed = true },
        new BmagePolygonAnnotation { Points = [new(10, 10), new(60, 15), new(30, 60)], Closed = false, IsLocked = true },
        new BmageTextAnnotation { Text = "Hello\nworld", Position = new Vector2(20, 40), BackgroundColor = BmageRgba32.White, MeasuredSize = new SizeF(80, 40), Style = new BmageTextStyle { FontSize = 22, Bold = true, MaxWidth = 120, Alignment = BmageTextAlignment.Center } },
        new BmageHighlightAnnotation { Rectangle = new RectangleF(0, 0, 40, 10) },
        new BmageRedactionAnnotation { Rectangle = new RectangleF(50, 50, 30, 20), Mode = BmageRedactionMode.Pixelate, Strength = 8 },
        new BmageCalloutAnnotation { Bubble = new RectangleF(60, 5, 60, 30), Target = new Vector2(20, 70), Text = "Look" },
        new BmageStepAnnotation { Center = new Vector2(100, 80), Number = 3, IsVisible = false },
        new BmageImageAnnotation { Image = Stamp, Rectangle = new RectangleF(70, 60, 32, 24) },
    }.Select(annotation => new object[] { annotation });

    [TestMethod]
    [DynamicData(nameof(AllOperations))]
    public void EveryOperationRoundTripsAndRendersTheSamePixels(IBmageImageOperation operation)
    {
        var json = BmageJson.SerializeObject(operation);
        var back = BmageJson.DeserializeObject<IBmageImageOperation>(json);

        Assert.AreEqual(operation.GetType(), back.GetType());
        Assert.AreEqual(json, BmageJson.SerializeObject(back), "Reading and writing again changed the JSON.");
        using var source = ParallelProcessingTests.Noise(160, 120, seed: 3);
        using var expected = operation.Apply(source, BmageOperationContext.Default);
        using var actual = back.Apply(source, BmageOperationContext.Default);
        Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes), $"{operation.Name} renders differently after a round trip.");
    }

    [TestMethod]
    [DynamicData(nameof(AllMasks))]
    public void EveryMaskRoundTripsAndRendersTheSameCoverage(BmageMask mask)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(mask, options);
        var back = BmageJson.DeserializeObject<BmageMask>(json, options);

        Assert.AreEqual(json, BmageJson.SerializeObject(back, options));
        using var image = ParallelProcessingTests.Noise(140, 100, seed: 9);
        using var expected = mask.Render(image.Size, image);
        using var actual = back.Render(image.Size, image);
        Assert.IsTrue(expected.Values.SequenceEqual(actual.Values), $"The {mask.Kind} mask renders differently after a round trip.");
    }

    [TestMethod]
    [DynamicData(nameof(AllLayers))]
    public void EveryLayerRoundTripsAndCompositesTheSame(BmageLayer layer)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(layer, options);
        var back = BmageJson.DeserializeObject<BmageLayer>(json, options);

        Assert.AreEqual(layer.Id, back.Id);
        Assert.AreEqual(json, BmageJson.SerializeObject(back, options));
        using var expected = ParallelProcessingTests.Noise(140, 100, seed: 10);
        using var actual = expected.Clone();
        layer.Composite(expected);
        back.Composite(actual);
        Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes));
    }

    [TestMethod]
    [DynamicData(nameof(AllAnnotations))]
    public void EveryAnnotationRoundTripsAsAnEqualRecord(BmageAnnotation annotation)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(annotation, options);
        var back = BmageJson.DeserializeObject<BmageAnnotation>(json, options);

        Assert.AreEqual(json, BmageJson.SerializeObject(back, options));
        // Records compare by value, apart from point lists and styles holding arrays, which the JSON comparison covers.
        Assert.AreEqual(annotation.Id, back.Id);
        Assert.AreEqual(annotation.GetBounds(), back.GetBounds());
    }

    [TestMethod]
    public void EveryPublicEditTypeHasACodec()
    {
        var kinds = new[] { typeof(IBmageImageOperation), typeof(BmageAnnotation), typeof(BmageLayer), typeof(BmageMask) };
        var missing = typeof(BmageImageBuffer).Assembly.GetExportedTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && kinds.Any(kind => kind.IsAssignableFrom(type)))
            .Where(type => !BmageJsonOptions.Default.Codecs.Supports(type))
            .Select(type => type.Name)
            .ToArray();

        Assert.AreEqual(0, missing.Length, $"These types cannot be serialised: {string.Join(", ", missing)}.");
    }

    [TestMethod]
    public void AWholeDocumentRoundTripsAndRendersTheSame()
    {
        using var source = ParallelProcessingTests.Noise(200, 150, seed: 14);
        var document = BmageImageDocument.Create(source)
            .AddOperation(new BmageCropOperation(10, 10, 180, 130))
            .AddOperation(new BmageMaskedOperation(new BmagePipelineOperation(BmageImagePipeline.Create().Exposure(-0.4f).Saturation(0.2f)), BmageMask.Gradient(new(0, 0), new(0, 0.5f))))
            .AddOperation(new BmageSharpenFilter(0.4f))
            .AddLayer(new BmageImageLayer { Image = Stamp, Bounds = new RectangleF(100, 80, 48, 36), BlendMode = BmageBlendMode.Multiply })
            .AddLayer(new BmageAdjustmentLayer(BmageImagePipeline.Create().Vibrance(0.3f)) { Mask = BmageMask.Ellipse() with { Feather = 6 } })
            .AddAnnotation(new BmageArrowAnnotation { Start = new Vector2(10, 100), End = new Vector2(90, 30) })
            .AddAnnotation(new BmageRedactionAnnotation { Rectangle = new RectangleF(120, 10, 40, 20) });

        var options = WithImages();
        var json = BmageJson.Serialize(document, options);
        var back = BmageJson.DeserializeDocument(json, options);

        Assert.AreEqual(document.SourceSize, back.SourceSize);
        Assert.AreEqual(json, BmageJson.Serialize(back, options));
        using var expected = new BmageDocumentRenderer().Render(document, source);
        using var actual = new BmageDocumentRenderer().Render(back, source);
        FastPathEquivalenceTests.AssertSamePixels(expected, actual, "document round trip");
    }

    [TestMethod]
    public void APipelineRoundTripsOnItsOwn()
    {
        var pipeline = BmageImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f).ApplyMask(BmageMask.Ellipse());
        var json = BmageJson.Serialize(pipeline);
        var back = BmageJson.DeserializePipeline(json);

        Assert.AreEqual(pipeline.ToString(), back.ToString());
        StringAssert.Contains(json, "\"format\":\"bmage-pipeline\"");
    }

    [TestMethod]
    public void TheFormatIsReadableJson()
    {
        var json = BmageJson.Serialize(BmageImagePipeline.Create().Crop(1, 2, 30, 40).Brightness(0.5f), new BmageJsonOptions { Indented = true });
        using var parsed = JsonDocument.Parse(json);
        var first = parsed.RootElement.GetProperty("operations")[0];

        Assert.AreEqual("crop", first.GetProperty("type").GetString());
        Assert.AreEqual(30, first.GetProperty("rectangle")[2].GetInt32());
        Assert.AreEqual(0.5f, parsed.RootElement.GetProperty("operations")[1].GetProperty("amount").GetSingle());
        StringAssert.Contains(json, "\n");
    }

    [TestMethod]
    public void ImagesAreReferencedNeverEmbedded()
    {
        var annotation = new BmageImageAnnotation { Image = Stamp, Rectangle = new RectangleF(0, 0, 16, 12) };

        Assert.ThrowsExactly<NotSupportedException>(() => BmageJson.SerializeObject(annotation));
        var json = BmageJson.SerializeObject(annotation, WithImages());
        StringAssert.Contains(json, "\"image\":\"stamp\"");
        Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeObject<BmageAnnotation>(json));
        Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeObject<BmageAnnotation>(json, new BmageJsonOptions { ImageResolver = _ => null }));
    }

    [TestMethod]
    public void MalformedInputIsRejectedWithItsLocation()
    {
        AssertRejected("not json", "not valid JSON");
        AssertRejected("""{"format":"bmage-document","version":99,"sourceSize":[10,10]}""", "version 99");
        AssertRejected("""{"format":"something-else","version":1}""", "something-else");
        AssertRejected("""{"format":"bmage-document","version":1,"sourceSize":[10,10],"pipeline":[{"type":"teleport"}]}""", "$.pipeline[0]");
        AssertRejected("""{"format":"bmage-document","version":1,"sourceSize":[10,10],"pipeline":[{"type":"crop"}]}""", "rectangle");
        AssertRejected("""{"format":"bmage-document","version":1,"sourceSize":[10,10],"pipeline":[{"type":"resize","mode":"Sideways"}]}""", "Sideways");
        AssertRejected("""{"format":"bmage-document","version":1,"sourceSize":[10,10],"pipeline":[{"type":"crop","rectangle":[0,0,0,5]}]}""", "invalid");
        AssertRejected("""{"format":"bmage-document","version":1,"sourceSize":[0,10]}""", "sourceSize");

        static void AssertRejected(string json, string expected)
        {
            var ex = Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeDocument(json));
            StringAssert.Contains(ex.Message, expected);
        }
    }

    [TestMethod]
    public void OptionalPropertiesTakeTheirDefaults()
    {
        var back = BmageJson.DeserializePipeline("""{"format":"bmage-pipeline","version":1,"operations":[{"type":"sharpen"},{"type":"rotate","degrees":10}]}""");

        var sharpen = (BmageSharpenFilter)back[0];
        Assert.AreEqual(0.5f, sharpen.Amount);
        Assert.AreEqual(1f, sharpen.Radius);
        var rotate = (BmageRotateOperation)back[1];
        Assert.IsTrue(rotate.ExpandCanvas);
        Assert.AreEqual(BmageWarpSampling.Bicubic, rotate.Sampling);
    }

    [TestMethod]
    public void ACustomOperationNeedsACodec_AndRoundTripsWithOne()
    {
        var duotone = new Duotone(new BmageRgba32(10, 20, 60), new BmageRgba32(250, 230, 200));
        Assert.ThrowsExactly<NotSupportedException>(() => BmageJson.Serialize(BmageImagePipeline.Create().Apply(duotone)));

        var options = new BmageJsonOptions().Add<Duotone>("test.duotone",
            (writer, d) => writer.Write("shadow", d.Shadow).Write("highlight", d.Highlight),
            reader => new Duotone(reader.GetColor("shadow"), reader.GetColor("highlight")));
        var json = BmageJson.Serialize(BmageImagePipeline.Create().Brightness(0.1f).Apply(duotone), options);
        var back = BmageJson.DeserializePipeline(json, options);

        var read = (Duotone)back[1];
        Assert.AreEqual(duotone.Shadow, read.Shadow);
        Assert.AreEqual(duotone.Highlight, read.Highlight);
    }

    [TestMethod]
    public void TheSharedDefaultOptionsCannotBeChanged()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BmageJsonOptions.Default.Indented = true);
        Assert.ThrowsExactly<InvalidOperationException>(() => BmageJsonOptions.Default.Add<Duotone>("x", (_, _) => { }, _ => new Duotone(default, default)));
    }

    [TestMethod]
    public void ASubclassOfABuiltInTypeNeedsACodecOfItsOwn()
    {
        // It may draw different pixels than its base; written as the base, it would read back as another edit.
        Assert.ThrowsExactly<NotSupportedException>(() => BmageJson.SerializeObject(new MyMatrix()));
    }

    [TestMethod]
    public void EnumsAreReadByTheirNamesOnly()
    {
        AssertMode("Multiply", ok: true);
        AssertMode(" multiply ", ok: true);
        AssertMode("Copy, Erase", ok: false);    // a combination of an enum that is not a set of flags
        AssertMode("3", ok: false);
        AssertMode(" 99", ok: false);
        var levels = BmageJson.DeserializeObject<IBmageImageOperation>("""{"type":"levels","channels":"Red, Green, Blue"}""");
        Assert.AreEqual(BmageColorChannels.Rgb, ((BmageLevelsAdjustment)levels).Channels, "A set of flags may combine its members.");

        static void AssertMode(string mode, bool ok)
        {
            var json = $$"""{"type":"fill","color":"#ff0000ff","blendMode":"{{mode}}"}""";
            if (ok) Assert.AreEqual(BmageBlendMode.Multiply, BmageJson.DeserializeObject<BmageLayer>(json).BlendMode);
            else Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeObject<BmageLayer>(json), mode);
        }
    }

    [TestMethod]
    public void WholeNumbersAreReadExactly()
    {
        var crop = (BmageCropOperation)BmageJson.DeserializeObject<IBmageImageOperation>("""{"type":"crop","rectangle":[16777217,0,2147483647,100]}""");
        Assert.AreEqual(new Rectangle(16777217, 0, int.MaxValue, 100), crop.Rectangle);
        Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeObject<IBmageImageOperation>("""{"type":"crop","rectangle":[0,0,2147483648,100]}"""));
        Assert.ThrowsExactly<BmageImageDocumentFormatException>(() => BmageJson.DeserializeObject<IBmageImageOperation>("""{"type":"crop","rectangle":[0,0,10.5,100]}"""));
    }

    [TestMethod]
    public void AnUndefinedEnumValueIsNotWritten()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BmageJson.SerializeObject(new BmageFillLayer(BmageRgba32.Black) { BlendMode = (BmageBlendMode)99 }));
    }

    private sealed class MyMatrix() : BmageColorMatrixOperation(BmageColorMatrix.Saturate(0.5f), "Mine");

    private sealed class Duotone(BmageRgba32 shadow, BmageRgba32 highlight) : BmagePointOperation
    {
        public BmageRgba32 Shadow { get; } = shadow;
        public BmageRgba32 Highlight { get; } = highlight;
        public override string Name => "Duotone";
        public override void ProcessRow(Span<Vector4> pixels) { }
    }
}
