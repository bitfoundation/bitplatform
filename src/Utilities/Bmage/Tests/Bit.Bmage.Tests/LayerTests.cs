using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

/// <summary>
/// Blend modes follow the W3C Compositing and Blending specification, and layers composite with them between the
/// document's pipeline and its annotations.
/// </summary>
[TestClass]
public sealed class LayerTests
{
    // ------------------------------------------------------------------ blend modes

    [TestMethod]
    [DataRow(BmageBlendMode.Multiply, 255, 128)]
    [DataRow(BmageBlendMode.Multiply, 0, 0)]
    [DataRow(BmageBlendMode.Screen, 0, 128)]
    [DataRow(BmageBlendMode.Screen, 255, 255)]
    [DataRow(BmageBlendMode.Darken, 200, 128)]
    [DataRow(BmageBlendMode.Lighten, 200, 200)]
    [DataRow(BmageBlendMode.Difference, 128, 0)]
    [DataRow(BmageBlendMode.Difference, 0, 128)]
    [DataRow(BmageBlendMode.Exclusion, 0, 128)]
    [DataRow(BmageBlendMode.Add, 100, 228)]
    [DataRow(BmageBlendMode.Add, 200, 255)]
    [DataRow(BmageBlendMode.ColorDodge, 0, 128)]
    [DataRow(BmageBlendMode.ColorBurn, 255, 128)]
    [DataRow(BmageBlendMode.HardLight, 128, 128)]
    [DataRow(BmageBlendMode.Overlay, 128, 128)]
    public void SeparableModesComputeTheirBlendFunction(BmageBlendMode mode, int source, int expected)
    {
        var result = BmageCompositor.Blend(new BmageRgba32(128, 128, 128), new BmageRgba32((byte)source, (byte)source, (byte)source), mode);
        Assert.IsTrue(Math.Abs(result.R - expected) <= 1, $"{mode} of {source} over 128 gave {result.R}, expected {expected}.");
        Assert.AreEqual(255, result.A);
    }

    [TestMethod]
    public void SoftLightMatchesTheSpecification()
    {
        // Cs <= 0.5: B = Cb - (1 - 2Cs) Cb (1 - Cb); Cs > 0.5 with Cb > 0.25: B = Cb + (2Cs - 1)(sqrt(Cb) - Cb).
        var darker = BmageCompositor.Blend(new BmageRgba32(128, 128, 128), new BmageRgba32(64, 64, 64), BmageBlendMode.SoftLight);
        var cb = 128 / 255.0; var cs = 64 / 255.0;
        Assert.IsTrue(Math.Abs(darker.R - (cb - (1 - 2 * cs) * cb * (1 - cb)) * 255) <= 1);
        var lighter = BmageCompositor.Blend(new BmageRgba32(128, 128, 128), new BmageRgba32(200, 200, 200), BmageBlendMode.SoftLight);
        cs = 200 / 255.0;
        Assert.IsTrue(Math.Abs(lighter.R - (cb + (2 * cs - 1) * (Math.Sqrt(cb) - cb)) * 255) <= 1);
    }

    [TestMethod]
    public void EveryModeShowsTheSourceUnchangedOverTransparency()
    {
        var source = new BmageRgba32(30, 140, 220, 255);
        foreach (var mode in Enum.GetValues<BmageBlendMode>())
        {
            if (mode is BmageBlendMode.Erase) continue;
            Assert.AreEqual(source, BmageCompositor.Blend(BmageRgba32.Transparent, source, mode), mode.ToString());
        }
    }

    [TestMethod]
    public void SourceOverIsTheClassicComposite()
    {
        var random = new Random(3);
        for (var i = 0; i < 500; i++)
        {
            var d = new BmageRgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            var s = new BmageRgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            var alpha = (float)random.NextDouble();
            Assert.AreEqual(BmageCompositor.SourceOver(d, s, alpha), BmageCompositor.Blend(d, s, BmageBlendMode.SourceOver, alpha));
        }
    }

    [TestMethod]
    public void TheNonSeparableModesTradeHueSaturationAndLuminosity()
    {
        var grey = new BmageRgba32(128, 128, 128);
        var red = new BmageRgba32(220, 30, 30);

        // Colour keeps the destination's luminosity and takes the source's hue: a grey becomes a red of the same lightness.
        var tinted = BmageCompositor.Blend(grey, red, BmageBlendMode.Color);
        Assert.IsTrue(tinted.R > tinted.G && tinted.R > tinted.B, $"{tinted}");
        Assert.IsTrue(Math.Abs(Lum(tinted) - Lum(grey)) <= 2, $"luminosity {Lum(tinted)} vs {Lum(grey)}");

        // Luminosity is the reverse: the red takes the grey's lightness and keeps its hue.
        var relit = BmageCompositor.Blend(red, grey, BmageBlendMode.Luminosity);
        Assert.IsTrue(Math.Abs(Lum(relit) - Lum(grey)) <= 2);
        Assert.IsTrue(relit.R > relit.G);

        // A grey source has no saturation, so Saturation turns any colour grey.
        var desaturated = BmageCompositor.Blend(red, grey, BmageBlendMode.Saturation);
        Assert.IsTrue(Math.Abs(desaturated.R - desaturated.G) <= 1 && Math.Abs(desaturated.G - desaturated.B) <= 1, $"{desaturated}");

        static double Lum(BmageRgba32 c) => 0.3 * c.R + 0.59 * c.G + 0.11 * c.B;
    }

    [TestMethod]
    public void TheCanvasDrawsImagesWithEveryMode()
    {
        using var canvas = BmageImageCanvas.Create(4, 4, new BmageRgba32(100, 100, 100));
        using var stamp = BmageImageBuffer.Create(2, 2, new BmageRgba32(200, 200, 200));
        canvas.DrawImage(stamp, new Vector2(0, 0), BmageBlendMode.Multiply);

        Assert.AreEqual(BmageCompositor.Blend(new BmageRgba32(100, 100, 100), new BmageRgba32(200, 200, 200), BmageBlendMode.Multiply), canvas.Buffer[0, 0]);
        Assert.AreEqual(new BmageRgba32(100, 100, 100), canvas.Buffer[3, 3]);
    }

    // ------------------------------------------------------------------ layers

    [TestMethod]
    public void AnImageLayerIsPlacedInItsBounds()
    {
        using var stamp = BmageImageBuffer.Create(10, 10, new BmageRgba32(0, 200, 0));
        using var result = BmageLayerCompositor.Flatten(new Size(50, 50), BmageRgba32.White, [new BmageImageLayer { Image = stamp, Bounds = new RectangleF(20, 20, 10, 10) }]);

        Assert.AreEqual(new BmageRgba32(0, 200, 0), result[25, 25]);
        Assert.AreEqual(BmageRgba32.White, result[10, 10]);
        Assert.AreEqual(BmageRgba32.White, result[31, 31]);
    }

    [TestMethod]
    public void AMaskedImageLayerOnlyShowsWhereItsMaskCovers()
    {
        using var stamp = BmageImageBuffer.Create(40, 40, new BmageRgba32(0, 0, 255));
        var layer = new BmageImageLayer { Image = stamp, Bounds = new RectangleF(0, 0, 40, 40), Mask = new BmageRectangleMask(new RectangleF(0, 0, 20, 40)) };
        using var result = BmageLayerCompositor.Flatten(new Size(40, 40), BmageRgba32.White, [layer]);

        Assert.AreEqual(new BmageRgba32(0, 0, 255), result[5, 20]);
        Assert.AreEqual(BmageRgba32.White, result[30, 20]);
    }

    [TestMethod]
    public void TheMaskedAndUnmaskedPathsOfAnImageLayerAgree()
    {
        using var photo = ParallelProcessingTests.Noise(60, 40, seed: 31);
        using var background = ParallelProcessingTests.Noise(100, 80, seed: 32);
        foreach (var mode in new[] { BmageBlendMode.SourceOver, BmageBlendMode.Multiply, BmageBlendMode.Screen, BmageBlendMode.Color })
        foreach (var rotation in new[] { 0f, 20f })
        {
            var plain = new BmageImageLayer { Image = photo, Bounds = new RectangleF(15.5f, 10, 70, 50), Rotation = rotation, BlendMode = mode, Opacity = 0.8f };
            using var unmasked = background.Clone();
            plain.Composite(unmasked);
            using var masked = background.Clone();
            (plain with { Mask = BmageMask.All() }).Composite(masked);
            FastPathEquivalenceTests.AssertSamePixels(unmasked, masked, $"{mode}, rotated {rotation}");
        }
    }

    [TestMethod]
    public void AFillLayerTintsThroughItsBlendMode()
    {
        using var image = BmageImageBuffer.Create(8, 8, new BmageRgba32(200, 200, 200));
        new BmageFillLayer(new BmageRgba32(255, 0, 0)) { BlendMode = BmageBlendMode.Multiply }.Composite(image);

        Assert.AreEqual(new BmageRgba32(200, 0, 0), image[3, 3]);
    }

    [TestMethod]
    public void AFullAdjustmentLayerIsExactlyItsPipeline()
    {
        using var source = ParallelProcessingTests.Noise(120, 90, seed: 40);
        var pipeline = BmageImagePipeline.Create().Contrast(0.3f).Saturation(-0.4f).Blur(1.5f);

        using var expected = pipeline.Execute(source);
        using var actual = source.Clone();
        new BmageAdjustmentLayer(pipeline).Composite(actual);

        Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes));
    }

    [TestMethod]
    public void AMaskedAdjustmentLayerMatchesTheMaskedOperation()
    {
        using var source = ParallelProcessingTests.Noise(120, 90, seed: 41);
        var mask = new BmageEllipseMask(new RectangleF(10, 10, 90, 60)) { Feather = 4 };
        var pipeline = BmageImagePipeline.Create().Exposure(0.6f);

        using var expected = new BmageMaskedOperation(new BmagePipelineOperation(pipeline), mask).Apply(source, BmageOperationContext.Default);
        using var actual = source.Clone();
        new BmageAdjustmentLayer(pipeline) { Mask = mask }.Composite(actual);

        FastPathEquivalenceTests.AssertSamePixels(expected, actual, "masked adjustment layer");
    }

    [TestMethod]
    public void AnAdjustmentLayerAtHalfOpacityMixesHalfWay()
    {
        using var image = BmageImageBuffer.Create(4, 4, new BmageRgba32(100, 100, 100));
        new BmageAdjustmentLayer(BmageImagePipeline.Create().Invert()) { Opacity = 0.5f }.Composite(image);

        Assert.IsTrue(Math.Abs(image[0, 0].R - 128) <= 1, $"{image[0, 0]}");
    }

    [TestMethod]
    public void AnEmptyAdjustmentLayerInMultiplyDarkensTheImageByItself()
    {
        using var image = BmageImageBuffer.Create(4, 4, new BmageRgba32(128, 128, 128));
        new BmageAdjustmentLayer(BmageImagePipeline.Empty) { BlendMode = BmageBlendMode.Multiply }.Composite(image);

        Assert.AreEqual(64, image[0, 0].R);
    }

    [TestMethod]
    public void AnAdjustmentLayerThatChangesTheSizeIsRefused()
    {
        using var image = BmageImageBuffer.Create(40, 40);
        Assert.ThrowsExactly<InvalidOperationException>(() => new BmageAdjustmentLayer(BmageImagePipeline.Create().Crop(0, 0, 10, 10)).Composite(image));
    }

    [TestMethod]
    public void HiddenAndTransparentLayersDoNothing()
    {
        using var image = ParallelProcessingTests.Noise(20, 20, seed: 1);
        var before = image.ToArray();
        new BmageFillLayer(BmageRgba32.Black) { IsVisible = false }.Composite(image);
        new BmageFillLayer(BmageRgba32.Black) { Opacity = 0 }.Composite(image);
        new BmageFillLayer(BmageRgba32.Black) { Mask = BmageMask.All() with { Opacity = 0 } }.Composite(image);

        Assert.IsTrue(before.AsSpan().SequenceEqual(image.Bytes));
    }

    [TestMethod]
    public void LayersCompositeOnManyThreadsWithIdenticalPixels()
    {
        using var photo = ParallelProcessingTests.Noise(400, 300, seed: 7);
        using var background = ParallelProcessingTests.Noise(1024, 768, seed: 8);
        BmageLayer[] layers =
        [
            new BmageImageLayer { Image = photo, Bounds = new RectangleF(100, 80, 700, 520), BlendMode = BmageBlendMode.Overlay, Mask = BmageMask.Ellipse() with { Feather = 10 } },
            new BmageFillLayer(new BmageRgba32(20, 40, 120, 180)) { BlendMode = BmageBlendMode.Color, Mask = BmageMask.Gradient(new(0, 0), new(0, 1)) },
            new BmageAdjustmentLayer(BmageImagePipeline.Create().Contrast(0.2f).Vibrance(0.3f)) { Mask = new BmageLuminanceRangeMask(0f, 0.4f) },
        ];

        using var one = background.Clone();
        BmageLayerCompositor.Composite(one, layers, new BmageOperationContext(maxDegreeOfParallelism: 1));
        using var many = background.Clone();
        BmageLayerCompositor.Composite(many, layers, new BmageOperationContext(maxDegreeOfParallelism: 8));

        Assert.IsTrue(one.Bytes.SequenceEqual(many.Bytes));
    }

    // ------------------------------------------------------------------ documents

    [TestMethod]
    public void ADocumentRendersItsPipelineThenItsLayersThenItsAnnotations()
    {
        using var source = BmageImageBuffer.Create(40, 40, new BmageRgba32(100, 100, 100));
        var document = BmageImageDocument.Create(source)
            .AddOperation(new BmageBrightnessAdjustment(0.2f))
            .AddLayer(new BmageFillLayer(new BmageRgba32(255, 0, 0)) { Mask = new BmageRectangleMask(new RectangleF(0, 0, 20, 40)) })
            .AddAnnotation(new BmageRectangleAnnotation { Rectangle = new RectangleF(0, 0, 40, 10), Style = BmageShapeStyle.Default with { FillColor = BmageRgba32.Black, StrokeColor = null } });

        using var rendered = new BmageDocumentRenderer().Render(document, source);

        Assert.AreEqual(BmageRgba32.Black, rendered[30, 5], "The annotation is drawn over everything.");
        Assert.AreEqual(new BmageRgba32(255, 0, 0), rendered[5, 30], "The fill layer covers its masked half.");
        Assert.AreEqual(new BmageBrightnessAdjustment(0.2f).Apply(source, BmageOperationContext.Default)[30, 30], rendered[30, 30], "The pipeline shows elsewhere.");
    }

    [TestMethod]
    public void LayerEditsAreValuesWithUndo()
    {
        var fill = new BmageFillLayer(BmageRgba32.Black);
        var adjustment = new BmageAdjustmentLayer(BmageImagePipeline.Create().Grayscale());
        var document = BmageImageDocument.Create(new Size(10, 10)).AddLayer(fill).AddLayer(adjustment);

        Assert.AreEqual(2, document.Layers.Count);
        Assert.IsFalse(document.IsEmpty);
        var moved = document.MoveLayer(adjustment.Id, 0);
        Assert.AreSame(adjustment, moved.Layers[0]);
        var faded = moved.UpdateLayer(fill with { Opacity = 0.3f });
        Assert.AreEqual(0.3f, faded.FindLayer(fill.Id)!.Opacity);
        Assert.AreEqual(1f, document.FindLayer(fill.Id)!.Opacity, "The original document changed.");
        Assert.AreEqual(1, faded.RemoveLayer(fill.Id).Layers.Count);
        Assert.AreSame(document, document.UpdateLayer(fill), "Replacing a layer with itself should change nothing.");
    }

    [TestMethod]
    public void LayersScaleWithTheDocument()
    {
        using var stamp = BmageImageBuffer.Create(10, 10);
        var document = BmageImageDocument.Create(new Size(400, 300))
            .AddLayer(new BmageImageLayer { Image = stamp, Bounds = new RectangleF(100, 50, 200, 100), Mask = new BmageRectangleMask(new RectangleF(100, 50, 100, 100)) });

        var scaled = (BmageImageLayer)document.ForScale(0.5).Layers[0];

        Assert.AreEqual(new RectangleF(50, 25, 100, 50), scaled.Bounds);
        Assert.AreEqual(new RectangleF(50, 25, 50, 50), ((BmageRectangleMask)scaled.Mask!).Rectangle);
    }

    [TestMethod]
    public async Task ALayeredPreviewMatchesAFreshRender_AndReusesTheCompositeForAnnotationEdits()
    {
        using var source = FastPathEquivalenceTests.Noise(300, 200, seed: 51);
        using var session = new BmageImageEditSession(source.Clone(), new BmageImageEditSessionOptions { MaxProxyDimension = 150 });
        var counting = new CountingLayer();
        session.AddOperation(new BmageBlurFilter(2f));
        session.Apply(d => d.AddLayer(counting), "layer");
        session.Apply(d => d.AddLayer(new BmageAdjustmentLayer(BmageImagePipeline.Create().Contrast(0.3f)) { Mask = BmageMask.Ellipse() }), "adjust");
        var rectangle = new BmageRectangleAnnotation { Rectangle = new RectangleF(20, 30, 120, 60) };
        session.AddAnnotation(rectangle);

        var before = counting.Composited;
        var preview = await session.RenderPreviewAsync();
        Assert.AreEqual(before + 1, counting.Composited);
        using (var expected = await FreshRender(source, session.Document, 150))
            FastPathEquivalenceTests.AssertSamePixels(expected, preview, "layered preview");

        // Moving the annotation draws it again over the kept composite: the layers are not composited again.
        session.Apply(d => d.UpdateAnnotation(rectangle.Translate(30, 10)), "move");
        before = counting.Composited;
        preview = await session.RenderPreviewAsync();
        Assert.AreEqual(before, counting.Composited, "The layers were composited again for an annotation edit.");
        using (var expected = await FreshRender(source, session.Document, 150))
            FastPathEquivalenceTests.AssertSamePixels(expected, preview, "moved annotation");

        // Editing a layer composites again.
        session.Apply(d => d.UpdateLayer(d.Layers[1] with { Opacity = 0.5f }), "fade");
        before = counting.Composited;
        preview = await session.RenderPreviewAsync();
        Assert.AreEqual(before + 1, counting.Composited);
        using (var expected = await FreshRender(source, session.Document, 150))
            FastPathEquivalenceTests.AssertSamePixels(expected, preview, "faded layer");
    }

    private static async Task<BmageImageBuffer> FreshRender(BmageImageBuffer source, BmageImageDocument document, int maxProxyDimension)
    {
        using var fresh = new BmageImageEditSession(source.Clone(), new BmageImageEditSessionOptions { MaxProxyDimension = maxProxyDimension });
        fresh.Apply(_ => document, "same document");
        return (await fresh.RenderPreviewAsync()).Clone();
    }

    /// <summary>A layer that darkens slightly and counts how often it is composited, shared by every copy of it.</summary>
    private sealed record CountingLayer : BmageLayer
    {
        private readonly int[] _count = [0];
        public int Composited => _count[0];
        public override string Kind => "Counting";

        protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context)
        {
            Interlocked.Increment(ref _count[0]);
            var pixels = target.Pixels;
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new BmageRgba32((byte)(pixels[i].R / 2), pixels[i].G, pixels[i].B, pixels[i].A);
        }

        protected override BmageLayer ScaleCore(double scale) => this;
    }
}
