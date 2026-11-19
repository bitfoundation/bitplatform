using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class PaintLayerTests
{
    private static readonly BmageRgba32 Red = new(230, 20, 20);

    private static BmageImageBuffer Paint(Size size, BmageRgba32 background, params BmagePaintStroke[] strokes)
    {
        var image = BmageImageBuffer.Create(size.Width, size.Height, background);
        new BmagePaintLayer(strokes).Composite(image);
        return image;
    }

    [TestMethod]
    public void AHardBrushPaintsASolidLineOfItsSize()
    {
        using var image = Paint(new Size(60, 30), BmageRgba32.White, new BmagePaintStroke([new(10, 15), new(50, 15)], 10, Red) { Hardness = 1 });
        Assert.AreEqual(Red, image[30, 15], "the middle of the line");
        Assert.AreEqual(Red, image[30, 11], "inside its five pixel radius");
        Assert.AreEqual(BmageRgba32.White, image[30, 8], "outside it");
        Assert.AreEqual(Red, image[50, 15], "to its end point");
    }

    [TestMethod]
    public void ASoftBrushFadesTowardsItsRim()
    {
        using var image = Paint(new Size(60, 40), BmageRgba32.White, new BmagePaintStroke([new(30, 20)], 30, BmageRgba32.Black) { Hardness = 0 });
        var centre = image[30, 20].R;
        var half = image[30, 27].R;
        var rim = image[30, 33].R;
        Assert.IsLessThan(half, (int)centre, "darkest in the middle");
        Assert.IsLessThan(rim, (int)half);
        Assert.AreEqual(255, image[30, 36].R, "nothing past the rim");
    }

    [TestMethod]
    public void PaintDoesNotBuildUpWithinOneStroke()
    {
        // A stroke doubling back over itself has one even opacity; a second stroke over it builds up.
        var stroke = new BmagePaintStroke([new(10, 10), new(50, 10), new(10, 10)], 8, BmageRgba32.Black) { Opacity = 0.5f, Hardness = 1 };
        using var once = Paint(new Size(60, 20), BmageRgba32.White, stroke);
        Assert.AreEqual(128, once[30, 10].R, 2);
        using var twice = Paint(new Size(60, 20), BmageRgba32.White, stroke, stroke);
        Assert.AreEqual(64, twice[30, 10].R, 2);
    }

    [TestMethod]
    public void PressureNarrowsTheBrush()
    {
        var stroke = new BmagePaintStroke([new(10, 20), new(70, 20)], 20, BmageRgba32.Black) { Hardness = 1, Pressures = [1f, 0.2f] };
        using var image = Paint(new Size(80, 40), BmageRgba32.White, stroke);
        static int Thickness(BmageImageBuffer i, int x)
        {
            var n = 0;
            for (var y = 0; y < i.Height; y++) if (i[x, y].R < 128) n++;
            return n;
        }
        Assert.IsGreaterThan(Thickness(image, 65), Thickness(image, 15), "full pressure paints wider than light pressure");
        Assert.ThrowsExactly<ArgumentException>(() => new BmagePaintStroke([new(0, 0), new(1, 1)], 4, Red) { Pressures = [1f] });
    }

    [TestMethod]
    public void PressureCanLightenInsteadOfNarrowing()
    {
        var stroke = new BmagePaintStroke([new(10, 20), new(70, 20)], 10, BmageRgba32.Black)
        {
            Hardness = 1,
            Pressures = [1f, 0.25f],
            PressureAffectsSize = false,
            PressureAffectsOpacity = true,
        };
        using var image = Paint(new Size(80, 40), BmageRgba32.White, stroke);
        Assert.IsLessThan((int)image[68, 20].R, (int)image[12, 20].R, "the light end is paler");
        Assert.IsLessThan(255, (int)image[68, 16].R, "but the brush keeps its full width");
    }

    [TestMethod]
    public void TheEraserRemovesTheLayersPaintNotTheImage()
    {
        var paint = new BmagePaintStroke([new(10, 10), new(50, 10)], 10, Red) { Hardness = 1 };
        var eraser = new BmagePaintStroke([new(30, 0), new(30, 20)], 6, default) { Erase = true, Hardness = 1 };
        using var image = Paint(new Size(60, 20), BmageRgba32.White, paint, eraser);
        Assert.AreEqual(Red, image[15, 10]);
        Assert.AreEqual(BmageRgba32.White, image[30, 10], "the eraser shows the image below again");
        Assert.AreEqual(BmageRgba32.White, image[30, 2], "and leaves unpainted pixels alone");
    }

    [TestMethod]
    public void TheLayersBlendModeOpacityAndMaskApply()
    {
        var stroke = new BmagePaintStroke([new(0, 10), new(40, 10)], 40, new BmageRgba32(255, 0, 0)) { Hardness = 1 };
        using var image = BmageImageBuffer.Create(40, 20, new BmageRgba32(128, 128, 128));
        new BmagePaintLayer([stroke]) { Opacity = 0.5f, Mask = new BmageRectangleMask(new RectangleF(0, 0, 20, 20)) }.Composite(image);
        Assert.AreEqual(new BmageRgba32(192, 64, 64), image[10, 10], "half of the red over grey, inside the mask");
        Assert.AreEqual(new BmageRgba32(128, 128, 128), image[30, 10], "outside the mask, nothing");

        using var multiplied = BmageImageBuffer.Create(40, 20, new BmageRgba32(200, 200, 200));
        new BmagePaintLayer([stroke]) { BlendMode = BmageBlendMode.Multiply }.Composite(multiplied);
        Assert.AreEqual(new BmageRgba32(200, 0, 0), multiplied[10, 10]);
    }

    [TestMethod]
    public void TranslucentPaintKeepsTheLayerTranslucent()
    {
        using var image = BmageImageBuffer.Create(30, 30);
        new BmagePaintLayer([new BmagePaintStroke([new(15, 15)], 20, new BmageRgba32(0, 0, 255, 128)) { Hardness = 1 }]).Composite(image);
        Assert.AreEqual(128, image[15, 15].A, 1);
        Assert.AreEqual(255, image[15, 15].B);
    }

    [TestMethod]
    public void AProxyPaintsTheSameStrokeAtItsScale()
    {
        var layer = new BmagePaintLayer([new BmagePaintStroke([new(40, 40), new(160, 120)], 24, Red) { Hardness = 1 }]);
        var scaled = (BmagePaintLayer)layer.ForScale(0.25);
        Assert.AreEqual(6f, scaled.Strokes[0].Size);
        Assert.AreEqual(new Vector2(10, 10), scaled.Strokes[0].Points[0]);

        using var full = BmageImageBuffer.Create(200, 160, BmageRgba32.White);
        layer.Composite(full);
        using var proxy = BmageImageBuffer.Create(50, 40, BmageRgba32.White);
        scaled.Composite(proxy);
        using var reduced = new BmageResizeOperation(50, 40, BmageResizeMode.Stretch).Apply(full, BmageOperationContext.Default);
        Assert.IsLessThan(12.0, TestImages.MeanChannelDifference(reduced, proxy));
    }

    [TestMethod]
    public void PaintingIsAnUndoableDocumentEditThatCostsStrokesNotPixels()
    {
        using var source = TestImages.Gradient(400, 300);
        var document = BmageImageDocument.Create(source);
        var layer = new BmagePaintLayer();
        var history = new BmageEditHistory(document);
        for (var i = 0; i < 20; i++)
        {
            layer = layer.Add(new BmagePaintStroke([new(i * 10, 20), new(i * 10 + 50, 200)], 12, Red));
            history.Push(document.AddLayer(layer), $"Paint {i}");
        }
        Assert.IsLessThan(64 * 1024L, history.EstimatedMemory, "twenty strokes of history cost kilobytes");
        Assert.IsGreaterThan(history.Entries[1].EstimatedSize, history.Entries[^1].EstimatedSize, "and more strokes cost more");
        history.Undo();
        Assert.AreEqual(19, ((BmagePaintLayer)history.Current.Layers[0]).Strokes.Count);
    }

    [TestMethod]
    public void APaintLayerRoundTripsThroughJson()
    {
        var layer = new BmagePaintLayer(
        [
            new BmagePaintStroke([new(5, 5), new(40, 30), new(80, 10)], 14, Red) { Pressures = [0.2f, 1f, 0.6f], Hardness = 0.3f, Opacity = 0.7f },
            new BmagePaintStroke([new(20, 0), new(20, 60)], 5, default) { Erase = true, PressureAffectsSize = false, PressureAffectsOpacity = true },
        ]) { Name = "Sketch", BlendMode = BmageBlendMode.Overlay };
        var json = BmageJson.SerializeObject(layer);
        var back = (BmagePaintLayer)BmageJson.DeserializeObject<BmageLayer>(json);
        Assert.AreEqual(json, BmageJson.SerializeObject(back));
        CollectionAssert.AreEqual(new[] { 0.2f, 1f, 0.6f }, back.Strokes[0].Pressures!.ToArray());

        using var a = BmageImageBuffer.Create(90, 60, BmageRgba32.White);
        using var b = BmageImageBuffer.Create(90, 60, BmageRgba32.White);
        layer.Composite(a);
        back.Composite(b);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(a, b));
    }

    [TestMethod]
    public void AnEraserOnlyLayerPaintsNothing()
    {
        Assert.IsTrue(new BmagePaintLayer().IsNoOp);
        Assert.IsTrue(new BmagePaintLayer([new BmagePaintStroke([new(1, 1)], 3, default) { Erase = true }]).IsNoOp);
        Assert.ThrowsExactly<ArgumentException>(() => new BmagePaintStroke([], 3, Red));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BmagePaintStroke([new(1, 1)], 0, Red));
    }
}

[TestClass]
public sealed class ColorSamplerTests
{
    [TestMethod]
    public void OnePixelOrTheAverageAroundIt()
    {
        using var image = BmageImageBuffer.Create(5, 5, BmageRgba32.Black);
        image[2, 2] = BmageRgba32.White;
        Assert.AreEqual(BmageRgba32.White, BmageColorSampler.Sample(image, new Point(2, 2)));
        var averaged = BmageColorSampler.Sample(image, new Point(2, 2), radius: 1);
        Assert.AreEqual(new BmageRgba32(28, 28, 28), averaged, "one white pixel in nine");
        Assert.AreEqual(BmageRgba32.Black, BmageColorSampler.Sample(image, new Point(-10, 99)), "outside, the nearest edge pixel");
    }

    [TestMethod]
    public void TransparentPixelsDoNotTintTheAverage()
    {
        using var image = BmageImageBuffer.Create(3, 1);
        image[0, 0] = new BmageRgba32(255, 0, 0);
        image[1, 0] = new BmageRgba32(0, 255, 0, 0);
        image[2, 0] = new BmageRgba32(255, 0, 0);
        var sample = BmageColorSampler.Sample(image, new Point(1, 0), radius: 1);
        Assert.AreEqual(255, sample.R);
        Assert.AreEqual(0, sample.G);
        Assert.AreEqual(170, sample.A);
    }
}
