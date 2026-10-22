using System.Drawing;
using System.Numerics;
using System.Text.Json;
using Bit.Bmage.Analysis;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Serialization;

namespace Bit.Bmage.Tests;

/// <summary>
/// An edit is portable: every built-in operation, mask, layer and annotation writes to JSON and reads back as the same
/// edit, which renders the same pixels.
/// </summary>
[TestClass]
public sealed class SerializationTests
{
    private static readonly ImageBuffer Stamp = ImageBuffer.Create(16, 12, new Rgba32(10, 200, 30, 220));

    private static BmageJsonOptions WithImages() => new()
    {
        ImageReferences = image => ReferenceEquals(image, Stamp) ? "stamp" : throw new InvalidOperationException("An unexpected image."),
        ImageResolver = reference => reference == "stamp" ? Stamp : null,
    };

    private static IEnumerable<object[]> AllOperations() => new IImageOperation[]
    {
        AutoOrientOperation.Instance,
        new CropOperation(10, 5, 120, 90),
        new AspectCropOperation(AspectRatio.Ratio16x9, Anchor.TopRight),
        new SmartCropOperation(AspectRatio.Square, SmartCropOptions.Default with { SkinWeight = 0.5, SearchSteps = 12 }),
        new ResizeOperation(100, null, ResizeMode.Max, ResamplingFilter.Lanczos3),
        new ResizeOperation(90, 70, ResizeMode.Contain, ResamplingFilter.Bicubic, Anchor.Bottom, new Rgba32(1, 2, 3, 4)),
        (IImageOperation)ImagePipeline.Create().Resize(100, 80, ResizeMode.Stretch).Crop(10, 10, 50, 40).Optimize(new Size(160, 120))[0],
        new RotateOperation(33.25, new Rgba32(9, 8, 7, 6), expandCanvas: false, WarpSampling.NearestNeighbor),
        new OrientationOperation(Orientation.Transverse),
        new SkewOperation(12, -4, Rgba32.White),
        new AffineTransformOperation(Matrix3x2.CreateRotation(0.3f) * Matrix3x2.CreateScale(0.8f), new Size(120, 100), Rgba32.Black, WarpSampling.Bicubic, "Tilt"),
        new PerspectiveTransformOperation([new PointF(5, 2), new PointF(140, 10), new PointF(150, 110), new PointF(0, 115)]),
        PerspectiveTransformOperation.Straighten([new PointF(10, 10), new PointF(140, 5), new PointF(150, 110), new PointF(5, 100)], new Size(100, 80)),
        new BrightnessAdjustment(0.12f), new ContrastAdjustment(-0.3f), new ExposureAdjustment(0.7f), new SaturationAdjustment(0.4f),
        new HueAdjustment(-45), new TemperatureAdjustment(0.3f), new TintAdjustment(-0.2f), new OpacityAdjustment(0.6f),
        new GrayscaleFilter(0.5f), new SepiaFilter(0.8f), new InvertFilter(), new VibranceAdjustment(0.6f), new GammaAdjustment(1.6f),
        new LevelsAdjustment(0.1f, 0.9f, 1.2f, 0.05f, 0.95f, ColorChannels.Red | ColorChannels.Blue),
        new PosterizeFilter(5), new ThresholdFilter(0.4f), new ShadowsHighlightsAdjustment(0.3f, -0.2f),
        new ColorMatrixOperation(ColorMatrix.Concat(ColorMatrix.Saturate(1.2f), ColorMatrix.Scale(1.05f, 1f, 0.9f)), "Warm"),
        CurveOperation.FromFunction(v => v * v, ColorChannels.Green, "Square green"),
        new FusedPointOperation([new BrightnessAdjustment(0.1f), new SaturationAdjustment(0.2f)]),
        new AutoLevelsOperation(0.01, perChannel: true, strength: 0.8f), new AutoContrastOperation(0.45f, 0.5f),
        new BlurFilter(2.5f, new Rectangle(10, 10, 60, 40)), new SharpenFilter(0.7f, 1.5f, 0.02f), new PixelateFilter(6, new Rectangle(0, 0, 50, 50)),
        new NoiseFilter(0.1f, monochrome: true, seed: 42), new MedianFilter(2), ConvolutionFilter.Emboss(new Rectangle(5, 5, 40, 40)),
        new VignetteFilter(0.6f, 0.7f, 0.4f, new Rgba32(20, 10, 0)),
        new MaskedOperation(new BrightnessAdjustment(0.3f), Mask.Ellipse() with { Feather = 3 }),
        new AlphaMaskOperation(new RectangleMask(new RectangleF(5, 5, 100, 80)) { CornerRadius = 12 }),
        new PipelineOperation(ImagePipeline.Create().Contrast(0.2f).Blur(1), "Pop"),
    }.Select(operation => new object[] { operation });

    private static IEnumerable<object[]> AllMasks() => new Mask[]
    {
        new RectangleMask(new RectangleF(10, 10, 80, 60)) { CornerRadius = 8, Feather = 2, Inverted = true },
        Mask.Ellipse() with { Opacity = 0.7f },
        new PolygonMask([new(5, 5), new(120, 20), new(60, 100)]) { Rule = FillRule.EvenOdd },
        new PathMask(new PathBuilder().AddEllipse(new RectangleF(20, 20, 60, 40)).Build()),
        new BrushMask([new BrushStroke([new(10, 10), new(80, 50)], 12), new BrushStroke([new(40, 0), new(40, 90)], 6) { Erase = true, Opacity = 0.5f }]),
        Mask.Gradient(new(0, 0), new(0, 0.7f)),
        new RadialGradientMask(new Vector2(70, 50), 50, 30) { Hardness = 0.3f, Rotation = 20 },
        new LuminanceRangeMask(0.6f, 1f) { Softness = 0.2f },
        new ColorRangeMask(new Rgba32(200, 40, 40)) { Tolerance = 0.2f, Softness = 0.05f },
        new BitmapMask(Stamp) { Channel = MaskChannel.Luminance },
        new EllipseMask(new RectangleF(0, 0, 60, 60)).Subtract(new RectangleMask(new RectangleF(20, 20, 20, 20))),
    }.Select(mask => new object[] { mask });

    private static IEnumerable<object[]> AllLayers() => new Layer[]
    {
        new ImageLayer { Image = Stamp, Bounds = new RectangleF(20, 10, 64, 48), Rotation = 15, Pipeline = ImagePipeline.Create().Grayscale(), Filter = ResamplingFilter.Bilinear, Name = "Logo" },
        new FillLayer(new Rgba32(30, 60, 200, 128)) { BlendMode = BlendMode.Color, Opacity = 0.8f, Mask = Mask.Gradient(new(0, 1), new(0, 0.4f)) },
        new AdjustmentLayer(ImagePipeline.Create().Contrast(0.3f).Vibrance(0.2f)) { Mask = new LuminanceRangeMask(0, 0.4f), IsVisible = false, IsLocked = true },
    }.Select(layer => new object[] { layer });

    private static IEnumerable<object[]> AllAnnotations() => new Annotation[]
    {
        new RectangleAnnotation { Rectangle = new RectangleF(10, 10, 50, 30), CornerRadius = 4, ZIndex = 2, Tag = "box", Style = ShapeStyle.Default with { FillColor = new Rgba32(1, 2, 3, 100) } },
        new EllipseAnnotation { Rectangle = new RectangleF(30, 20, 40, 40), Rotation = 30, Opacity = 0.5f },
        new LineAnnotation { Start = new Vector2(1, 2), End = new Vector2(90, 70), Style = ShapeStyle.Default with { Stroke = new StrokeStyle { Width = 5 }.WithDashes(6, 3) } },
        new ArrowAnnotation { Start = new Vector2(5, 80), End = new Vector2(100, 10), HeadLengthFactor = 5 },
        new FreehandAnnotation { Points = [new(10, 10), new(20, 30), new(40, 25), new(60, 50)], Closed = true },
        new PolygonAnnotation { Points = [new(10, 10), new(60, 15), new(30, 60)], Closed = false, IsLocked = true },
        new TextAnnotation { Text = "Hello\nworld", Position = new Vector2(20, 40), BackgroundColor = Rgba32.White, MeasuredSize = new SizeF(80, 40), Style = new TextStyle { FontSize = 22, Bold = true, MaxWidth = 120, Alignment = TextAlignment.Center } },
        new HighlightAnnotation { Rectangle = new RectangleF(0, 0, 40, 10) },
        new RedactionAnnotation { Rectangle = new RectangleF(50, 50, 30, 20), Mode = RedactionMode.Pixelate, Strength = 8 },
        new CalloutAnnotation { Bubble = new RectangleF(60, 5, 60, 30), Target = new Vector2(20, 70), Text = "Look" },
        new StepAnnotation { Center = new Vector2(100, 80), Number = 3, IsVisible = false },
        new ImageAnnotation { Image = Stamp, Rectangle = new RectangleF(70, 60, 32, 24) },
    }.Select(annotation => new object[] { annotation });

    [TestMethod]
    [DynamicData(nameof(AllOperations))]
    public void EveryOperationRoundTripsAndRendersTheSamePixels(IImageOperation operation)
    {
        var json = BmageJson.SerializeObject(operation);
        var back = BmageJson.DeserializeObject<IImageOperation>(json);

        Assert.AreEqual(operation.GetType(), back.GetType());
        Assert.AreEqual(json, BmageJson.SerializeObject(back), "Reading and writing again changed the JSON.");
        using var source = ParallelProcessingTests.Noise(160, 120, seed: 3);
        using var expected = operation.Apply(source, OperationContext.Default);
        using var actual = back.Apply(source, OperationContext.Default);
        Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes), $"{operation.Name} renders differently after a round trip.");
    }

    [TestMethod]
    [DynamicData(nameof(AllMasks))]
    public void EveryMaskRoundTripsAndRendersTheSameCoverage(Mask mask)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(mask, options);
        var back = BmageJson.DeserializeObject<Mask>(json, options);

        Assert.AreEqual(json, BmageJson.SerializeObject(back, options));
        using var image = ParallelProcessingTests.Noise(140, 100, seed: 9);
        using var expected = mask.Render(image.Size, image);
        using var actual = back.Render(image.Size, image);
        Assert.IsTrue(expected.Values.SequenceEqual(actual.Values), $"The {mask.Kind} mask renders differently after a round trip.");
    }

    [TestMethod]
    [DynamicData(nameof(AllLayers))]
    public void EveryLayerRoundTripsAndCompositesTheSame(Layer layer)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(layer, options);
        var back = BmageJson.DeserializeObject<Layer>(json, options);

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
    public void EveryAnnotationRoundTripsAsAnEqualRecord(Annotation annotation)
    {
        var options = WithImages();
        var json = BmageJson.SerializeObject(annotation, options);
        var back = BmageJson.DeserializeObject<Annotation>(json, options);

        Assert.AreEqual(json, BmageJson.SerializeObject(back, options));
        // Records compare by value, apart from point lists and styles holding arrays, which the JSON comparison covers.
        Assert.AreEqual(annotation.Id, back.Id);
        Assert.AreEqual(annotation.GetBounds(), back.GetBounds());
    }

    [TestMethod]
    public void EveryPublicEditTypeHasACodec()
    {
        var kinds = new[] { typeof(IImageOperation), typeof(Annotation), typeof(Layer), typeof(Mask) };
        var missing = typeof(ImageBuffer).Assembly.GetExportedTypes()
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
        var document = ImageDocument.Create(source)
            .AddOperation(new CropOperation(10, 10, 180, 130))
            .AddOperation(new MaskedOperation(new PipelineOperation(ImagePipeline.Create().Exposure(-0.4f).Saturation(0.2f)), Mask.Gradient(new(0, 0), new(0, 0.5f))))
            .AddOperation(new SharpenFilter(0.4f))
            .AddLayer(new ImageLayer { Image = Stamp, Bounds = new RectangleF(100, 80, 48, 36), BlendMode = BlendMode.Multiply })
            .AddLayer(new AdjustmentLayer(ImagePipeline.Create().Vibrance(0.3f)) { Mask = Mask.Ellipse() with { Feather = 6 } })
            .AddAnnotation(new ArrowAnnotation { Start = new Vector2(10, 100), End = new Vector2(90, 30) })
            .AddAnnotation(new RedactionAnnotation { Rectangle = new RectangleF(120, 10, 40, 20) });

        var options = WithImages();
        var json = BmageJson.Serialize(document, options);
        var back = BmageJson.DeserializeDocument(json, options);

        Assert.AreEqual(document.SourceSize, back.SourceSize);
        Assert.AreEqual(json, BmageJson.Serialize(back, options));
        using var expected = new DocumentRenderer().Render(document, source);
        using var actual = new DocumentRenderer().Render(back, source);
        FastPathEquivalenceTests.AssertSamePixels(expected, actual, "document round trip");
    }

    [TestMethod]
    public void APipelineRoundTripsOnItsOwn()
    {
        var pipeline = ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f).ApplyMask(Mask.Ellipse());
        var json = BmageJson.Serialize(pipeline);
        var back = BmageJson.DeserializePipeline(json);

        Assert.AreEqual(pipeline.ToString(), back.ToString());
        StringAssert.Contains(json, "\"format\":\"bmage-pipeline\"");
    }

    [TestMethod]
    public void TheFormatIsReadableJson()
    {
        var json = BmageJson.Serialize(ImagePipeline.Create().Crop(1, 2, 30, 40).Brightness(0.5f), new BmageJsonOptions { Indented = true });
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
        var annotation = new ImageAnnotation { Image = Stamp, Rectangle = new RectangleF(0, 0, 16, 12) };

        Assert.ThrowsExactly<NotSupportedException>(() => BmageJson.SerializeObject(annotation));
        var json = BmageJson.SerializeObject(annotation, WithImages());
        StringAssert.Contains(json, "\"image\":\"stamp\"");
        Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<Annotation>(json));
        Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<Annotation>(json, new BmageJsonOptions { ImageResolver = _ => null }));
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
            var ex = Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeDocument(json));
            StringAssert.Contains(ex.Message, expected);
        }
    }

    [TestMethod]
    public void OptionalPropertiesTakeTheirDefaults()
    {
        var back = BmageJson.DeserializePipeline("""{"format":"bmage-pipeline","version":1,"operations":[{"type":"sharpen"},{"type":"rotate","degrees":10}]}""");

        var sharpen = (SharpenFilter)back[0];
        Assert.AreEqual(0.5f, sharpen.Amount);
        Assert.AreEqual(1f, sharpen.Radius);
        var rotate = (RotateOperation)back[1];
        Assert.IsTrue(rotate.ExpandCanvas);
        Assert.AreEqual(WarpSampling.Bicubic, rotate.Sampling);
    }

    [TestMethod]
    public void ACustomOperationNeedsACodec_AndRoundTripsWithOne()
    {
        var duotone = new Duotone(new Rgba32(10, 20, 60), new Rgba32(250, 230, 200));
        Assert.ThrowsExactly<NotSupportedException>(() => BmageJson.Serialize(ImagePipeline.Create().Apply(duotone)));

        var options = new BmageJsonOptions().Add<Duotone>("test.duotone",
            (writer, d) => writer.Write("shadow", d.Shadow).Write("highlight", d.Highlight),
            reader => new Duotone(reader.GetColor("shadow"), reader.GetColor("highlight")));
        var json = BmageJson.Serialize(ImagePipeline.Create().Brightness(0.1f).Apply(duotone), options);
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
        var levels = BmageJson.DeserializeObject<IImageOperation>("""{"type":"levels","channels":"Red, Green, Blue"}""");
        Assert.AreEqual(ColorChannels.Rgb, ((LevelsAdjustment)levels).Channels, "A set of flags may combine its members.");

        static void AssertMode(string mode, bool ok)
        {
            var json = $$"""{"type":"fill","color":"#ff0000ff","blendMode":"{{mode}}"}""";
            if (ok) Assert.AreEqual(BlendMode.Multiply, BmageJson.DeserializeObject<Layer>(json).BlendMode);
            else Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<Layer>(json), mode);
        }
    }

    [TestMethod]
    public void WholeNumbersAreReadExactly()
    {
        var crop = (CropOperation)BmageJson.DeserializeObject<IImageOperation>("""{"type":"crop","rectangle":[16777217,0,2147483647,100]}""");
        Assert.AreEqual(new Rectangle(16777217, 0, int.MaxValue, 100), crop.Rectangle);
        Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<IImageOperation>("""{"type":"crop","rectangle":[0,0,2147483648,100]}"""));
        Assert.ThrowsExactly<ImageDocumentFormatException>(() => BmageJson.DeserializeObject<IImageOperation>("""{"type":"crop","rectangle":[0,0,10.5,100]}"""));
    }

    [TestMethod]
    public void AnUndefinedEnumValueIsNotWritten()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BmageJson.SerializeObject(new FillLayer(Rgba32.Black) { BlendMode = (BlendMode)99 }));
    }

    private sealed class MyMatrix() : ColorMatrixOperation(ColorMatrix.Saturate(0.5f), "Mine");

    private sealed class Duotone(Rgba32 shadow, Rgba32 highlight) : PointOperation
    {
        public Rgba32 Shadow { get; } = shadow;
        public Rgba32 Highlight { get; } = highlight;
        public override string Name => "Duotone";
        public override void ProcessRow(Span<Vector4> pixels) { }
    }
}
