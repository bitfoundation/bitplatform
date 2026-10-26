using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Pipeline;

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
    [DataRow(BlendMode.Multiply, 255, 128)]
    [DataRow(BlendMode.Multiply, 0, 0)]
    [DataRow(BlendMode.Screen, 0, 128)]
    [DataRow(BlendMode.Screen, 255, 255)]
    [DataRow(BlendMode.Darken, 200, 128)]
    [DataRow(BlendMode.Lighten, 200, 200)]
    [DataRow(BlendMode.Difference, 128, 0)]
    [DataRow(BlendMode.Difference, 0, 128)]
    [DataRow(BlendMode.Exclusion, 0, 128)]
    [DataRow(BlendMode.Add, 100, 228)]
    [DataRow(BlendMode.Add, 200, 255)]
    [DataRow(BlendMode.ColorDodge, 0, 128)]
    [DataRow(BlendMode.ColorBurn, 255, 128)]
    [DataRow(BlendMode.HardLight, 128, 128)]
    [DataRow(BlendMode.Overlay, 128, 128)]
    public void SeparableModesComputeTheirBlendFunction(BlendMode mode, int source, int expected)
    {
        var result = Compositor.Blend(new Rgba32(128, 128, 128), new Rgba32((byte)source, (byte)source, (byte)source), mode);
        Assert.IsTrue(Math.Abs(result.R - expected) <= 1, $"{mode} of {source} over 128 gave {result.R}, expected {expected}.");
        Assert.AreEqual(255, result.A);
    }

    [TestMethod]
    public void SoftLightMatchesTheSpecification()
    {
        // Cs <= 0.5: B = Cb - (1 - 2Cs) Cb (1 - Cb); Cs > 0.5 with Cb > 0.25: B = Cb + (2Cs - 1)(sqrt(Cb) - Cb).
        var darker = Compositor.Blend(new Rgba32(128, 128, 128), new Rgba32(64, 64, 64), BlendMode.SoftLight);
        var cb = 128 / 255.0; var cs = 64 / 255.0;
        Assert.IsTrue(Math.Abs(darker.R - (cb - (1 - 2 * cs) * cb * (1 - cb)) * 255) <= 1);
        var lighter = Compositor.Blend(new Rgba32(128, 128, 128), new Rgba32(200, 200, 200), BlendMode.SoftLight);
        cs = 200 / 255.0;
        Assert.IsTrue(Math.Abs(lighter.R - (cb + (2 * cs - 1) * (Math.Sqrt(cb) - cb)) * 255) <= 1);
    }

    [TestMethod]
    public void EveryModeShowsTheSourceUnchangedOverTransparency()
    {
        var source = new Rgba32(30, 140, 220, 255);
        foreach (var mode in Enum.GetValues<BlendMode>())
        {
            if (mode is BlendMode.Erase) continue;
            Assert.AreEqual(source, Compositor.Blend(Rgba32.Transparent, source, mode), mode.ToString());
        }
    }

    [TestMethod]
    public void SourceOverIsTheClassicComposite()
    {
        var random = new Random(3);
        for (var i = 0; i < 500; i++)
        {
            var d = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            var s = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            var alpha = (float)random.NextDouble();
            Assert.AreEqual(Compositor.SourceOver(d, s, alpha), Compositor.Blend(d, s, BlendMode.SourceOver, alpha));
        }
    }

    [TestMethod]
    public void TheNonSeparableModesTradeHueSaturationAndLuminosity()
    {
        var grey = new Rgba32(128, 128, 128);
        var red = new Rgba32(220, 30, 30);

        // Colour keeps the destination's luminosity and takes the source's hue: a grey becomes a red of the same lightness.
        var tinted = Compositor.Blend(grey, red, BlendMode.Color);
        Assert.IsTrue(tinted.R > tinted.G && tinted.R > tinted.B, $"{tinted}");
        Assert.IsTrue(Math.Abs(Lum(tinted) - Lum(grey)) <= 2, $"luminosity {Lum(tinted)} vs {Lum(grey)}");

        // Luminosity is the reverse: the red takes the grey's lightness and keeps its hue.
        var relit = Compositor.Blend(red, grey, BlendMode.Luminosity);
        Assert.IsTrue(Math.Abs(Lum(relit) - Lum(grey)) <= 2);
        Assert.IsTrue(relit.R > relit.G);

        // A grey source has no saturation, so Saturation turns any colour grey.
        var desaturated = Compositor.Blend(red, grey, BlendMode.Saturation);
        Assert.IsTrue(Math.Abs(desaturated.R - desaturated.G) <= 1 && Math.Abs(desaturated.G - desaturated.B) <= 1, $"{desaturated}");

        static double Lum(Rgba32 c) => 0.3 * c.R + 0.59 * c.G + 0.11 * c.B;
    }

    [TestMethod]
    public void TheCanvasDrawsImagesWithEveryMode()
    {
        using var canvas = ImageCanvas.Create(4, 4, new Rgba32(100, 100, 100));
        using var stamp = ImageBuffer.Create(2, 2, new Rgba32(200, 200, 200));
        canvas.DrawImage(stamp, new Vector2(0, 0), BlendMode.Multiply);

        Assert.AreEqual(Compositor.Blend(new Rgba32(100, 100, 100), new Rgba32(200, 200, 200), BlendMode.Multiply), canvas.Buffer[0, 0]);
        Assert.AreEqual(new Rgba32(100, 100, 100), canvas.Buffer[3, 3]);
    }

    // ------------------------------------------------------------------ layers

    [TestMethod]
    public void AnImageLayerIsPlacedInItsBounds()
    {
        using var stamp = ImageBuffer.Create(10, 10, new Rgba32(0, 200, 0));
        using var result = LayerCompositor.Flatten(new Size(50, 50), Rgba32.White, [new ImageLayer { Image = stamp, Bounds = new RectangleF(20, 20, 10, 10) }]);

        Assert.AreEqual(new Rgba32(0, 200, 0), result[25, 25]);
        Assert.AreEqual(Rgba32.White, result[10, 10]);
        Assert.AreEqual(Rgba32.White, result[31, 31]);
    }

    [TestMethod]
    public void AMaskedImageLayerOnlyShowsWhereItsMaskCovers()
    {
        using var stamp = ImageBuffer.Create(40, 40, new Rgba32(0, 0, 255));
        var layer = new ImageLayer { Image = stamp, Bounds = new RectangleF(0, 0, 40, 40), Mask = new RectangleMask(new RectangleF(0, 0, 20, 40)) };
        using var result = LayerCompositor.Flatten(new Size(40, 40), Rgba32.White, [layer]);

        Assert.AreEqual(new Rgba32(0, 0, 255), result[5, 20]);
        Assert.AreEqual(Rgba32.White, result[30, 20]);
    }

    [TestMethod]
    public void TheMaskedAndUnmaskedPathsOfAnImageLayerAgree()
    {
        using var photo = ParallelProcessingTests.Noise(60, 40, seed: 31);
        using var background = ParallelProcessingTests.Noise(100, 80, seed: 32);
        foreach (var mode in new[] { BlendMode.SourceOver, BlendMode.Multiply, BlendMode.Screen, BlendMode.Color })
        foreach (var rotation in new[] { 0f, 20f })
        {
            var plain = new ImageLayer { Image = photo, Bounds = new RectangleF(15.5f, 10, 70, 50), Rotation = rotation, BlendMode = mode, Opacity = 0.8f };
            using var unmasked = background.Clone();
            plain.Composite(unmasked);
            using var masked = background.Clone();
            (plain with { Mask = Mask.All() }).Composite(masked);
            FastPathEquivalenceTests.AssertSamePixels(unmasked, masked, $"{mode}, rotated {rotation}");
        }
    }

    [TestMethod]
    public void AFillLayerTintsThroughItsBlendMode()
    {
        using var image = ImageBuffer.Create(8, 8, new Rgba32(200, 200, 200));
        new FillLayer(new Rgba32(255, 0, 0)) { BlendMode = BlendMode.Multiply }.Composite(image);

        Assert.AreEqual(new Rgba32(200, 0, 0), image[3, 3]);
    }

    [TestMethod]
    public void AFullAdjustmentLayerIsExactlyItsPipeline()
    {
        using var source = ParallelProcessingTests.Noise(120, 90, seed: 40);
        var pipeline = ImagePipeline.Create().Contrast(0.3f).Saturation(-0.4f).Blur(1.5f);

        using var expected = pipeline.Execute(source);
        using var actual = source.Clone();
        new AdjustmentLayer(pipeline).Composite(actual);

        Assert.IsTrue(expected.Bytes.SequenceEqual(actual.Bytes));
    }

    [TestMethod]
    public void AMaskedAdjustmentLayerMatchesTheMaskedOperation()
    {
        using var source = ParallelProcessingTests.Noise(120, 90, seed: 41);
        var mask = new EllipseMask(new RectangleF(10, 10, 90, 60)) { Feather = 4 };
        var pipeline = ImagePipeline.Create().Exposure(0.6f);

        using var expected = new MaskedOperation(new PipelineOperation(pipeline), mask).Apply(source, OperationContext.Default);
        using var actual = source.Clone();
        new AdjustmentLayer(pipeline) { Mask = mask }.Composite(actual);

        FastPathEquivalenceTests.AssertSamePixels(expected, actual, "masked adjustment layer");
    }

    [TestMethod]
    public void AnAdjustmentLayerAtHalfOpacityMixesHalfWay()
    {
        using var image = ImageBuffer.Create(4, 4, new Rgba32(100, 100, 100));
        new AdjustmentLayer(ImagePipeline.Create().Invert()) { Opacity = 0.5f }.Composite(image);

        Assert.IsTrue(Math.Abs(image[0, 0].R - 128) <= 1, $"{image[0, 0]}");
    }

    [TestMethod]
    public void AnEmptyAdjustmentLayerInMultiplyDarkensTheImageByItself()
    {
        using var image = ImageBuffer.Create(4, 4, new Rgba32(128, 128, 128));
        new AdjustmentLayer(ImagePipeline.Empty) { BlendMode = BlendMode.Multiply }.Composite(image);

        Assert.AreEqual(64, image[0, 0].R);
    }

    [TestMethod]
    public void AnAdjustmentLayerThatChangesTheSizeIsRefused()
    {
        using var image = ImageBuffer.Create(40, 40);
        Assert.ThrowsExactly<InvalidOperationException>(() => new AdjustmentLayer(ImagePipeline.Create().Crop(0, 0, 10, 10)).Composite(image));
    }

    [TestMethod]
    public void HiddenAndTransparentLayersDoNothing()
    {
        using var image = ParallelProcessingTests.Noise(20, 20, seed: 1);
        var before = image.ToArray();
        new FillLayer(Rgba32.Black) { IsVisible = false }.Composite(image);
        new FillLayer(Rgba32.Black) { Opacity = 0 }.Composite(image);
        new FillLayer(Rgba32.Black) { Mask = Mask.All() with { Opacity = 0 } }.Composite(image);

        Assert.IsTrue(before.AsSpan().SequenceEqual(image.Bytes));
    }

    [TestMethod]
    public void LayersCompositeOnManyThreadsWithIdenticalPixels()
    {
        using var photo = ParallelProcessingTests.Noise(400, 300, seed: 7);
        using var background = ParallelProcessingTests.Noise(1024, 768, seed: 8);
        Layer[] layers =
        [
            new ImageLayer { Image = photo, Bounds = new RectangleF(100, 80, 700, 520), BlendMode = BlendMode.Overlay, Mask = Mask.Ellipse() with { Feather = 10 } },
            new FillLayer(new Rgba32(20, 40, 120, 180)) { BlendMode = BlendMode.Color, Mask = Mask.Gradient(new(0, 0), new(0, 1)) },
            new AdjustmentLayer(ImagePipeline.Create().Contrast(0.2f).Vibrance(0.3f)) { Mask = new LuminanceRangeMask(0f, 0.4f) },
        ];

        using var one = background.Clone();
        LayerCompositor.Composite(one, layers, new OperationContext(maxDegreeOfParallelism: 1));
        using var many = background.Clone();
        LayerCompositor.Composite(many, layers, new OperationContext(maxDegreeOfParallelism: 8));

        Assert.IsTrue(one.Bytes.SequenceEqual(many.Bytes));
    }

    // ------------------------------------------------------------------ documents

    [TestMethod]
    public void ADocumentRendersItsPipelineThenItsLayersThenItsAnnotations()
    {
        using var source = ImageBuffer.Create(40, 40, new Rgba32(100, 100, 100));
        var document = ImageDocument.Create(source)
            .AddOperation(new BrightnessAdjustment(0.2f))
            .AddLayer(new FillLayer(new Rgba32(255, 0, 0)) { Mask = new RectangleMask(new RectangleF(0, 0, 20, 40)) })
            .AddAnnotation(new RectangleAnnotation { Rectangle = new RectangleF(0, 0, 40, 10), Style = ShapeStyle.Default with { FillColor = Rgba32.Black, StrokeColor = null } });

        using var rendered = new DocumentRenderer().Render(document, source);

        Assert.AreEqual(Rgba32.Black, rendered[30, 5], "The annotation is drawn over everything.");
        Assert.AreEqual(new Rgba32(255, 0, 0), rendered[5, 30], "The fill layer covers its masked half.");
        Assert.AreEqual(new BrightnessAdjustment(0.2f).Apply(source, OperationContext.Default)[30, 30], rendered[30, 30], "The pipeline shows elsewhere.");
    }

    [TestMethod]
    public void LayerEditsAreValuesWithUndo()
    {
        var fill = new FillLayer(Rgba32.Black);
        var adjustment = new AdjustmentLayer(ImagePipeline.Create().Grayscale());
        var document = ImageDocument.Create(new Size(10, 10)).AddLayer(fill).AddLayer(adjustment);

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
        using var stamp = ImageBuffer.Create(10, 10);
        var document = ImageDocument.Create(new Size(400, 300))
            .AddLayer(new ImageLayer { Image = stamp, Bounds = new RectangleF(100, 50, 200, 100), Mask = new RectangleMask(new RectangleF(100, 50, 100, 100)) });

        var scaled = (ImageLayer)document.ForScale(0.5).Layers[0];

        Assert.AreEqual(new RectangleF(50, 25, 100, 50), scaled.Bounds);
        Assert.AreEqual(new RectangleF(50, 25, 50, 50), ((RectangleMask)scaled.Mask!).Rectangle);
    }

    [TestMethod]
    public async Task ALayeredPreviewMatchesAFreshRender_AndReusesTheCompositeForAnnotationEdits()
    {
        using var source = FastPathEquivalenceTests.Noise(300, 200, seed: 51);
        using var session = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = 150 });
        var counting = new CountingLayer();
        session.AddOperation(new BlurFilter(2f));
        session.Apply(d => d.AddLayer(counting), "layer");
        session.Apply(d => d.AddLayer(new AdjustmentLayer(ImagePipeline.Create().Contrast(0.3f)) { Mask = Mask.Ellipse() }), "adjust");
        var rectangle = new RectangleAnnotation { Rectangle = new RectangleF(20, 30, 120, 60) };
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

    private static async Task<ImageBuffer> FreshRender(ImageBuffer source, ImageDocument document, int maxProxyDimension)
    {
        using var fresh = new ImageEditSession(source.Clone(), new ImageEditSessionOptions { MaxProxyDimension = maxProxyDimension });
        fresh.Apply(_ => document, "same document");
        return (await fresh.RenderPreviewAsync()).Clone();
    }

    /// <summary>A layer that darkens slightly and counts how often it is composited, shared by every copy of it.</summary>
    private sealed record CountingLayer : Layer
    {
        private readonly int[] _count = [0];
        public int Composited => _count[0];
        public override string Kind => "Counting";

        protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
        {
            Interlocked.Increment(ref _count[0]);
            var pixels = target.Pixels;
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Rgba32((byte)(pixels[i].R / 2), pixels[i].G, pixels[i].B, pixels[i].A);
        }

        protected override Layer ScaleCore(double scale) => this;
    }
}
